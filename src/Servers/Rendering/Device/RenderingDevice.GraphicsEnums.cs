namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Identifies native raster sample counts.</summary>
    public enum TextureSamples
    {
        /// <summary>One sample per pixel.</summary>
        Samples1 = 0,
        /// <summary>Two samples per pixel.</summary>
        Samples2 = 1,
        /// <summary>Four samples per pixel.</summary>
        Samples4 = 2,
        /// <summary>Eight samples per pixel.</summary>
        Samples8 = 3,
        /// <summary>Marks the exclusive numeric extent; it is not an executable selection.</summary>
        Max = 7,
    }
    /// <summary>Selects pipeline states that may change during a draw list.</summary>
    [Flags]
    public enum PipelineDynamicStateFlags
    {
        /// <summary>Allows DrawListSetBlendConstants to replace the copied pipeline constant.</summary>
        BlendConstants = 4
    }
    /// <summary>Identifies VertexFrequency values for executable graphics operations.</summary>
    public enum VertexFrequency
    {
        /// <summary>Selects Vertex.</summary>
        Vertex = 0,
        /// <summary>Selects Instance.</summary>
        Instance = 1,
    }
    /// <summary>Identifies IndexBufferFormat values for executable graphics operations.</summary>
    public enum IndexBufferFormat
    {
        /// <summary>Selects UInt16.</summary>
        UInt16 = 0,
        /// <summary>Selects UInt32.</summary>
        UInt32 = 1,
    }
    /// <summary>Identifies RenderPrimitive values for executable graphics operations.</summary>
    public enum RenderPrimitive
    {
        /// <summary>Selects Points.</summary>
        Points = 0,
        /// <summary>Selects Lines.</summary>
        Lines = 1,
        /// <summary>Selects LineStrips.</summary>
        LineStrips = 3,
        /// <summary>Selects Triangles.</summary>
        Triangles = 5,
        /// <summary>Selects TriangleStrips.</summary>
        TriangleStrips = 7,
        /// <summary>Selects TriangleStripsWithRestartIndex.</summary>
        TriangleStripsWithRestartIndex = 9,
        /// <summary>Marks the exclusive numeric extent; it is not an executable selection.</summary>
        Max = 11,
    }
    /// <summary>Identifies PolygonCullMode values for executable graphics operations.</summary>
    public enum PolygonCullMode
    {
        /// <summary>Selects Disabled.</summary>
        Disabled = 0,
        /// <summary>Selects Front.</summary>
        Front = 1,
        /// <summary>Selects Back.</summary>
        Back = 2,
    }
    /// <summary>Identifies PolygonFrontFace values for executable graphics operations.</summary>
    public enum PolygonFrontFace
    {
        /// <summary>Selects Clockwise.</summary>
        Clockwise = 0,
        /// <summary>Selects CounterClockwise.</summary>
        CounterClockwise = 1,
    }
    /// <summary>Identifies BlendOperation values for executable graphics operations.</summary>
    public enum BlendOperation
    {
        /// <summary>Selects Add.</summary>
        Add = 0,
        /// <summary>Selects Subtract.</summary>
        Subtract = 1,
        /// <summary>Selects ReverseSubtract.</summary>
        ReverseSubtract = 2,
        /// <summary>Selects Minimum.</summary>
        Minimum = 3,
        /// <summary>Selects Maximum.</summary>
        Maximum = 4,
        /// <summary>Marks the exclusive numeric extent; it is not an executable selection.</summary>
        Max = 5,
    }
    /// <summary>Identifies BlendFactor values for executable graphics operations.</summary>
    public enum BlendFactor
    {
        /// <summary>Selects Zero.</summary>
        Zero = 0,
        /// <summary>Selects One.</summary>
        One = 1,
        /// <summary>Selects SrcColor.</summary>
        SrcColor = 2,
        /// <summary>Selects OneMinusSrcColor.</summary>
        OneMinusSrcColor = 3,
        /// <summary>Selects DstColor.</summary>
        DstColor = 4,
        /// <summary>Selects OneMinusDstColor.</summary>
        OneMinusDstColor = 5,
        /// <summary>Selects SrcAlpha.</summary>
        SrcAlpha = 6,
        /// <summary>Selects OneMinusSrcAlpha.</summary>
        OneMinusSrcAlpha = 7,
        /// <summary>Selects DstAlpha.</summary>
        DstAlpha = 8,
        /// <summary>Selects OneMinusDstAlpha.</summary>
        OneMinusDstAlpha = 9,
        /// <summary>Selects ConstantColor.</summary>
        ConstantColor = 10,
        /// <summary>Selects OneMinusConstantColor.</summary>
        OneMinusConstantColor = 11,
        /// <summary>Selects SrcAlphaSaturate.</summary>
        SrcAlphaSaturate = 14,
        /// <summary>Marks the exclusive numeric extent; it is not an executable selection.</summary>
        Max = 19,
    }
    /// <summary>Identifies DrawFlags values for executable graphics operations.</summary>
    [Flags]
    public enum DrawFlags
    {
        /// <summary>Selects DefaultAll.</summary>
        DefaultAll = 0,
        /// <summary>Selects ClearColor0.</summary>
        ClearColor0 = 1,
        /// <summary>Selects ClearColor1.</summary>
        ClearColor1 = 2,
        /// <summary>Selects ClearColor2.</summary>
        ClearColor2 = 4,
        /// <summary>Selects ClearColor3.</summary>
        ClearColor3 = 8,
        /// <summary>Selects ClearColor4.</summary>
        ClearColor4 = 16,
        /// <summary>Selects ClearColor5.</summary>
        ClearColor5 = 32,
        /// <summary>Selects ClearColor6.</summary>
        ClearColor6 = 64,
        /// <summary>Selects ClearColor7.</summary>
        ClearColor7 = 128,
        /// <summary>Selects ClearColorAll.</summary>
        ClearColorAll = 255,
        /// <summary>Masks the per-color clear flags.</summary>
        ClearColorMask = 255,
        /// <summary>Selects IgnoreColor0.</summary>
        IgnoreColor0 = 256,
        /// <summary>Selects IgnoreColor1.</summary>
        IgnoreColor1 = 512,
        /// <summary>Selects IgnoreColor2.</summary>
        IgnoreColor2 = 1024,
        /// <summary>Selects IgnoreColor3.</summary>
        IgnoreColor3 = 2048,
        /// <summary>Selects IgnoreColor4.</summary>
        IgnoreColor4 = 4096,
        /// <summary>Selects IgnoreColor5.</summary>
        IgnoreColor5 = 8192,
        /// <summary>Selects IgnoreColor6.</summary>
        IgnoreColor6 = 16384,
        /// <summary>Selects IgnoreColor7.</summary>
        IgnoreColor7 = 32768,
        /// <summary>Selects IgnoreColorAll.</summary>
        IgnoreColorAll = 65280,
        /// <summary>Masks the per-color discard flags.</summary>
        IgnoreColorMask = 65280,
        /// <summary>Clears the complete depth aspect.</summary>
        ClearDepth = 65536,
        /// <summary>Discards existing depth before the pass.</summary>
        IgnoreDepth = 131072,
        /// <summary>Clears the complete stencil aspect.</summary>
        ClearStencil = 262144,
        /// <summary>Discards existing stencil before the pass.</summary>
        IgnoreStencil = 524288,
        /// <summary>Clears every color, depth and stencil aspect; absent aspects are ignored.</summary>
        ClearAll = 327935,
        /// <summary>Discards every color, depth and stencil aspect; absent aspects are ignored.</summary>
        IgnoreAll = 720640,
    }
    /// <summary>Identifies BufferCreationBits values for executable graphics operations.</summary>
    [Flags]
    public enum BufferCreationBits
    {
        /// <summary>Selects AsStorage.</summary>
        AsStorage = 2,
    }
}
