"""Independent Windows x64 playback-field probe. No player controls or writes.

Reads only explicit, fingerprint-gated RVAs and a bounded song-ID pointer chain.
No injection, debugger, privilege adjustment, memory scanning or raw memory dump.
Uses Python's standard library; importing this file never attaches to a process.
"""
import argparse
import ctypes as C
from ctypes import wintypes as W
from contextlib import closing
import datetime as dt
import hashlib
import json
import math
import os
from pathlib import Path
import re
import sqlite3
import struct
import time

READ_ACCESS = 0x0400 | 0x0010  # PROCESS_QUERY_INFORMATION | PROCESS_VM_READ
MAX_READ = 256


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def fingerprint(path):
    data = Path(path).read_bytes()
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    if data[:2] != b'MZ' or data[pe:pe + 4] != b'PE\0\0':
        raise ValueError('Not a PE image')
    if struct.unpack_from('<H', data, pe + 4)[0] != 0x8664:
        raise ValueError('Only the x64 module is supported')
    count, optional = struct.unpack_from('<H', data, pe + 6)[0], struct.unpack_from('<H', data, pe + 20)[0]
    sections = []
    for i in range(count):
        at = pe + 24 + optional + i * 40
        size, rva = struct.unpack_from('<II', data, at + 8)
        flags = struct.unpack_from('<I', data, at + 36)[0]
        sections.append((rva, size, flags))
    return {'sha256': hashlib.sha256(data).hexdigest(), 'fileSize': len(data),
            'imageSize': struct.unpack_from('<I', data, pe + 24 + 56)[0],
            'peTimestamp': struct.unpack_from('<I', data, pe + 8)[0]}, pe, sections


def validate_profile(profile, actual, sections):
    if profile.get('schema') != 1 or profile.get('module') != 'cloudmusic.dll':
        raise ValueError('Unsupported profile')
    if profile.get('fingerprint') != actual:
        raise ValueError('Exact DLL fingerprint mismatch; refusing memory reads')
    for key in ('positionRva', 'durationRva', 'songRootRva'):
        rva = profile[key]
        if type(rva) is not int or not any(start <= rva and rva + 8 <= start + size and flags & 0x80000000
                                         for start, size, flags in sections):
            raise ValueError('Candidate field is outside writable module data: ' + key)
    offsets = profile.get('songOffsets')
    if offsets != [0x10, 0, 0x10, 0x68, 0]:
        raise ValueError('Unreviewed pointer-chain layout')


