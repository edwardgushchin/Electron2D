sealed class Calibration
{
    public byte[] Left = new byte[65536];
    public byte[] Right;
    public WeakReference<byte[]> Weak = null!;
    public Calibration() => Right = Left;
}
