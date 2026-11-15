if (args.Contains("--engine-metadata")) { EngineMetadataChecks.Run(); return; }
if (args.Contains("--webrtc")) { WebRTCTests.Run(); return; }
if (args.Contains("--webrtc-close-error")) { WebRTCTests.CloseError(); return; }
if (args.Contains("--ktx-production-memory")) { NativeKTXMemoryTests.RunProduction(); return; }
if (args.Contains("--ktx-memory")) { NativeKTXMemoryTests.Run(); return; }
if (args.Contains("--utf8-legacy")) { NativeUTF8LegacyTests.Run(); return; }
if (args.Contains("--utf8-memory")) { NativeUTF8StringTests.Run(); NativeUTF8StringTests.RunCallers(); return; }
if (args.Contains("--enet-allocation")) { ENetAllocationTests.Run(); return; }
if (args.Contains("--gzip-cleanup")) { GZIPCleanupTests.Run(args.Contains("--native-probe")); return; }
if (args.Contains("--freetype-allocation")) { NativeFontAllocationTests.Run(); return; }
if (args.Contains("--native-allocation")) { NativeAllocationTests.Run(); return; }
ContractChecks.Run();
EngineMetadataChecks.Run();
CanvasVertexLayoutTests.Run();
