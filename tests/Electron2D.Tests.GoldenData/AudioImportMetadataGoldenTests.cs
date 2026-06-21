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
using Xunit;

namespace Electron2D.Tests.GoldenData;

public sealed class AudioImportMetadataGoldenTests
{
    [Fact]
    public void AudioImportMetadataTextSerializerMatchesGoldenText()
    {
        var metadata = new Electron2D.AudioImportMetadata(
            sourcePath: "res://audio/jump.wav",
            uid: 123456789L,
            format: Electron2D.AudioSourceFormat.Wav,
            mode: Electron2D.AudioImportMode.Static,
            sampleRate: 48_000,
            channelCount: 2,
            bitsPerSample: 16,
            sampleCount: 96_000L,
            lengthSeconds: 2f,
            loop: new Electron2D.AudioLoopMetadata(enabled: true, beginSeconds: 0.25f, endSeconds: 1.75f),
            platformPackages:
            [
                new Electron2D.AudioPlatformPackage("android", "streaming_asset"),
                new Electron2D.AudioPlatformPackage("desktop", "copy")
            ]);

        const string expected = "{\n" +
            "  \"format\": \"Electron2D.AudioImportMetadata\",\n" +
            "  \"version\": 1,\n" +
            "  \"source\": \"res://audio/jump.wav\",\n" +
            "  \"uid\": \"uid://21i3v9\",\n" +
            "  \"audioFormat\": \"Wav\",\n" +
            "  \"mode\": \"Static\",\n" +
            "  \"sampleRate\": 48000,\n" +
            "  \"channelCount\": 2,\n" +
            "  \"bitsPerSample\": 16,\n" +
            "  \"sampleCount\": 96000,\n" +
            "  \"length\": 2,\n" +
            "  \"loop\": {\n" +
            "    \"enabled\": true,\n" +
            "    \"begin\": 0.25,\n" +
            "    \"end\": 1.75\n" +
            "  },\n" +
            "  \"platforms\": [\n" +
            "    {\n" +
            "      \"name\": \"android\",\n" +
            "      \"packaging\": \"streaming_asset\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"name\": \"desktop\",\n" +
            "      \"packaging\": \"copy\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        Assert.Equal(expected, Electron2D.AudioImportMetadataTextSerializer.Serialize(metadata));
    }
}
