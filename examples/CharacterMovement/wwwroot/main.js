import { dotnet } from "./_framework/dotnet.js";

const canvas = document.getElementById("canvas");
const errorDisplay = document.getElementById("error");
document.getElementById("fullscreen").addEventListener("click", async () => {
  try {
    if (document.fullscreenElement) await document.exitFullscreen();
    else await document.documentElement.requestFullscreen();
  } catch (error) {
    console.warn("Fullscreen request was not granted.", error);
  }
  canvas.focus();
});
canvas.addEventListener("pointerdown", () => canvas.focus());
document.addEventListener("keydown", event => {
  if (["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].includes(event.key)) event.preventDefault();
});

try {
  const runtime = await dotnet.withModuleConfig({ canvas }).create();
  canvas.focus();
  await runtime.runMain(runtime.getConfig().mainAssemblyName, []);
} catch (error) {
  errorDisplay.hidden = false;
  errorDisplay.textContent = String(error.stack ?? error);
  console.error(error);
}
