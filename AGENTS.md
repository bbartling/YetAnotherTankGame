# Agent Context: Cannon Physics Sim (Practice / WebGL)

Unity practice-scene cannon simulator. Hosted for learning via Flask WebGL,
PythonAnywhere zip upload, and later Buildroot on **Linux Mint dual-boot**.

## LOCKED SCOPE

- Primary gameplay scene: `Assets/Scenes/Practice.unity` only.
- WebGL build output: `pythonanywhere_flask/webgl/` (`index.html`, `Build/`, `TemplateData/`).
- Flask entry: `pythonanywhere_flask/flask_app.py` (`application` for WSGI).
- **One portable zip:** `pythonanywhere_flask/package_webgl_zip.py` →
  `pythonanywhere_flask/dist/cannon-webgl-latest.zip` (+ `.sha256`).
  Same zip for PythonAnywhere **and** Linux Mint / Buildroot.

## Current lab reality (read this first)

- Windows WSL + VMware are **unreliable / corrupted** — do **not** depend on them.
- Next reliable host: **Linux Mint dual-boot**.
- Agent on Mint: open this repo (or unzip) and follow
  `pythonanywhere_flask/LINUX_MINT_HANDOFF.md` plus this file.
- Optional later: fix/reinstall Windows, then VMware Ubuntu is nice-to-have again — not required.

## Unity WebGL build (wherever Unity still runs)

1. File → Build Settings → WebGL.
2. Practice scene in the build.
3. Build into `pythonanywhere_flask/webgl`.
4. Local Windows test (if OS is healthy): see `pythonanywhere_flask/README.md`.
5. Package once for both destinations:
   ```text
   python pythonanywhere_flask/package_webgl_zip.py
   ```
6. Copy `dist/cannon-webgl-latest.zip` (+ `.sha256`) to USB / cloud / PA.

## PythonAnywhere (browser test of the zip)

1. Upload `cannon-webgl-latest.zip`, unzip into the web app folder.
2. WSGI: `from flask_app import application`
3. Play Practice in the browser. If it works, the zip is the gold master for Mint.

## Linux Mint dual-boot (primary lab path)

```bash
mkdir -p ~/cannon && cd ~/cannon
unzip -o /path/to/cannon-webgl-latest.zip -d pythonanywhere_flask
cd pythonanywhere_flask
chmod +x serve_linux.sh
./serve_linux.sh
# http://127.0.0.1:5000/
```

Full steps + Buildroot notes: `pythonanywhere_flask/LINUX_MINT_HANDOFF.md`.

### Buildroot (only after Mint Flask works)

- [ ] Clone Buildroot on Mint; x86_64 defconfig (QEMU first)
- [ ] Packages: dropbear/openssh + lighttpd/nginx (static WebGL; Flask optional)
- [ ] Rootfs overlay: zip’s `webgl/` → `/opt/cannon/webgl`
- [ ] Init starts web server on boot
- [ ] Test in QEMU before a “real” image
- [ ] LAN-only / lab-only — not for public internet

## Ubuntu Server VMware (optional, Windows healthy again)

Suggested VM: **4 vCPU / 8 GB RAM / 80 GB thin disk**, NIC NAT/Bridged,
`openssh-server` + `open-vm-tools`. Same zip or folder; Flask on `:5000`.
Skip this path while Windows virt is broken.

## Cannon gameplay notes

- Fire trail / 13s fuse: `ProjectileCameraController`
- Tutorial coach marks: `CannonControlsTutorial`
- Blow-up if shot fails muzzle clearance: `CannonManager.NotifyFailedMuzzleClearance`
- Successful shot camera follows `baseForCamera` (goal/target); destroy mode locks camera on cannon
