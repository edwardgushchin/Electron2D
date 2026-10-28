if (args.Contains("--freetype-allocation")) { NativeFontAllocationTests.Run(); return; }
NativeAllocationTests.Run();
if (args.Contains("--native-allocation")) return;
ContractChecks.Run();
CanvasVertexLayoutTests.Run();
