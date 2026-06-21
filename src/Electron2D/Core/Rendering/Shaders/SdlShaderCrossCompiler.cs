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
using System.Runtime.InteropServices;
using System.Text;
using SDL3;

namespace Electron2D;

internal sealed class SdlShaderCrossCompiler : ICanvasShaderCompiler
{
    public CanvasShaderCompileResult Compile(CanvasShaderCompileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ShaderCross.Init())
        {
            return CanvasShaderCompileResult.Failure(ReadError("SDL_shadercross initialization failed."));
        }

        try
        {
            return request.TargetPlatform switch
            {
                CanvasShaderTargetPlatform.Windows => CompileDxil(request),
                CanvasShaderTargetPlatform.Linux => CompileSpirv(request),
                CanvasShaderTargetPlatform.Android => CompileSpirv(request),
                CanvasShaderTargetPlatform.MacOS => CompileMsl(request),
                CanvasShaderTargetPlatform.Ios => CompileMsl(request),
                _ => CanvasShaderCompileResult.Failure($"Unsupported shader target platform: {request.TargetPlatform}.")
            };
        }
        finally
        {
            ShaderCross.Quit();
        }
    }

    private static CanvasShaderCompileResult CompileSpirv(CanvasShaderCompileRequest request)
    {
        var info = CreateHlslInfo(request);
        UIntPtr size = 0;
        var bytecode = ShaderCross.CompileSPIRVFromHLSL(ref info, out size);
        if (bytecode == IntPtr.Zero)
        {
            return CanvasShaderCompileResult.Failure(ReadError("SDL_shadercross SPIR-V compilation failed."));
        }

        try
        {
            return CanvasShaderCompileResult.FromBytecode(CopyBytes(bytecode, size));
        }
        finally
        {
            SDL.Free(bytecode);
        }
    }

    private static CanvasShaderCompileResult CompileDxil(CanvasShaderCompileRequest request)
    {
        var info = CreateHlslInfo(request);
        UIntPtr size = 0;
        var bytecode = ShaderCross.CompileDXILFromHLSL(in info, out size);
        if (bytecode == IntPtr.Zero)
        {
            return CanvasShaderCompileResult.Failure(ReadError("SDL_shadercross DXIL compilation failed."));
        }

        try
        {
            return CanvasShaderCompileResult.FromBytecode(CopyBytes(bytecode, size));
        }
        finally
        {
            SDL.Free(bytecode);
        }
    }

    private static CanvasShaderCompileResult CompileMsl(CanvasShaderCompileRequest request)
    {
        var info = CreateHlslInfo(request);
        UIntPtr size = 0;
        var spirv = ShaderCross.CompileSPIRVFromHLSL(ref info, out size);
        if (spirv == IntPtr.Zero)
        {
            return CanvasShaderCompileResult.Failure(ReadError("SDL_shadercross SPIR-V compilation for MSL failed."));
        }

        IntPtr msl = IntPtr.Zero;
        try
        {
            var spirvInfo = new ShaderCross.SPIRVInfo
            {
                ByteCode = spirv,
                ByteCodeSize = size,
                ManagedEntrypoint = request.EntryPoint,
                ShaderStage = ToSdlStage(request.Stage),
                Props = 0
            };

            msl = ShaderCross.TranspileMSLFromSPIRV(in spirvInfo);
            if (msl == IntPtr.Zero)
            {
                return CanvasShaderCompileResult.Failure(ReadError("SDL_shadercross MSL transpilation failed."));
            }

            var text = Marshal.PtrToStringUTF8(msl) ?? string.Empty;
            return CanvasShaderCompileResult.FromBytecode(Encoding.UTF8.GetBytes(text));
        }
        finally
        {
            if (msl != IntPtr.Zero)
            {
                SDL.Free(msl);
            }

            SDL.Free(spirv);
        }
    }

    private static ShaderCross.HLSLInfo CreateHlslInfo(CanvasShaderCompileRequest request)
    {
        return new ShaderCross.HLSLInfo
        {
            ManagedSource = request.Source,
            ManagedEntrypoint = request.EntryPoint,
            ManagedIncludeDir = null,
            Defines = IntPtr.Zero,
            ShaderStage = ToSdlStage(request.Stage),
            Props = 0
        };
    }

    private static ShaderCross.ShaderStage ToSdlStage(CanvasShaderStage stage)
    {
        return stage switch
        {
            CanvasShaderStage.Vertex => ShaderCross.ShaderStage.Vertex,
            CanvasShaderStage.Fragment => ShaderCross.ShaderStage.Fragment,
            _ => ShaderCross.ShaderStage.Vertex
        };
    }

    private static byte[] CopyBytes(IntPtr source, UIntPtr size)
    {
        var length = checked((int)size);
        var data = new byte[length];
        Marshal.Copy(source, data, 0, length);
        return data;
    }

    private static string ReadError(string fallback)
    {
        var error = SDL.GetError();
        return string.IsNullOrWhiteSpace(error) ? fallback : error;
    }
}
