if (args.Contains("--enet-allocation")) { ENetAllocationTests.Run(); return; }
if (args.Contains("--gzip-cleanup")) { GZIPCleanupTests.Run(args.Contains("--native-probe")); return; }
if (args.Contains("--freetype-allocation")) { NativeFontAllocationTests.Run(); return; }
NativeAllocationTests.Run();
if (args.Contains("--native-allocation")) return;
ContractChecks.Run();
CanvasVertexLayoutTests.Run();