class ReadOnlyProcess:
    def __init__(self, pid, expected_exe, budget):
        if os.name != 'nt' or C.sizeof(C.c_void_p) != 8:
            raise RuntimeError('Use 64-bit Python on Windows')
        self.kernel = C.WinDLL('kernel32', use_last_error=True)
        self.psapi = C.WinDLL('psapi', use_last_error=True)
        signatures = {
            'OpenProcess': ([W.DWORD, W.BOOL, W.DWORD], W.HANDLE),
            'CloseHandle': ([W.HANDLE], W.BOOL),
            'ReadProcessMemory': ([W.HANDLE, C.c_void_p, C.c_void_p, C.c_size_t, C.POINTER(C.c_size_t)], W.BOOL),
            'GetExitCodeProcess': ([W.HANDLE, C.POINTER(W.DWORD)], W.BOOL),
            'QueryFullProcessImageNameW': ([W.HANDLE, W.DWORD, W.LPWSTR, C.POINTER(W.DWORD)], W.BOOL),
            'GetProcessTimes': ([W.HANDLE] + [C.POINTER(W.FILETIME)] * 4, W.BOOL),
        }
        for name, (args, result) in signatures.items():
            method = getattr(self.kernel, name)
            method.argtypes, method.restype = args, result
        self.psapi.EnumProcessModulesEx.argtypes = [W.HANDLE, C.POINTER(W.HMODULE), W.DWORD, C.POINTER(W.DWORD), W.DWORD]
        self.psapi.EnumProcessModulesEx.restype = W.BOOL
        self.psapi.GetModuleFileNameExW.argtypes = [W.HANDLE, W.HMODULE, W.LPWSTR, W.DWORD]
        self.psapi.GetModuleFileNameExW.restype = W.DWORD
        self.handle = self.kernel.OpenProcess(READ_ACCESS, False, pid)
        if not self.handle:
            raise C.WinError(C.get_last_error())
        self.bytes_read = self.requests = 0
        self.budget = budget
        try:
            text, length = C.create_unicode_buffer(32768), W.DWORD(32768)
            if not self.kernel.QueryFullProcessImageNameW(self.handle, 0, text, C.byref(length)):
                raise C.WinError(C.get_last_error())
            self.exe = Path(text.value).resolve()
            if self.exe.name.lower() != 'cloudmusic.exe' or self.exe != Path(expected_exe).resolve():
                raise ValueError('Unexpected executable identity')
            created, exited, kernel, user = W.FILETIME(), W.FILETIME(), W.FILETIME(), W.FILETIME()
            if not self.kernel.GetProcessTimes(self.handle, C.byref(created), C.byref(exited), C.byref(kernel), C.byref(user)):
                raise C.WinError(C.get_last_error())
            self.created = (created.dwHighDateTime << 32) | created.dwLowDateTime
        except Exception:
            self.close()
            raise

    def module(self):
        modules, needed = (W.HMODULE * 1024)(), W.DWORD()
        if not self.psapi.EnumProcessModulesEx(self.handle, modules, C.sizeof(modules), C.byref(needed), 3):
            raise C.WinError(C.get_last_error())
        if needed.value > C.sizeof(modules):
            raise ValueError('Module list exceeds the fixed bound')
        for module in modules[:needed.value // C.sizeof(W.HMODULE)]:
            name = C.create_unicode_buffer(32768)
            if self.psapi.GetModuleFileNameExW(self.handle, module, name, len(name)):
                path = Path(name.value).resolve()
                if path.name.lower() == 'cloudmusic.dll' and path.parent == self.exe.parent:
                    return int(module), path
        raise ValueError('Expected cloudmusic.dll not loaded')

    def alive(self):
        code = W.DWORD()
        return bool(self.kernel.GetExitCodeProcess(self.handle, C.byref(code))) and code.value == 259

    def read(self, address, size):
        if not 0 < size <= MAX_READ or self.bytes_read + size > self.budget:
            raise ValueError('Read budget exceeded')
        if not 0x10000 <= address < 0x800000000000 - size:
            raise ValueError('Invalid user-mode address')
        buffer, copied = C.create_string_buffer(size), C.c_size_t()
        self.requests += 1
        if not self.kernel.ReadProcessMemory(self.handle, address, buffer, size, C.byref(copied)) or copied.value != size:
            raise OSError('Candidate field unreadable; winerror=' + str(C.get_last_error()))
        self.bytes_read += size
        return buffer.raw

    def close(self):
        if self.handle:
            self.kernel.CloseHandle(self.handle)
            self.handle = None


def pointer(reader, address):
    value = struct.unpack('<Q', reader.read(address, 8))[0]
    if not 0x10000 <= value < 0x800000000000:
        raise ValueError('Invalid song pointer')
    return value


def song_id(reader, base, profile):
    value = pointer(reader, base + profile['songRootRva'])
    offsets = profile['songOffsets']
    for index, offset in enumerate(offsets[:-1]):
        slot = value + offset
        if index == len(offsets) - 2:
            obj = reader.read(slot, 32)
            length, capacity = struct.unpack_from('<QQ', obj, 16)
            if not 1 <= length <= 64 or not length <= capacity <= 65536:
                raise ValueError('Song string layout unrecognized')
            if capacity == 15 and length <= 15:
                raw, storage = obj[:length + 1], 'inline'
            elif capacity >= 16:
                raw, storage = reader.read(pointer(reader, slot), length + 1), 'heap'
            else:
                raise ValueError('Song string capacity unrecognized')
            match = re.fullmatch(rb'([1-9][0-9]{0,19})(?:_[A-Za-z0-9]+)?\0', raw)
            if not match:
                raise ValueError('Song token unrecognized; raw text is not recorded')
            return match.group(1).decode('ascii'), storage
        value = pointer(reader, slot)
    raise ValueError('Missing song string')


def read_sample(reader, base, profile):
    total = struct.unpack('<d', reader.read(base + profile['durationRva'], 8))[0]
    pos = struct.unpack('<d', reader.read(base + profile['positionRva'], 8))[0]
    if not math.isfinite(total) or not math.isfinite(pos) or not 0 < total <= 86400 or not 0 <= pos <= total + .5:
        return {'valid': False, 'reason': 'Position/duration bounds failed'}
    ident, storage = song_id(reader, base, profile)
    pos_after = struct.unpack('<d', reader.read(base + profile['positionRva'], 8))[0]
    ident_after, _ = song_id(reader, base, profile)
    total_after = struct.unpack('<d', reader.read(base + profile['durationRva'], 8))[0]
    if ident != ident_after or total != total_after or not math.isfinite(pos_after) or abs(pos_after - pos) > .5:
        return {'valid': False, 'reason': 'Song/position changed during bounded sample'}
    return {'valid': True, 'positionSeconds': pos_after, 'durationSeconds': total,
            'songId': ident, 'songIdStorage': storage}


def local_reference(ident):
    file = Path(os.environ['LOCALAPPDATA']) / 'NetEase/CloudMusic/Library/webdb.dat'
    with closing(sqlite3.connect(file.as_uri() + '?mode=ro', uri=True, timeout=.05)) as db:
        row = db.execute('SELECT jsonStr FROM historyTracks WHERE id=?', (ident,)).fetchone()
        if not row:
            return None
        if len(row[0]) > 524288:
            raise ValueError('Reference record too large')
        data = json.loads(row[0])
        return {'songId': str(data['id']), 'title': data['name'],
                'artists': [a['name'] for a in data.get('artists', [])],
                'durationSeconds': float(data['duration']) / 1000}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--pid', type=int, required=True)
    parser.add_argument('--expected-exe', required=True)
    parser.add_argument('--profile', type=Path, required=True)
    parser.add_argument('--seconds', type=float, default=30)
    parser.add_argument('--interval', type=float, default=.5)
    parser.add_argument('--output-prefix', type=Path, required=True)
    args = parser.parse_args()
    if not 1 <= args.seconds <= 300 or not .25 <= args.interval <= 5:
        parser.error('Use 1..300 seconds and .25..5 second intervals')
    output, report = Path(str(args.output_prefix) + '.jsonl'), Path(str(args.output_prefix) + '-summary.json')
    if output.exists() or report.exists():
        parser.error('Refusing to overwrite previous probe evidence')
    output.parent.mkdir(parents=True, exist_ok=True)
    profile = json.loads(args.profile.read_text(encoding='utf-8-sig'))
    results, reader, error, refs = [], None, None, {}
    started = time.monotonic()
    try:
        reader = ReadOnlyProcess(args.pid, args.expected_exe, int(math.ceil(args.seconds / args.interval) * 512 + 1024))
        base, module = reader.module()
        actual, pe, sections = fingerprint(module)
        validate_profile(profile, actual, sections)
        if struct.unpack('<I', reader.read(base + pe + 8, 4))[0] != actual['peTimestamp']:
            raise ValueError('Loaded module differs from the fingerprinted disk image')
        invalid_streak = 0
        with output.open('x', encoding='utf-8') as stream:
            while time.monotonic() - started < args.seconds:
                if not reader.alive():
                    raise RuntimeError('Original player process exited; probe will not reattach')
                row = {'at': utc(), 'elapsed': time.monotonic() - started}
                before = time.perf_counter()
                try:
                    row.update(read_sample(reader, base, profile))
                    if row.get('valid'):
                        ident = row['songId']
                        if ident not in refs:
                            refs[ident] = local_reference(ident)
                        ref = refs[ident]
                        row['referenceDurationMatches'] = bool(ref and abs(ref['durationSeconds'] - row['durationSeconds']) <= 1)
                except Exception as exc:
                    row.update(valid=False, reason=str(exc))
                row['readMilliseconds'] = round((time.perf_counter() - before) * 1000, 3)
                results.append(row)
                stream.write(json.dumps(row, ensure_ascii=False, allow_nan=False) + '\n')
                stream.flush()
                invalid_streak = 0 if row.get('valid') else invalid_streak + 1
                if invalid_streak >= 3:
                    raise RuntimeError('Three invalid samples; refusing to continue or scan memory')
                time.sleep(min(args.interval, max(0, args.seconds - (time.monotonic() - started))))
    except Exception as exc:
        error = str(exc)
    finally:
        alive = reader.alive() if reader else None
        created = reader.created if reader else None
        requests, size = (reader.requests, reader.bytes_read) if reader else (0, 0)
        if reader:
            reader.close()
    valid = [r for r in results if r.get('valid')]
    summary = {'probe': 'cloudmusic-read-only-memory', 'at': utc(), 'pid': args.pid,
               'processCreationFileTime': created, 'originalProcessStillAlive': alive,
               'requestedProcessAccess': hex(READ_ACCESS), 'playerControlsSent': 0,
               'samples': len(results), 'validSamples': len(valid), 'elapsedSeconds': round(time.monotonic() - started, 3),
               'readRequests': requests, 'bytesRead': size, 'references': refs,
               'observedSongIds': sorted({r['songId'] for r in valid}),
               'positionMin': min((r['positionSeconds'] for r in valid), default=None),
               'positionMax': max((r['positionSeconds'] for r in valid), default=None),
               'referenceDurationMatches': sum(bool(r.get('referenceDurationMatches')) for r in valid),
               'maxReadMilliseconds': max((r['readMilliseconds'] for r in results), default=None),
               'profileFingerprint': profile.get('fingerprint'), 'error': error,
               'limitation': 'Passive observations only; unobserved play/pause/seek/track changes remain unverified.'}
    report.write_text(json.dumps(summary, ensure_ascii=False, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps(summary, ensure_ascii=True, allow_nan=False))
    return 0 if valid and not error else 2


if __name__ == '__main__':
    raise SystemExit(main())
