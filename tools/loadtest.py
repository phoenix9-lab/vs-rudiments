#!/usr/bin/env python3
"""Headless load test of the built mod under a module preset.

  loadtest.py PRESET [--compat]      (build first: dotnet build -c Release in Rudiments/)

Packages Rudiments/ into a fresh data dir under the Flatpak cache, writes ModConfig for the
preset, boots the dedicated server, runs `/rudimentsmodules audit`, stops it, and prints the
lines that matter. --compat also copies the compat mods from the user's Mods folder.
"""
import json, os, subprocess, sys, time, zipfile, shutil, glob, re

HOME = os.path.expanduser('~')
APP = HOME + '/.var/app/at.vintagestory.VintageStory'
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'Rudiments'))
COMPAT = ['clayworks_', 'fitnshard', 'StringSense', 'wool-', 'spinningwheel', 'toolsmith', 'vsroofing']

OFF = dict(FibreEnabled=False, NettleEnabled=False, WoolCardingEnabled=False, ReedSpreadEnabled=False,
           SeedsOnlyWhenMature=False, WareTiersEnabled=False, ClayBreakageEnabled=False,
           ClaySeepageEnabled=False, KilnsEnabled=False, PorcelainEnabled=False, GlazesEnabled=False)
PRESETS = {
    'defaults': {},
    'alloff': OFF,
    'breakage': {**OFF, 'ClayBreakageEnabled': True},
    'breakage-seep-tiers': {**OFF, 'ClayBreakageEnabled': True, 'ClaySeepageEnabled': True, 'WareTiersEnabled': True},
    'fibre': {**OFF, 'FibreEnabled': True},
    'kilns-no-tiers': {**OFF, 'KilnsEnabled': True},
}

def main():
    preset = sys.argv[1]
    compat = '--compat' in sys.argv
    stamp = time.strftime('%H%M%S')
    d = f'{APP}/cache/rudiments-mod-{preset}-{"c" if compat else "r"}-{stamp}'
    os.makedirs(d + '/Mods'); os.makedirs(d + '/ModConfig')
    ver = json.load(open(REPO + '/modinfo.json'))['version']
    with zipfile.ZipFile(f'{d}/Mods/rudiments_{ver}.zip', 'w', zipfile.ZIP_DEFLATED) as z:
        for f in ('modinfo.json', 'modicon.png'): z.write(f'{REPO}/{f}', f)
        z.write(f'{REPO}/bin/Release/Rudiments.dll', 'Rudiments.dll')
        for root, _, files in os.walk(f'{REPO}/assets'):
            for f in files:
                p = os.path.join(root, f); z.write(p, os.path.relpath(p, REPO))
    if compat:
        for pre in COMPAT:
            for m in glob.glob(f'{APP}/config/VintagestoryData/Mods/{pre}*'): shutil.copy(m, d + '/Mods')
    json.dump(PRESETS[preset], open(d + '/ModConfig/rudiments.json', 'w'))
    gi = HOME + '/.local/share/flatpak/app/at.vintagestory.VintageStory/x86_64/stable/active/files/extra/vintagestory'
    cmd = ['flatpak', 'run', '--command=bash', 'at.vintagestory.VintageStory', '-c',
           f'cd /app/extra/vintagestory && timeout 240 dotnet VintagestoryServer.dll --dataPath {d} --port 42099']
    p = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    out = []
    start = time.time(); sent = False
    import threading
    def reader():
        for line in p.stdout: out.append(line)
    t = threading.Thread(target=reader, daemon=True); t.start()
    while p.poll() is None and time.time() - start < 230:
        text = ''.join(out)
        if 'Dedicated Server now running' in text and not sent:
            time.sleep(2)
            p.stdin.write('/rudimentsmodules audit\n'); p.stdin.flush(); time.sleep(4)
            p.stdin.write('/stop\n'); p.stdin.flush(); sent = True
        time.sleep(1)
    try: p.wait(timeout=30)
    except Exception: p.kill()
    t.join(2)
    text = ''.join(out)
    print(f'== {preset} compat={compat} dir={d}')
    print('server running:', 'Dedicated Server now running' in text)
    for line in text.splitlines():
        if re.search(r'\[(Error|Warning)\]|Rudiments\]|Modules|Nothing of a switched|live |visible |recipes of|patches|unmet', line) and 'Rudiments' in line or re.search(r'\[Error\]', line) \
           or line.startswith(('Fibre=', 'Nothing of', 'live ', 'visible ', 'recipes of')):
            print('  ' + line.strip()[:260])

if __name__ == '__main__':
    main()
