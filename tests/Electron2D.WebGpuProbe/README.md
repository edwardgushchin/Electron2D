# Browser WebGPU shader probe

This standalone page requests a WebGPU adapter and device, compiles a WGSL vertex/fragment shader, draws a full-screen red triangle, and waits for GPU submission. It proves the browser's programmable GPU path; it does **not** use Electron2D's canvas or ShaderMaterial.

From the repository root:

```bash
python3 -m http.server 8765 --bind 127.0.0.1 --directory tests/Electron2D.WebGpuProbe
```

Open `http://127.0.0.1:8765/` in Chrome. Success shows a red 640×360 canvas and `WebGPU fragment shader submitted`; errors are displayed below the canvas. Localhost provides the secure context required by WebGPU. The page was visually checked in Chrome on 2026-09-25. The engine's WebGPU backend and persistent browser host remain separate work.
