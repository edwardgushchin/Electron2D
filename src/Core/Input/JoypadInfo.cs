namespace Electron2D;

/// <summary>Provides typed platform information about a connected game controller.</summary>
public readonly struct JoypadInfo
{
    private readonly string? _rawName;

    internal JoypadInfo(string rawName, ushort vendorID, ushort productID, string? serialNumber)
    {
        _rawName = rawName;
        VendorID = vendorID;
        ProductID = productID;
        SerialNumber = serialNumber;
    }

    /// <summary>Gets the name reported by the operating system before mapping.</summary>
    public string RawName => _rawName ?? string.Empty;
    /// <summary>Gets the USB vendor identifier, or zero when unavailable.</summary>
    public ushort VendorID { get; }
    /// <summary>Gets the USB product identifier, or zero when unavailable.</summary>
    public ushort ProductID { get; }
    /// <summary>Gets the serial number when the native backend provides one.</summary>
    public string? SerialNumber { get; }
}
