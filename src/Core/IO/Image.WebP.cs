using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Encodes the base image as WebP bytes.</summary>
    /// <param name="lossy">True for lossy RGB compression; false for exact RGBA8 compression.</param>
    /// <param name="quality">Finite lossy quality from zero through one; ignored for lossless encoding.</param>
    /// <returns>An independent caller-owned encoded buffer.</returns>
    /// <remarks>Lossless encoding preserves RGB beneath transparent pixels. Uses a stable snapshot converted to RGBA8;
    /// supported BC formats are decompressed first. Mipmaps are not encoded. Does not mutate this image or emit Changed.
    /// Encoding uses ProjectSettings.WebPCompressionMethod and, for lossless output, WebPLosslessCompressionFactor.
    /// Each dimension is at most 16383; expanded RGBA8 input and encoded output are each bounded to 64 MiB.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Quality is not finite or is outside zero through one.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The compressed format has no decoder.</exception>
    /// <exception cref="IOException">The image exceeds encoding limits or encoding fails.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public unsafe byte[] SaveWebPToBuffer(bool lossy = false, float quality = 0.75f)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(quality) || quality is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(quality));
        var state = Snapshot();
        if (state.Width == 0) throw new InvalidOperationException("An empty image cannot be encoded.");
        if (state.Width > 16383 || state.Height > 16383 || (long)state.Width * state.Height * 4 > MaximumEncodedBytes)
            throw new IOException("WebP dimensions or expanded RGBA8 pixels exceed encoding limits.");
        if (IsCompressedFormat(state.Format)) state = DecompressBCPixels(state);
        state = ConvertState(state, Format.Rgba8);
        var inputLength = checked(state.Width * state.Height * 4);
        var effort = lossy ? quality * 100 : ProjectSettings.Get(ProjectSettings.WebPLosslessCompressionFactor);
        var method = ProjectSettings.Get(ProjectSettings.WebPCompressionMethod);
        using var output = new WebPOutput();
        var handle = GCHandle.Alloc(output);
        try
        {
            fixed (byte* pixels = state.Data)
            {
                var status = WebPEncodeNative(pixels, (ulong)inputLength, state.Width, state.Height, lossy ? 1 : 0,
                    effort, method, &WriteWebP, GCHandle.ToIntPtr(handle));
                if (output.Failure is { } failure) throw new IOException("Could not write encoded WebP pixels.", failure);
                if (status != 0) throw new IOException($"WebP encoding failed with codec status {status}.");
            }
            return output.Stream.ToArray();
        }
        finally { handle.Free(); }
    }

    /// <summary>Atomically saves the base image as a lossless or lossy WebP file.</summary>
    /// <param name="path">An operating-system, res:// or user:// destination path.</param>
    /// <param name="lossy">True for lossy RGB compression; false for exact RGBA8 compression.</param>
    /// <param name="quality">Finite lossy quality from zero through one; ignored for lossless encoding.</param>
    /// <remarks>Uses SaveWebPToBuffer, then atomically replaces the destination. Parent directories must exist.
    /// Encoding failures preserve an existing destination and the complete source image.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Quality is not finite or is outside zero through one.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="NotSupportedException">The compressed format has no decoder.</exception>
    /// <exception cref="IOException">Encoding or saving fails.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SaveWebP(string path, bool lossy = false, float quality = 0.75f) => SaveEncodedFile(path, SaveWebPToBuffer(lossy, quality));

    private sealed class WebPOutput : IDisposable
    {
        internal readonly MemoryStream Stream = new();
        internal Exception? Failure;
        public void Dispose() => Stream.Dispose();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int WriteWebP(byte* bytes, nuint length, nint context)
    {
        var output = (WebPOutput)GCHandle.FromIntPtr(context).Target!;
        try
        {
            if (length > (nuint)(MaximumEncodedBytes - output.Stream.Length))
                throw new IOException("Encoded WebP output exceeds 64 MiB.");
            output.Stream.Write(new ReadOnlySpan<byte>(bytes, checked((int)length)));
            return 1;
        }
        catch (Exception failure) { output.Failure = failure; return 0; }
    }

    [LibraryImport(NativeLibraries.WebPLibrary, EntryPoint = "e2d_webp_encode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int WebPEncodeNative(byte* pixels, ulong length, int width, int height, int lossy, float quality,
        int method, delegate* unmanaged[Cdecl]<byte*, nuint, nint, int> writer, nint context);
}
