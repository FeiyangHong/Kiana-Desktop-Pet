"""One ephemeral local turn to verify trusted hooks; never restarts desktop apps."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tomllib
import uuid

parser = argparse.ArgumentParser()
parser.add_argument('--codex', required=True)
parser.add_argument('--legacy-local-harness', action='store_true')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
root = repo / '.local' / 'connection-probes'
run = root / ('cli-' + str(uuid.uuid4()))
run.mkdir(parents=True)
config_path = Path(os.environ['USERPROFILE']) / '.codex' / 'config.toml'
config_bytes = config_path.read_bytes()
config = tomllib.loads(config_bytes.decode('utf-8'))
command = [args.codex, 'exec', '--ephemeral', '--sandbox', 'read-only', '--skip-git-repo-check', '--json', '-C', str(run), '-c', 'notify=[]', '-c', 'approval_policy="never"']
if args.legacy_local_harness:
    command += ['--disable', 'code_mode_host']
# Disable unrelated integrations in this test process, preserving model/account defaults.
for section in ('plugins', 'mcp_servers'):
    for name in config.get(section, {}):
        if not re.fullmatch(r'[A-Za-z0-9_@-]+', name):
            raise ValueError('Unsupported override name in isolated probe')
        command += ['-c', f'{section}.{name}.enabled=false']
command += ['This is a local connection test. Reply with exactly HOOK_PROBE_OK. Do not use any tools, read or write files, or control any applications.']
with (run / 'stdout.jsonl').open('w', encoding='utf-8') as stdout, (run / 'stderr.log').open('w', encoding='utf-8') as stderr:
    process = subprocess.Popen(command, cwd=repo, stdin=subprocess.DEVNULL, stdout=stdout, stderr=stderr, creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        code = process.wait(timeout=120)
    except subprocess.TimeoutExpired:
        process.terminate()  # This probe's CLI only; never target desktop processes.
        code = process.wait(timeout=10)
report = {'probe': 'ephemeral-local-codex', 'exitCode': code, 'output': str(run), 'desktopConfigurationChanged': hashlib.sha256(config_path.read_bytes()).digest() != hashlib.sha256(config_bytes).digest(), 'desktopAppsRestarted': False}
(run / 'result.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report))
raise SystemExit(0 if code == 0 and not report['desktopConfigurationChanged'] else 1)
