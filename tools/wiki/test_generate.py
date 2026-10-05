#!/usr/bin/env python3
"""Small regression checks for wiki member rendering."""

from xml.etree import ElementTree as ET

from generate import member_heading, plain, xml_id


assert plain(ET.fromstring('<returns><see langword="true"/> when <paramref name="other"/> overlaps.</returns>')) == "true when other overlaps."
assert member_heading({"kind": "constructor", "declaringType": "Electron2D.Rect2i", "name": ".ctor",
                       "parameters": [{"type": "System.Int32"}]}) == "Rect2i(System.Int32)"

assert xml_id({"id": "method:Electron2D.Animation.AddBezierTrack`2(PropertyDescriptor<TOwner, TValue>,System.Int32)", "kind": "method", "declaringType": "Electron2D.Animation", "name": "AddBezierTrack", "signature": "public int AddBezierTrack<TOwner, TValue>(...)"}) == "M:Electron2D.Animation.AddBezierTrack``2(Electron2D.PropertyDescriptor{``0,``1},System.Int32)"
assert xml_id({"id": "constructor:Electron2D.AnimationMethodKey<TOwner, TArguments>..ctor(System.String,Action<TOwner, TArguments>,TArguments)", "kind": "constructor", "declaringType": "Electron2D.AnimationMethodKey<TOwner, TArguments>", "name": ".ctor", "signature": ""}) == "M:Electron2D.AnimationMethodKey`2.#ctor(System.String,System.Action{`0,`1},`1)"

assert xml_id({"id": "method:Electron2D.Vector2.Deconstruct(ref System.Single,ref System.Single)", "kind": "method", "declaringType": "Electron2D.Vector2", "name": "Deconstruct", "signature": ""}) == "M:Electron2D.Vector2.Deconstruct(System.Single@,System.Single@)"

assert xml_id({"id": "method:Electron2D.JSON.FromNative`1(T,JsonTypeInfo<T>)", "kind": "method", "declaringType": "Electron2D.JSON", "name": "FromNative", "signature": "public static JsonNode FromNative<T>(T value, JsonTypeInfo<T> typeInfo)"}) == "M:Electron2D.JSON.FromNative``1(``0,System.Text.Json.Serialization.Metadata.JsonTypeInfo{``0})"
assert xml_id({"id": "constructor:Electron2D.ConfigKey<T>..ctor(System.String,System.String,JsonTypeInfo<T>)", "kind": "constructor", "declaringType": "Electron2D.ConfigKey<T>", "name": ".ctor", "signature": ""}) == "M:Electron2D.ConfigKey`1.#ctor(System.String,System.String,System.Text.Json.Serialization.Metadata.JsonTypeInfo{`0})"
