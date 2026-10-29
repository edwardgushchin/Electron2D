# Multisample shader sampling consumer

The public-only consumer creates MSAA color/depth targets, writes distinct samples, then reads them through compute and graphics programs. It checks both authoring languages, array layers, descriptor arrays, component views, integer formats, queries, feedback, lifetime and prepared owner-thread managed allocation.

Source execution needs a matching installed ShaderImport tool and the native image/view adapter. The exercised profile requires native four-sample color/depth/integer support.

```sh
ELECTRON2D_SHADER_IMPORT_TOOL=/path/to/Electron2D.ShaderImport ELECTRON2D_MS_SAMPLING_OUTPUT=/tmp/ms-shaders dotnet run -c Release --project tests/MultisampleSampling.Consumer
dotnet run -c Release --project tests/MultisampleSampling.Consumer -- /tmp/ms-shaders
dotnet publish tests/MultisampleSampling.Consumer -c Release -r linux-x64 -p:PublishAot=true -p:IlcTreatWarningsAsErrors=true -p:TrimmerTreatWarningsAsErrors=true -o /tmp/ms-consumer
/tmp/ms-consumer/MultisampleSampling.Consumer /tmp/ms-shaders
```

Binary replay uses the authored shader containers without starting a compiler. Package mode uses the standard Electron2DUsePackage properties. Native/driver allocation, other platforms and owner visual acceptance remain separate; the [component](../../docs/components/local-compute.md#multisample-shader-reads) owns the precise contract and limits.
