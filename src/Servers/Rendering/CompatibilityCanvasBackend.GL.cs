using SDL3;

namespace Electron2D;

internal sealed partial class CompatibilityCanvasBackend
{
    private unsafe delegate* unmanaged<uint, int, int, void> _lineDrawArrays;
    private unsafe delegate* unmanaged<uint> _lineGLError;
    private unsafe delegate* unmanaged<uint, uint, void> _lineBindBuffer;
    private unsafe delegate* unmanaged<int, uint*, void> _lineGenBuffers, _lineDeleteBuffers;
    private unsafe delegate* unmanaged<uint, nint, void*, uint, void> _lineBufferData;
    private unsafe delegate* unmanaged<uint, nint, nint, void*, void> _lineBufferSubData;
    private unsafe delegate* unmanaged<uint, void> _lineEnableArray, _lineDisableArray;
    private unsafe delegate* unmanaged<int, uint, int, void*, void> _lineVertexPointer, _lineColorPointer, _lineUVPointer;
    private unsafe delegate* unmanaged<uint, int, uint, byte, int, void*, void> _lineAttributePointer;
    private unsafe delegate* unmanaged<uint, int*, void> _lineGetInteger;
    private unsafe delegate* unmanaged<uint, byte*, int> _lineGetAttribute;
    private readonly Dictionary<uint, (int Position, int Color, int UV)> _lineAttributes = [];
    private const uint GLArrayBuffer = 0x8892, GLFloat = 0x1406, GLStreamDraw = 0x88E0, GLCurrentProgram = 0x8B8D;
    private const uint GLVertexArray = 0x8074, GLColorArray = 0x8076, GLTextureCoordinateArray = 0x8078;
    private const uint GLLineStrip = 3, GLLines = 1;
    private uint _lineBuffer;
    private int _lineBufferBytes;
    private nint _lineContext, _lineWindow;

    private static nint LineGLFunction(string name)
    {
        var pointer = SDL.GLGetProcAddress(name);
        if (pointer == 0) throw new NotSupportedException($"The compatibility driver does not expose {name}.");
        return pointer;
    }

    private unsafe void PrepareLineGL()
    {
        if (_lineDrawArrays != null) return;
        _lineGLError = (delegate* unmanaged<uint>)LineGLFunction("glGetError");
        _lineBindBuffer = (delegate* unmanaged<uint, uint, void>)LineGLFunction("glBindBuffer");
        if (Driver == "opengl")
        {
            _lineEnableArray = (delegate* unmanaged<uint, void>)LineGLFunction("glEnableClientState");
            _lineDisableArray = (delegate* unmanaged<uint, void>)LineGLFunction("glDisableClientState");
            _lineVertexPointer = (delegate* unmanaged<int, uint, int, void*, void>)LineGLFunction("glVertexPointer");
            _lineColorPointer = (delegate* unmanaged<int, uint, int, void*, void>)LineGLFunction("glColorPointer");
            _lineUVPointer = (delegate* unmanaged<int, uint, int, void*, void>)LineGLFunction("glTexCoordPointer");
        }
        else
        {
            _lineEnableArray = (delegate* unmanaged<uint, void>)LineGLFunction("glEnableVertexAttribArray");
            _lineDisableArray = (delegate* unmanaged<uint, void>)LineGLFunction("glDisableVertexAttribArray");
            _lineAttributePointer = (delegate* unmanaged<uint, int, uint, byte, int, void*, void>)LineGLFunction("glVertexAttribPointer");
            _lineGetInteger = (delegate* unmanaged<uint, int*, void>)LineGLFunction("glGetIntegerv");
            _lineGetAttribute = (delegate* unmanaged<uint, byte*, int>)LineGLFunction("glGetAttribLocation");
            _lineGenBuffers = (delegate* unmanaged<int, uint*, void>)LineGLFunction("glGenBuffers");
            _lineDeleteBuffers = (delegate* unmanaged<int, uint*, void>)LineGLFunction("glDeleteBuffers");
            _lineBufferData = (delegate* unmanaged<uint, nint, void*, uint, void>)LineGLFunction("glBufferData");
            _lineBufferSubData = (delegate* unmanaged<uint, nint, nint, void*, void>)LineGLFunction("glBufferSubData");
        }
        _lineDrawArrays = (delegate* unmanaged<uint, int, int, void>)LineGLFunction("glDrawArrays");
    }

