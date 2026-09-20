# Linux Mint handoff (same zip as PythonAnywhere)

Use this when Windows is unusable (WSL/VMware broken) and you boot into **Linux Mint**.
The deliverable is one zip: `dist/cannon-webgl-latest.zip`.

## What the zip contains

```text
flask_app.py              # Flask host (PA WSGI + local)
requirements.txt
README.md
LINUX_MINT_HANDOFF.md     # this file
AGENTS.md                 # project agent context (copy from repo root into zip)
WEBGL_BUILD_MANIFEST.json # file list + sha256 of every WebGL asset
webgl/index.html
webgl/Build/...
webgl/TemplateData/...
serve_linux.sh            # one-shot Flask serve on Mint
```

Same archive you upload to PythonAnywhere.

## Path A — PythonAnywhere (test the build in a browser)

1. On a machine that can still build Unity WebGL (or use an already-built zip):
   `python package_webgl_zip.py`
2. Upload `cannon-webgl-latest.zip` to PA → unzip into your web app directory.
3. WSGI: `from flask_app import application`
4. Open the PA site URL and play the Practice WebGL build.

## Path B — Linux Mint dual-boot (Flask first, Buildroot later)

### 0. Get the zip onto Mint

Any one of:

- USB: copy `pythonanywhere_flask/dist/cannon-webgl-latest.zip` (+ optional `*.sha256`)
- Cloud: Dropbox/Drive/PA files tab download
- Git: clone this repo on Mint (zip under `pythonanywhere_flask/dist/` if committed, or rebuild WebGL on another host and copy only the zip)

Verify (optional):

```bash
sha256sum -c cannon-webgl-latest.zip.sha256
```

### 1. Unzip and serve with Flask (prove WebGL before Buildroot)

```bash
mkdir -p ~/cannon && cd ~/cannon
unzip -o /path/to/cannon-webgl-latest.zip -d pythonanywhere_flask
cd pythonanywhere_flask
chmod +x serve_linux.sh
./serve_linux.sh
```

Open http://127.0.0.1:5000/ on Mint (or `http://<mint-lan-ip>:5000/` from another device on LAN).

If Flask works here, the WebGL payload is good. Only then touch Buildroot.

### 2. Buildroot (after Flask works on Mint)

Goal: embed the **same** `webgl/` tree into a tiny Linux rootfs and serve it on boot (lab/LAN only).

Suggested order:

1. On Mint: clone Buildroot, use an x86_64 defconfig (QEMU target first).
2. Enable: dropbear **or** openssh, plus **lighttpd** or **nginx** (or python3 + a tiny static server).
3. Copy zip’s `webgl/` into rootfs overlay, e.g. `/opt/cannon/webgl`.
4. Init/systemd (or Buildroot inittab) starts the web server on boot → port 80 or 8080.
5. Boot in **QEMU** and browse from the Mint host. Do not expose to the public internet.

Overlay sketch (concept):

```text
board/cannon/rootfs_overlay/opt/cannon/webgl/   ← contents of zip’s webgl/
board/cannon/rootfs_overlay/etc/...             ← lighttpd/nginx site config
```

You do **not** need Flask inside Buildroot if you only serve static WebGL files. Keep Flask for PA + Mint desktop testing.

## Agent context on Mint

Open this repo (or the unzipped folder) in Cursor on Mint and point the agent at:

1. `AGENTS.md` (repo root) — locked scope, gameplay notes
2. `pythonanywhere_flask/LINUX_MINT_HANDOFF.md` — this file
3. `pythonanywhere_flask/README.md` — build/serve details

Tell the agent: **Windows VMware/WSL path is retired; Mint dual-boot is the lab host; same zip as PA.**

## Do not wait on

- WSL
- VMware Ubuntu Server (optional later if Windows is healthy again)

## Success criteria

| Step | Passes when |
|------|-------------|
| PA | Practice scene loads and fires in browser |
| Mint Flask | Same zip → `./serve_linux.sh` → playable at :5000 |
| Buildroot | QEMU image serves `webgl/` statically; game loads from that URL |
