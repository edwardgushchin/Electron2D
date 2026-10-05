import { dotnet } from "./_framework/dotnet.js";

try {
  const runtime = await dotnet.withDiagnosticTracing(false).create();
  const result = await runtime.runMain(runtime.getConfig().mainAssemblyName, []);
  if (result !== 0) throw new Error(`Managed test runner exited with ${result}`);
  globalThis.electron2dResult = { status: "passed" };
  document.body.textContent = "PASS";
} catch (error) {
  globalThis.electron2dResult = { status: "failed", error: String(error) };
  document.body.textContent = `FAIL: ${error}`;
  console.error(error);
}
