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
using SDL3;

namespace Electron2D;

internal sealed class SdlGpuApi : ISdlGpuApi
{
    private const string MissingWindowHandleError =
        "SDL_GPU window claim requires a valid SDL_Window handle.";

    private static readonly SDL.GPUShaderFormat ShaderFormats =
        SDL.GPUShaderFormat.SPIRV
        | SDL.GPUShaderFormat.DXIL
        | SDL.GPUShaderFormat.MSL
        | SDL.GPUShaderFormat.MetalLib;

    public SdlGpuDeviceHandle CreateDevice(SdlGpuDeviceCreateInfo createInfo, out string? error)
    {
        var properties = SDL.CreateProperties();
        if (properties == 0)
        {
            error = ReadError("SDL_CreateProperties failed for SDL_GPU device creation.");
            return default;
        }

        try
        {
            if (!ConfigureDeviceProperties(properties, createInfo, out error))
            {
                return default;
            }

            var device = SDL.CreateGPUDeviceWithProperties(properties);
            error = device == 0 ? ReadError("SDL_CreateGPUDeviceWithProperties failed.") : null;
            return new SdlGpuDeviceHandle(device);
        }
        finally
        {
            SDL.DestroyProperties(properties);
        }
    }

    public bool ClaimWindow(SdlGpuDeviceHandle device, SdlGpuWindowInfo window, out string? error)
    {
        if (!device.IsValid)
        {
            error = "SDL_GPU device handle is invalid.";
            return false;
        }

        if (window.NativeWindowHandle == 0)
        {
            error = MissingWindowHandleError;
            return false;
        }

        var result = SDL.ClaimWindowForGPUDevice(device.Value, window.NativeWindowHandle);
        error = result ? null : ReadError("SDL_ClaimWindowForGPUDevice failed.");
        return result;
    }

    public SdlGpuDeviceInfo GetDeviceInfo(SdlGpuDeviceHandle device)
    {
        if (!device.IsValid)
        {
            return SdlGpuDeviceInfo.Unknown;
        }

        var properties = SDL.GetGPUDeviceProperties(device.Value);
        var driverName = SDL.GetGPUDeviceDriver(device.Value);
        if (properties == 0)
        {
            return new SdlGpuDeviceInfo(
                gpuName: "unknown",
                driverName: string.IsNullOrWhiteSpace(driverName) ? "unknown" : driverName,
                driverVersion: "unknown",
                driverInfo: "unknown");
        }

        return new SdlGpuDeviceInfo(
            SDL.GetStringProperty(properties, SDL.Props.GPUDeviceNameString, "unknown"),
            string.IsNullOrWhiteSpace(driverName)
                ? SDL.GetStringProperty(properties, SDL.Props.GPUDeviceDriverNameString, "unknown")
                : driverName,
            SDL.GetStringProperty(properties, SDL.Props.GPUDeviceDriverVersionString, "unknown"),
            SDL.GetStringProperty(properties, SDL.Props.GPUDeviceDriverInfoString, "unknown"));
    }

    public bool ValidateTextureSmoke(SdlGpuDeviceHandle device, out string? error)
    {
        if (!device.IsValid)
        {
            error = "SDL_GPU texture smoke requires a valid device handle.";
            return false;
        }

        var supported = SDL.GPUTextureSupportsFormat(
            device.Value,
            SDL.GPUTextureFormat.R8G8B8A8Unorm,
            SDL.GPUTextureType.TextureType2D,
            SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ColorTarget);
        error = supported ? null : ReadError("SDL_GPU RGBA8 texture smoke failed.");
        return supported;
    }

    public bool ValidatePipelineSmoke(SdlGpuDeviceHandle device, out string? error)
    {
        if (!device.IsValid)
        {
            error = "SDL_GPU pipeline smoke requires a valid device handle.";
            return false;
        }

        var formats = SDL.GetGPUShaderFormats(device.Value);
        var supported = (formats & ShaderFormats) != 0;
        error = supported ? null : "SDL_GPU pipeline smoke found no supported shader format.";
        return supported;
    }

    public SdlGpuCommandBufferHandle AcquireCommandBuffer(SdlGpuDeviceHandle device, out string? error)
    {
        if (!device.IsValid)
        {
            error = "SDL_GPU device handle is invalid.";
            return default;
        }

        var commandBuffer = SDL.AcquireGPUCommandBuffer(device.Value);
        error = commandBuffer == 0 ? ReadError("SDL_AcquireGPUCommandBuffer failed.") : null;
        return new SdlGpuCommandBufferHandle(commandBuffer);
    }

    public bool SubmitCommandBuffer(SdlGpuCommandBufferHandle commandBuffer, out string? error)
    {
        if (!commandBuffer.IsValid)
        {
            error = "SDL_GPU command buffer handle is invalid.";
            return false;
        }

        var result = SDL.SubmitGPUCommandBuffer(commandBuffer.Value);
        error = result ? null : ReadError("SDL_SubmitGPUCommandBuffer failed.");
        return result;
    }

    public void DestroyDevice(SdlGpuDeviceHandle device)
    {
        if (device.IsValid)
        {
            SDL.DestroyGPUDevice(device.Value);
        }
    }

    private static bool ConfigureDeviceProperties(
        uint properties,
        SdlGpuDeviceCreateInfo createInfo,
        out string? error)
    {
        return SetBoolean(properties, SDL.Props.GPUDeviceCreateDebugModeBoolean, createInfo.DebugMode, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateShadersSPIRVBoolean, (ShaderFormats & SDL.GPUShaderFormat.SPIRV) != 0, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateShadersDXILBoolean, (ShaderFormats & SDL.GPUShaderFormat.DXIL) != 0, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateShadersMSLBoolean, (ShaderFormats & SDL.GPUShaderFormat.MSL) != 0, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateShadersMetalLibBoolean, (ShaderFormats & SDL.GPUShaderFormat.MetalLib) != 0, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateFeatureClipDistanceBoolean, createInfo.OptionalFeatures.ClipDistance, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateFeatureDepthClampingBoolean, createInfo.OptionalFeatures.DepthClamping, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateFeatureIndirectDrawFirstInstanceBoolean, createInfo.OptionalFeatures.IndirectDrawFirstInstance, out error) &&
            SetBoolean(properties, SDL.Props.GPUDeviceCreateFeatureAnisotropyBoolean, createInfo.OptionalFeatures.Anisotropy, out error);
    }

    private static bool SetBoolean(uint properties, string name, bool value, out string? error)
    {
        if (SDL.SetBooleanProperty(properties, name, value))
        {
            error = null;
            return true;
        }

        error = ReadError($"SDL_SetBooleanProperty failed for {name}.");
        return false;
    }

    private static string ReadError(string fallback)
    {
        var error = SDL.GetError();
        return string.IsNullOrWhiteSpace(error) ? fallback : error;
    }
}
