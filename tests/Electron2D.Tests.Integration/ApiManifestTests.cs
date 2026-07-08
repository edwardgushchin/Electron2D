/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ApiManifestTests
{
    [Fact]
    public void ApiManifestDescribesCompiledPublicRuntimeSurface()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "api", "electron2d-api-manifest.json");
        var profilePath = Path.Combine(root, "data", "api", "electron2d-public-api-profile.json");

        Assert.True(File.Exists(manifestPath), $"API manifest was not found: {manifestPath}");
        Assert.True(File.Exists(profilePath), $"Manual public API profile was not found: {profilePath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var rootElement = document.RootElement;

        Assert.Equal(1, rootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("0.1-preview", rootElement.GetProperty("manifestVersion").GetString());
        Assert.Equal("Electron2D 0.1-preview", rootElement.GetProperty("profileName").GetString());
        Assert.Equal("4.7-stable", rootElement.GetProperty("godotBaseline").GetString());

        var generatedFrom = rootElement.GetProperty("generatedFrom");
        Assert.EndsWith("src/Electron2D/bin/Debug/net10.0/Electron2D.dll", generatedFrom.GetProperty("compiledAssembly").GetString(), StringComparison.Ordinal);
        Assert.EndsWith("data/api/electron2d-public-api-profile.json", generatedFrom.GetProperty("publicApiProfile").GetString(), StringComparison.Ordinal);
        Assert.False(generatedFrom.TryGetProperty("compatibilityPage", out _));
        Assert.EndsWith("Electron2D.xml", generatedFrom.GetProperty("xmlDocumentation").GetString(), StringComparison.Ordinal);

        var manifestTypes = rootElement.GetProperty("types")
            .EnumerateArray()
            .Select(type => type.GetProperty("fullName").GetString())
            .ToArray();
        var runtimeTypes = typeof(Electron2D.Object).Assembly.GetExportedTypes()
            .Where(type => type.Assembly == typeof(Electron2D.Object).Assembly)
            .Select(DisplayName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(runtimeTypes, manifestTypes);
    }

    [Fact]
    public void ApiManifestCarriesStableIdentifiersAndProfileStatus()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "api", "electron2d-api-manifest.json");

        Assert.True(File.Exists(manifestPath), $"API manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var rootElement = document.RootElement;

        var summary = rootElement.GetProperty("strictParitySummary");
        Assert.Equal(0, summary.GetProperty("missingTypes").GetInt32());
        Assert.Equal(0, summary.GetProperty("missingMembers").GetInt32());
        Assert.Equal(0, summary.GetProperty("signatureMismatches").GetInt32());
        Assert.Equal(0, summary.GetProperty("inheritanceMismatches").GetInt32());
        Assert.Equal(0, summary.GetProperty("defaultMismatches").GetInt32());
        Assert.Equal(0, summary.GetProperty("unexpectedChanges").GetInt32());

        var node = FindType(rootElement, "Electron2D.Node");
        Assert.Equal("electron2d://api/type/Electron2D.Node", node.GetProperty("id").GetString());
        Assert.Equal("Electron2D.Object", node.GetProperty("baseType").GetString());
        Assert.Contains(node.GetProperty("members").EnumerateArray(), member =>
            member.GetProperty("kind").GetString() == "Property" &&
            member.GetProperty("name").GetString() == "Name" &&
            member.GetProperty("id").GetString()!.StartsWith("electron2d://api/member/Electron2D.Node/Property/", StringComparison.Ordinal));
        Assert.Contains(node.GetProperty("members").EnumerateArray(), member =>
            member.GetProperty("kind").GetString() == "Method" &&
            member.GetProperty("name").GetString() == "AddChild" &&
            member.GetProperty("id").GetString()!.StartsWith("electron2d://api/member/Electron2D.Node/Method/", StringComparison.Ordinal));

        var control = FindType(rootElement, "Electron2D.Control");
        var profile = control.GetProperty("profile");
        Assert.Equal("unapproved", profile.GetProperty("status").GetString());
        Assert.Equal("not_verified", profile.GetProperty("parity").GetString());
        Assert.True(profile.GetProperty("outOfProfile").GetBoolean());

        var textureRect = FindType(rootElement, "Electron2D.TextureRect");
        var textureRectMembers = textureRect.GetProperty("members").EnumerateArray().ToArray();
        var stretchMode = textureRectMembers.Single(member =>
            member.GetProperty("kind").GetString() == "Property" &&
            member.GetProperty("name").GetString() == "StretchMode");
        Assert.Equal("Electron2D.TextureRect.StretchMode", stretchMode.GetProperty("returnType").GetString());
        Assert.DoesNotContain("StretchModeEnum", stretchMode.GetProperty("signature").GetString(), StringComparison.Ordinal);

        var draw = textureRectMembers.Single(member =>
            member.GetProperty("kind").GetString() == "Method" &&
            member.GetProperty("xmlDocId").GetString() == "M:Electron2D.TextureRect._Draw");
        Assert.Equal("Draw", draw.GetProperty("name").GetString());
        Assert.Equal("public System.Void Draw()", draw.GetProperty("signature").GetString());
        Assert.DoesNotContain(textureRectMembers, member => member.GetProperty("name").GetString() == "_Draw");

        var stretchModeType = FindType(rootElement, "Electron2D.TextureRect.StretchMode");
        Assert.Equal("TextureRect.StretchMode", stretchModeType.GetProperty("name").GetString());
        Assert.DoesNotContain(rootElement.GetProperty("types").EnumerateArray(), type =>
            type.GetProperty("fullName").GetString() == "Electron2D.TextureRect.StretchModeEnum");
        Assert.DoesNotContain(stretchModeType.GetProperty("members").EnumerateArray(), member =>
            member.GetProperty("name").GetString() == "value__");

        var vector2 = FindType(rootElement, "Electron2D.Vector2");
        var vector2Members = vector2.GetProperty("members").EnumerateArray().ToArray();
        Assert.Contains(vector2Members, member =>
            member.GetProperty("kind").GetString() == "Operator" &&
            member.GetProperty("xmlDocId").GetString() == "M:Electron2D.Vector2.op_Addition(Electron2D.Vector2,Electron2D.Vector2)");
        AssertMemberValue(vector2Members, "Field", "Zero", "(0, 0)");
        AssertMemberValue(vector2Members, "Field", "One", "(1, 1)");

        var color = FindType(rootElement, "Electron2D.Color");
        var colorMembers = color.GetProperty("members").EnumerateArray().ToArray();
        AssertMemberValue(colorMembers, "Field", "White", "(1, 1, 1, 1)");

        var math = FindType(rootElement, "Electron2D.Mathf");
        var mathMembers = math.GetProperty("members").EnumerateArray().ToArray();
        AssertMemberValue(mathMembers, "Constant", "Epsilon", "1E-05");
        AssertMemberValue(mathMembers, "Constant", "Pi", "3.1415927");
        AssertMemberValue(mathMembers, "Constant", "Tau", "6.2831855");

        var resourceUid = FindType(rootElement, "Electron2D.ResourceUid");
        AssertMemberValue(resourceUid.GetProperty("members").EnumerateArray().ToArray(), "Constant", "InvalidId", "-1");

        var popupMenu = FindType(rootElement, "Electron2D.PopupMenu");
        var setItemChecked = popupMenu.GetProperty("members").EnumerateArray()
            .Single(member => member.GetProperty("name").GetString() == "SetItemChecked");
        Assert.Contains("System.Boolean @checked", setItemChecked.GetProperty("signature").GetString(), StringComparison.Ordinal);

        var tween = FindType(rootElement, "Electron2D.Tween");
        var tweenProperty = tween.GetProperty("members").EnumerateArray()
            .Single(member => member.GetProperty("name").GetString() == "TweenProperty");
        Assert.Contains("Electron2D.Object @object", tweenProperty.GetProperty("signature").GetString(), StringComparison.Ordinal);

        AssertMemberValue(stretchModeType.GetProperty("members").EnumerateArray().ToArray(), "EnumValue", "Keep", "2");
    }

    private static JsonElement FindType(JsonElement rootElement, string fullName)
    {
        foreach (var type in rootElement.GetProperty("types").EnumerateArray())
        {
            if (type.GetProperty("fullName").GetString() == fullName)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"API manifest does not contain type {fullName}.");
    }

    private static void AssertMemberValue(JsonElement[] members, string kind, string name, string expectedValue)
    {
        var member = members.Single(item =>
            item.GetProperty("kind").GetString() == kind &&
            item.GetProperty("name").GetString() == name);
        Assert.True(member.TryGetProperty("value", out var value), $"{kind} {name} must include a value.");
        Assert.Equal(expectedValue, value.GetString());
    }

    private static string DisplayName(Type type) => ProjectTypeDisplayName((type.FullName ?? type.Name).Replace('+', '.'), type.IsEnum);

    private static string ProjectTypeDisplayName(string displayName, bool isEnum)
    {
        const string rootNamespace = "Electron2D.";
        if (!isEnum || !displayName.StartsWith(rootNamespace, StringComparison.Ordinal))
        {
            return displayName;
        }

        const string suffix = "Enum";
        var lastDot = displayName.LastIndexOf('.');
        var nameStart = lastDot < 0 ? 0 : lastDot + 1;
        return displayName.EndsWith(suffix, StringComparison.Ordinal) &&
            displayName.Length > nameStart + suffix.Length
            ? displayName[..^suffix.Length]
            : displayName;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
