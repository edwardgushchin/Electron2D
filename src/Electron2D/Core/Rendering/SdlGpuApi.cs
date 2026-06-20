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

    public SdlGpuDeviceHandle CreateDevice(bool debugMode, out string? error)
    {
        var device = SDL.CreateGPUDevice(ShaderFormats, debugMode, name: null!);
        error = device == 0 ? ReadError("SDL_CreateGPUDevice failed.") : null;
        return new SdlGpuDeviceHandle(device);
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

    private static string ReadError(string fallback)
    {
        var error = SDL.GetError();
        return string.IsNullOrWhiteSpace(error) ? fallback : error;
    }
}
