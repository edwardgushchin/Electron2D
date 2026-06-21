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
namespace Electron2D;

internal enum SdlGpuDeviceProfile
{
    Standard = 0,
    AndroidMobile = 1
}

internal readonly struct SdlGpuOptionalFeaturePolicy
{
    public SdlGpuOptionalFeaturePolicy(
        bool clipDistance,
        bool depthClamping,
        bool indirectDrawFirstInstance,
        bool anisotropy)
    {
        ClipDistance = clipDistance;
        DepthClamping = depthClamping;
        IndirectDrawFirstInstance = indirectDrawFirstInstance;
        Anisotropy = anisotropy;
    }

    public bool ClipDistance { get; }

    public bool DepthClamping { get; }

    public bool IndirectDrawFirstInstance { get; }

    public bool Anisotropy { get; }

    public static SdlGpuOptionalFeaturePolicy Standard => new(
        clipDistance: true,
        depthClamping: true,
        indirectDrawFirstInstance: true,
        anisotropy: true);

    public static SdlGpuOptionalFeaturePolicy AndroidMobile => new(
        clipDistance: false,
        depthClamping: false,
        indirectDrawFirstInstance: false,
        anisotropy: false);
}

internal readonly struct SdlGpuDeviceCreateInfo
{
    private SdlGpuDeviceCreateInfo(
        SdlGpuDeviceProfile profile,
        bool debugMode,
        SdlGpuOptionalFeaturePolicy optionalFeatures)
    {
        Profile = profile;
        DebugMode = debugMode;
        OptionalFeatures = optionalFeatures;
    }

    public SdlGpuDeviceProfile Profile { get; }

    public bool DebugMode { get; }

    public SdlGpuOptionalFeaturePolicy OptionalFeatures { get; }

    public static SdlGpuDeviceCreateInfo Standard(bool debugMode)
    {
        return new SdlGpuDeviceCreateInfo(
            SdlGpuDeviceProfile.Standard,
            debugMode,
            SdlGpuOptionalFeaturePolicy.Standard);
    }

    public static SdlGpuDeviceCreateInfo AndroidMobile(bool debugMode)
    {
        return new SdlGpuDeviceCreateInfo(
            SdlGpuDeviceProfile.AndroidMobile,
            debugMode,
            SdlGpuOptionalFeaturePolicy.AndroidMobile);
    }
}
