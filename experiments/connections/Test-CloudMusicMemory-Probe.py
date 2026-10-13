"""Offline fixtures only: this test never opens a real process."""
import copy
import importlib.util
from pathlib import Path
import struct
import unittest

spec = importlib.util.spec_from_file_location('probe', Path(__file__).with_name('CloudMusicMemory-Probe.py'))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class MemoryFixture:
    def __init__(self, token=b'12345_ABC', heap=False):
        self.regions = {}
        self.reads = []
        self.base = 0x100000
        self.profile = {'schema': 1, 'module': 'cloudmusic.dll', 'fingerprint': {'sha256': 'fixture'},
                        'positionRva': 0x100, 'durationRva': 0x200, 'songRootRva': 0x300,
                        'songOffsets': [16, 0, 16, 104, 0]}
        self.regions[self.base + 0x100] = struct.pack('<d', 27.25)
        self.regions[self.base + 0x200] = struct.pack('<d', 240.5)
        for at, value in [(self.base + 0x300, 0x200000), (0x200010, 0x300000),
                          (0x300000, 0x400000), (0x400010, 0x500000)]:
            self.regions[at] = struct.pack('<Q', value)
        if heap:
            self.regions[0x500068] = struct.pack('<Q', 0x600000) + b'\0' * 8 + struct.pack('<QQ', len(token), 63)
            self.regions[0x600000] = token + b'\0'
        else:
            self.regions[0x500068] = (token + b'\0').ljust(16, b'\0') + struct.pack('<QQ', len(token), 15)

    def read(self, at, size):
        self.reads.append((at, size))
        return self.regions[at][:size]


class Tests(unittest.TestCase):
    def test_minimum_access_mask(self):
        self.assertEqual(probe.READ_ACCESS, 0x410)
        self.assertEqual(probe.READ_ACCESS & (0x20 | 0x8 | 0x2 | 0x800 | 0x1), 0)

    def test_inline_id_and_progress(self):
        f = MemoryFixture()
        value = probe.read_sample(f, f.base, f.profile)
        self.assertTrue(value['valid'])
        self.assertEqual((value['songId'], value['positionSeconds'], value['durationSeconds']), ('12345', 27.25, 240.5))
        self.assertEqual(value['songIdStorage'], 'inline')

    def test_heap_id(self):
        f = MemoryFixture(b'2123750257_ABCDEF', heap=True)
        self.assertEqual(probe.read_sample(f, f.base, f.profile)['songId'], '2123750257')
        self.assertLess(max(n for _, n in f.reads), 256)

    def test_bad_values_stop_before_pointer_reads(self):
        for value in [float('nan'), float('inf'), -1, 1e10]:
            f = MemoryFixture()
            f.regions[f.base + 0x100] = struct.pack('<d', value)
            self.assertFalse(probe.read_sample(f, f.base, f.profile)['valid'])
            self.assertEqual(len(f.reads), 2)

    def test_unrecognized_text_never_leaks(self):
        f = MemoryFixture(b'PRIVATE')
        with self.assertRaises(ValueError) as caught:
            probe.song_id(f, f.base, f.profile)
        self.assertNotIn('PRIVATE', str(caught.exception))

    def test_invalid_pointer_stops(self):
        f = MemoryFixture()
        f.regions[f.base + 0x300] = struct.pack('<Q', 0)
        with self.assertRaises(ValueError):
            probe.song_id(f, f.base, f.profile)
        self.assertEqual(len(f.reads), 1)

    def test_mixed_song_sample_rejected(self):
        class Race(MemoryFixture):
            def read(self, at, size):
                if at == self.base + 0x300 and any(p == at for p, _ in self.reads):
                    self.regions[0x500068] = b'67890_X\0'.ljust(16, b'\0') + struct.pack('<QQ', 7, 15)
                return super().read(at, size)
        f = Race()
        self.assertFalse(probe.read_sample(f, f.base, f.profile)['valid'])

    def test_exact_fingerprint_required(self):
        f = MemoryFixture()
        with self.assertRaises(ValueError):
            probe.validate_profile(f.profile, {'sha256': 'different'}, [(0, 4096, 0x80000000)])

    def test_rvas_must_stay_in_reviewed_data(self):
        f = MemoryFixture()
        actual = f.profile['fingerprint']
        probe.validate_profile(f.profile, actual, [(0, 4096, 0x80000000)])
        for sections in [[(0, 4096, 0x40000000)], [(0, 256, 0x80000000)]]:
            with self.assertRaises(ValueError):
                probe.validate_profile(f.profile, actual, sections)
        bad = copy.deepcopy(f.profile)
        bad['songOffsets'] = [0] * 50
        with self.assertRaises(ValueError):
            probe.validate_profile(bad, actual, [(0, 4096, 0x80000000)])


if __name__ == '__main__':
    unittest.main()
