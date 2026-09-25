using System.Runtime.InteropServices;
using System.Text;
using Android.Util;
using SDL = SDL3.SDL;

namespace Electron2DAndroidProbe;

internal static class Gles2Probe
{
    private const uint VertexShader = 0x8B31, FragmentShader = 0x8B30, CompileStatus = 0x8B81, LinkStatus = 0x8B82;
    private const uint ArrayBuffer = 0x8892, StaticDraw = 0x88E4, Float = 0x1406, Triangles = 0x0004;
    private const uint ColorBufferBit = 0x4000, Rgba = 0x1908, UnsignedByte = 0x1401;

    internal static void Run()
    {
        if (!SDL.InitSubSystem(SDL.InitFlags.Video) ||
            !SDL.GLSetAttribute(SDL.GLAttr.ContextProfileMask, (int)SDL.GLProfile.ES) ||
            !SDL.GLSetAttribute(SDL.GLAttr.ContextMajorVersion, 2) ||
            !SDL.GLSetAttribute(SDL.GLAttr.ContextMinorVersion, 0))
            throw new InvalidOperationException(SDL.GetError());
        var window = SDL.CreateWindow("Electron2D GLES2 shader probe", 640, 360, SDL.WindowFlags.OpenGL);
        if (window == 0) throw new InvalidOperationException(SDL.GetError());
        var context = SDL.GLCreateContext(window);
        if (context == 0) throw new InvalidOperationException(SDL.GetError());
        try
        {
            var vertex = Compile(VertexShader, "attribute vec2 position; void main(){ gl_Position = vec4(position, 0.0, 1.0); }");
            var fragment = Compile(FragmentShader, "precision mediump float; void main(){ gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0); }");
            try
            {
                var program = glCreateProgram();
                glAttachShader(program, vertex);
                glAttachShader(program, fragment);
                glBindAttribLocation(program, 0, "position");
                glLinkProgram(program);
                glGetProgramiv(program, LinkStatus, out var linked);
                if (linked == 0) throw new InvalidOperationException("GLES2 program link failed.");
                try
                {
                    if (!SDL.GetWindowSizeInPixels(window, out var width, out var height))
                        throw new InvalidOperationException(SDL.GetError());
                    glViewport(0, 0, width, height);
                    glClearColor(0, 0, 0, 1);
                    glClear(ColorBufferBit);
                    glUseProgram(program);
                    float[] vertices = [-1, -1, 3, -1, -1, 3];
                    glGenBuffers(1, out var buffer);
                    try
                    {
                        glBindBuffer(ArrayBuffer, buffer);
                        glBufferData(ArrayBuffer, vertices.Length * sizeof(float), vertices, StaticDraw);
                        glVertexAttribPointer(0, 2, Float, 0, 0, 0);
                        glEnableVertexAttribArray(0);
                        glDrawArrays(Triangles, 0, 3);
                        byte[] pixel = new byte[4];
                        glReadPixels(4, 4, 1, 1, Rgba, UnsignedByte, pixel);
                        Log.Info("Electron2DProbe", $"GLES2_SHADER_PIXEL {pixel[0]},{pixel[1]},{pixel[2]},{pixel[3]} ERROR {glGetError()}");
                        if (pixel[0] < 240 || pixel[1] > 15 || pixel[2] > 15)
                            throw new InvalidOperationException("GLES2 fragment shader did not draw red.");
                        if (!SDL.GLSwapWindow(window)) throw new InvalidOperationException(SDL.GetError());
                    }
                    finally { glDeleteBuffers(1, ref buffer); }
                }
                finally { glDeleteProgram(program); }
            }
            finally { glDeleteShader(vertex); glDeleteShader(fragment); }
        }
        finally { SDL.GLDestroyContext(context); SDL.DestroyWindow(window); SDL.QuitSubSystem(SDL.InitFlags.Video); }
    }

    private static uint Compile(uint type, string source)
    {
        var shader = glCreateShader(type);
        var bytes = Encoding.UTF8.GetBytes(source + '\0');
        var data = Marshal.AllocHGlobal(bytes.Length);
        var pointers = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            Marshal.Copy(bytes, 0, data, bytes.Length);
            Marshal.WriteIntPtr(pointers, data);
            glShaderSource(shader, 1, pointers, 0);
            glCompileShader(shader);
            glGetShaderiv(shader, CompileStatus, out var compiled);
            if (compiled == 0) throw new InvalidOperationException("GLES2 shader compile failed.");
            return shader;
        }
        finally { Marshal.FreeHGlobal(pointers); Marshal.FreeHGlobal(data); }
    }

    [DllImport("libGLESv2.so")] private static extern uint glCreateShader(uint type);
    [DllImport("libGLESv2.so")] private static extern void glShaderSource(uint shader, int count, nint strings, nint lengths);
    [DllImport("libGLESv2.so")] private static extern void glCompileShader(uint shader);
    [DllImport("libGLESv2.so")] private static extern void glGetShaderiv(uint shader, uint pname, out int value);
    [DllImport("libGLESv2.so")] private static extern void glDeleteShader(uint shader);
    [DllImport("libGLESv2.so")] private static extern uint glCreateProgram();
    [DllImport("libGLESv2.so")] private static extern void glAttachShader(uint program, uint shader);
    [DllImport("libGLESv2.so")] private static extern void glBindAttribLocation(uint program, uint index, string name);
    [DllImport("libGLESv2.so")] private static extern void glLinkProgram(uint program);
    [DllImport("libGLESv2.so")] private static extern void glGetProgramiv(uint program, uint pname, out int value);
    [DllImport("libGLESv2.so")] private static extern void glUseProgram(uint program);
    [DllImport("libGLESv2.so")] private static extern void glDeleteProgram(uint program);
    [DllImport("libGLESv2.so")] private static extern void glViewport(int x, int y, int width, int height);
    [DllImport("libGLESv2.so")] private static extern void glClearColor(float r, float g, float b, float a);
    [DllImport("libGLESv2.so")] private static extern void glClear(uint mask);
    [DllImport("libGLESv2.so")] private static extern void glGenBuffers(int n, out uint buffer);
    [DllImport("libGLESv2.so")] private static extern void glBindBuffer(uint target, uint buffer);
    [DllImport("libGLESv2.so")] private static extern void glBufferData(uint target, int size, float[] data, uint usage);
    [DllImport("libGLESv2.so")] private static extern void glDeleteBuffers(int n, ref uint buffer);
    [DllImport("libGLESv2.so")] private static extern void glVertexAttribPointer(uint index, int size, uint type, byte normalized, int stride, nint pointer);
    [DllImport("libGLESv2.so")] private static extern void glEnableVertexAttribArray(uint index);
    [DllImport("libGLESv2.so")] private static extern void glDrawArrays(uint mode, int first, int count);
    [DllImport("libGLESv2.so")] private static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, byte[] pixels);
    [DllImport("libGLESv2.so")] private static extern uint glGetError();
}
