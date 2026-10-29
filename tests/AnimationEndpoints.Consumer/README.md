# Custom animation endpoint consumer

The public API consumer runs 3564 independent interval vectors through actual custom graph nodes, typed method callbacks and nested animation playback. It also checks seek/test-only/weight suppression, deferred delivery, mutation/disposal, callback errors, finite guards and prepared caller-thread managed allocation.

```sh
dotnet run -c Release --project tests/AnimationEndpoints.Consumer
dotnet run -c Release --project tests/AnimationEndpoints.Consumer -- sparse
SDL_AUDIODRIVER=dummy dotnet run -c Release --project tests/AnimationEndpoints.Consumer -- audio-linear
SDL_AUDIODRIVER=dummy dotnet run -c Release --project tests/AnimationEndpoints.Consumer -- audio-pingpong
SDL_AUDIODRIVER=dummy dotnet run -c Release --project tests/AnimationEndpoints.Consumer -- audio-none
dotnet publish tests/AnimationEndpoints.Consumer -c Release -r linux-x64 -p:PublishAot=true -p:IlcTreatWarningsAsErrors=true -p:TrimmerTreatWarningsAsErrors=true -o /tmp/e2d-animation-endpoints
```

The `sparse` profile measures 4000 empty intervals in a 20,000-key nested track and checks zero warmed caller-thread managed allocation. Its timing is diagnostic, with no machine-dependent pass threshold. Run the same five profiles against the published executable. Audio profiles use the actual native callback and public capture effect; dummy output does not establish physical listening. The consumer uses only the public engine API. Package runs use the standard `Electron2DUsePackage` properties. [The component](../../docs/components/scene-animation.md#custom-clip-event-boundaries) owns behavior and verification limits; [oracle provenance](../Electron2D.Tests/Fixtures/AnimationEndpoints/PROVENANCE.md) owns source identities and fixture reproduction.
