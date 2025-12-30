namespace Electron2D;

public struct Color
{
    private uint _color;
    
    public Color(uint color) => _color = color;

    public Color(byte red, byte green, byte blue)
    {
        
    }

    public Color(float red, float green, float blue, float alpha)
    {
        
    }
    
    public byte Red => (byte)((_color >> 24) & 0xFF);
    public byte Green => (byte)((_color >> 16) & 0xFF);
    public byte Blue => (byte)((_color >> 8) & 0xFF);
    public byte Alpha => (byte)((_color >> 0) & 0xFF);
}