    private unsafe int DrawGLAttributeLines(nint renderer, nint texture, ReadOnlySpan<SDL.Vertex> vertices, Mesh.PrimitiveType primitive, bool snap)
    {
        var connected = primitive == Mesh.PrimitiveType.LineStrip; var count = 0;
        if (_attributePoints.Length < vertices.Length) Array.Resize(ref _attributePoints, vertices.Length);
        var previous = default(SDL.FPoint); var previousSource = default(SDL.FPoint);
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i]; var source = LinePoint(vertex.Position, snap);
            if (!connected && i % 2 == 0 && EqualPoint(source, LinePoint(vertices[i + 1].Position, snap))) { i++; continue; }
            var first = connected ? i == 0 : i % 2 == 0;
            var point = new SDL.FPoint { X = source.X + .5f, Y = source.Y + .5f };
            if (!first)
            {
                if (EqualPoint(source, previousSource)) point = previous;
                else
                {
                    var angle = MathF.Atan2(point.Y - previous.Y, point.X - previous.X);
                    point.X += MathF.Cos(angle) * .25f; point.Y += MathF.Sin(angle) * .25f;
                }
            }
            previousSource = source; previous = point; vertex.Position = point; _attributePoints[count++] = vertex;
        }
        if (count < 2) return 0;
        // A degenerate SDL triangle prepares its shader/texture/blend/clip state without covering a pixel.
        Span<SDL.Vertex> prime = stackalloc SDL.Vertex[3]; prime.Clear();
        Check(SDL.RenderGeometry(renderer, texture, prime, 3, 0, 0), "prepare native attribute line state");
        Check(SDL.FlushRenderer(renderer), "flush SDL before native attribute lines");
        PrepareLineGL();
        var data = _attributePoints.AsSpan(0, count);
        if (Driver == "opengl" && texture != 0)
        {
            var properties = SDL.GetTextureProperties(texture);
            var width = SDL.GetFloatProperty(properties, SDL.Props.TextureOpenGLTexWFloat, 1);
            var height = SDL.GetFloatProperty(properties, SDL.Props.TextureOpenGLTexHFloat, 1);
            foreach (ref var vertex in data) { vertex.TexCoord.X *= width; vertex.TexCoord.Y *= height; }
        }
        fixed (SDL.Vertex* pointer = data)
        {
            if (Driver == "opengl")
            {
                _lineBindBuffer(GLArrayBuffer, 0);
                _lineEnableArray(GLVertexArray); _lineEnableArray(GLColorArray);
                if (texture != 0) _lineEnableArray(GLTextureCoordinateArray); else _lineDisableArray(GLTextureCoordinateArray);
                _lineVertexPointer(2, GLFloat, sizeof(SDL.Vertex), pointer);
                _lineColorPointer(4, GLFloat, sizeof(SDL.Vertex), (byte*)pointer + 8);
                if (texture != 0) _lineUVPointer(2, GLFloat, sizeof(SDL.Vertex), (byte*)pointer + 24);
                try { _lineDrawArrays(connected ? GLLineStrip : GLLines, 0, count); CheckLineGL("draw native attribute lines"); }
                finally { _lineDisableArray(GLVertexArray); _lineDisableArray(GLColorArray); _lineDisableArray(GLTextureCoordinateArray); }
            }
            else
            {
                if (_lineBuffer == 0)
                {
                    uint buffer = 0; _lineGenBuffers(1, &buffer); _lineBuffer = buffer;
                    CheckLineGL("create native attribute line buffer");
                    _lineContext = SDL.GLGetCurrentContext(); _lineWindow = SDL.GLGetCurrentWindow();
                    if (buffer == 0) throw new InvalidOperationException("The compatibility driver could not create its attribute line buffer.");
                }
                _lineBindBuffer(GLArrayBuffer, _lineBuffer);
                try
                {
                    var bytes = checked(count * sizeof(SDL.Vertex));
                    if (_lineBufferBytes < bytes) { _lineBufferData(GLArrayBuffer, bytes, pointer, GLStreamDraw); CheckLineGL("prepare native attribute line storage"); _lineBufferBytes = bytes; }
                    else _lineBufferSubData(GLArrayBuffer, 0, bytes, pointer);
                    var attributes = LineGLAttributes(texture != 0);
                    _lineEnableArray((uint)attributes.Position); _lineEnableArray((uint)attributes.Color);
                    if (attributes.UV >= 0) { if (texture != 0) _lineEnableArray((uint)attributes.UV); else _lineDisableArray((uint)attributes.UV); }
                    _lineAttributePointer((uint)attributes.Position, 2, GLFloat, 0, sizeof(SDL.Vertex), null);
                    _lineAttributePointer((uint)attributes.Color, 4, GLFloat, 0, sizeof(SDL.Vertex), (void*)8);
                    if (texture != 0) _lineAttributePointer((uint)attributes.UV, 2, GLFloat, 0, sizeof(SDL.Vertex), (void*)24);
                    try { _lineDrawArrays(connected ? GLLineStrip : GLLines, 0, count); CheckLineGL("draw native attribute lines"); }
                    finally { if (attributes.UV >= 0) _lineDisableArray((uint)attributes.UV); }
                }
                finally { _lineBindBuffer(GLArrayBuffer, 0); }
            }
        }
        return 2;
    }

    private unsafe (int Position, int Color, int UV) LineGLAttributes(bool textured)
    {
        int current; _lineGetInteger(GLCurrentProgram, &current); var program = (uint)current;
        if (!_lineAttributes.TryGetValue(program, out var attributes))
        {
            int Location(ReadOnlySpan<byte> name) { fixed (byte* pointer = name) return _lineGetAttribute(program, pointer); }
            attributes = (Location("a_position\0"u8), Location("a_color\0"u8), Location("a_texCoord\0"u8));
            if (attributes.Item1 < 0 || attributes.Item2 < 0) throw new NotSupportedException("The native compatibility program has an unsupported vertex interface.");
            _lineAttributes.Add(program, attributes);
        }
        if (textured && attributes.UV < 0) throw new NotSupportedException("The native compatibility program has no texture coordinate input.");
        return attributes;
    }

    private unsafe void CheckLineGL(string operation)
    {
        var error = _lineGLError();
        if (error != 0) throw new InvalidOperationException($"Cannot {operation}: OpenGL error 0x{error:X}.");
    }

    private unsafe void ReleaseLineGL()
    {
        var buffer = _lineBuffer; _lineBuffer = 0;
        // Destroying the renderer's context also retires storage when that context has already been lost.
        if (buffer != 0 && SDL.GLMakeCurrent(_lineWindow, _lineContext)) _lineDeleteBuffers(1, &buffer);
    }
}
