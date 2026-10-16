using AssetRipper.TextureDecoder.Etc;
using AssetRipper.TextureDecoder.Rgb.Formats;

var mode = int.Parse(args[0]);
var width = int.Parse(args[1]);
var height = int.Parse(args[2]);
var input = File.ReadAllBytes(args[3]);
var output = new byte[width * height * 4];
switch (mode)
{
    case 0: EtcDecoder.DecompressETC<ColorRGBA<byte>, byte>(input, width, height, output); break;
    case 1: EtcDecoder.DecompressETC2<ColorRGBA<byte>, byte>(input, width, height, output); break;
    case 2: EtcDecoder.DecompressETC2A8<ColorRGBA<byte>, byte>(input, width, height, output); break;
    case 3: EtcDecoder.DecompressETC2A1<ColorRGBA<byte>, byte>(input, width, height, output); break;
    case 4: EtcDecoder.DecompressEACRUnsigned<ColorRGBA<byte>, byte>(input, width, height, output); break;
    case 6: EtcDecoder.DecompressEACRGUnsigned<ColorRGBA<byte>, byte>(input, width, height, output); break;
    default: throw new ArgumentOutOfRangeException(nameof(mode));
}
File.WriteAllBytes(args[4], output);
