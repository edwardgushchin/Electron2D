#nullable disable
namespace Box2D.NET
{
    internal enum B2UserDataType : byte // must be byte!!
    {
        None = 0,
        Signed = 1,
        Unsigned = 2,
        Double = 3,
        Ref = 4,
    }
}