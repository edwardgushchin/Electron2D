# SceneConnectionBinding<TPublisher, TSubscriber, TBindings>

## Authoring-event rollback

A successful subscription requests the selected publisher's EditorStateChanged after retaining the typed connection. If that callback fails, the new record and token are removed without a second authoring event. Subscription and rollback errors are preserved together. Manual token disposal emits once; endpoint lifetime cleanup and consumed one-shot connections emit no disconnect event.

## Overview

A compiled typed definition of one authored subscription between Node roles. Create it through `ResourceFileTypes.RegisterSceneConnection`; its stable ID selects the same compiled factory during file loading. No delegate or original Node is stored in a packed scene.

## Syntax

```csharp
public sealed class SceneConnectionBinding<TPublisher, TSubscriber, TBindings>
    where TPublisher : Node where TSubscriber : Node
```

## Member reference

`EventConnection Subscribe(TPublisher publisher, TSubscriber subscriber, TBindings bindings)` subscribes immediately using a copied typed payload. Both endpoints retain cleanup; caller disposal also removes authoring identity. Active tree endpoints require the owner thread. Duplicate calls create separate native subscriptions. Disposed or consumed one-shot tokens are omitted by Pack.

Bound resources follow the ordinary packed resource graph, including aliases, scene-local duplication and file ownership. Value payloads require the existing portable codecs; arbitrary Nodes and delegate payloads are rejected. Factory code uses typed event accessors and handles omitted arguments directly. Diagnostic labels and omitted argument count describe that adapter; delivery policy is checked against its returned EventConnection.

See [packed scenes](../components/packed-scenes.md) for capture selection, reconstruction order, file limits and verification.
