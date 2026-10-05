import { dotnet } from "./_framework/dotnet.js";

try {
  // Keep optimized interpreter code fixed during strict warmed allocation checks.
  const runtime = await dotnet.withDiagnosticTracing(false)
    .withEnvironmentVariable("MONO_INTERPRETER_OPTIONS", "-tiering").create();
  const result = await runtime.runMain(runtime.getConfig().mainAssemblyName, []);
  if (result !== 0) throw new Error(`Managed test runner exited with ${result}`);
  globalThis.electron2dResult = { status: "passed" };
  document.body.textContent = "PASS";
} catch (error) {
  globalThis.electron2dResult = { status: "failed", error: String(error.stack ?? error).slice(0, 12000) };
  document.body.textContent = `FAIL: ${error}`;
  console.error(error.stack ?? error);
}

const run = new URLSearchParams(location.search).get("run");
if (run) {
  await fetch("/__electron2d_result", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ run, ...globalThis.electron2dResult }),
  });
}
