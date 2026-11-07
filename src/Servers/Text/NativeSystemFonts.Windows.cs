using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Electron2D;

internal sealed partial class NativeSystemFonts
{
#if !IOS && !TVOS && !ANDROID && !ELECTRON2D_BROWSER_NATIVE
    internal sealed unsafe partial class WindowsCatalog(nint factory, nint collection, nint fallback) : IDisposable
    {
        private nint _factory = factory, _collection = collection, _fallback = fallback;
        private static readonly Guid FactoryInterface = new("0439FC60-CA44-4994-8DEE-3A9AF7B732EC"), LocalLoaderInterface = new("B2D9F3EC-C9FE-4A11-A2EC-D86208F7C0A2");
        private static void* Method(nint value, int slot) => (*(void***)value)[slot];
        private static void Check(int result) { if (result < 0) throw new InvalidOperationException($"The system font catalog failed (HRESULT 0x{result:X8})."); }
        private static void Release(ref nint value) { var owned = value; value = 0; if (owned != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)Method(owned, 2))(owned); }
        private static void Budget(uint count, uint limit = 65536) { if (count > limit) throw new InvalidDataException("The system font catalog exceeds its budget."); }

        internal static WindowsCatalog? Create()
        {
            nint factory = 0, collection = 0, fallback = 0;
            try
            {
                if (DWriteCreateFactory(0, in FactoryInterface, out factory) < 0 || factory == 0) return null;
                if (((delegate* unmanaged[Stdcall]<nint, nint*, int, int>)Method(factory, 3))(factory, &collection, 0) < 0) return null;
                if (((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(factory, 26))(factory, &fallback) < 0) return null;
                if (collection == 0 || fallback == 0) return null;
                var catalog = new WindowsCatalog(factory, collection, fallback); factory = collection = fallback = 0; return catalog;
            }
            catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException) { return null; }
            finally { Release(ref fallback); Release(ref collection); Release(ref factory); }
        }

        internal string[] Names()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var count = ((delegate* unmanaged[Stdcall]<nint, uint>)Method(_collection, 3))(_collection); Budget(count);
            for (uint index = 0; index < count; index++)
            {
                nint family = 0, names = 0;
                try
                {
                    Check(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(_collection, 4))(_collection, index, &family));
                    Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(family, 6))(family, &names));
                    var aliases = ((delegate* unmanaged[Stdcall]<nint, uint>)Method(names, 3))(names); Budget(aliases, 1024);
                    for (uint slot = 0; slot < aliases; slot++) { var name = Name(names, slot); if (name.Length != 0) result.Add(name); }
                }
                finally { Release(ref names); Release(ref family); }
            }
            return result.Order(StringComparer.Ordinal).ToArray();
        }

        private static string Name(nint names, uint slot)
        {
            uint length = 0; Check(((delegate* unmanaged[Stdcall]<nint, uint, uint*, int>)Method(names, 7))(names, slot, &length)); Budget(length);
            var value = new char[length + 1];
            fixed (char* buffer = value) Check(((delegate* unmanaged[Stdcall]<nint, uint, char*, uint, int>)Method(names, 8))(names, slot, buffer, length + 1));
            return new string(value, 0, (int)length);
        }

        internal SystemFontMatch[] Match(string familyName, string text, string locale, int weight, int stretch, bool italic)
        {
            weight = Math.Clamp(weight, 100, 999); stretch = Width(Math.Clamp(stretch, 50, 200));
            if (text.Length == 0)
            {
                nint family = 0, font = 0;
                try
                {
                    uint index = 0; int exists = 0;
                    fixed (char* name = familyName) Check(((delegate* unmanaged[Stdcall]<nint, char*, uint*, int*, int>)Method(_collection, 5))(_collection, name, &index, &exists));
                    if (exists != 0)
                    {
                        Check(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(_collection, 4))(_collection, index, &family));
                        Check(((delegate* unmanaged[Stdcall]<nint, int, int, int, nint*, int>)Method(family, 7))(family, weight, stretch, italic ? 2 : 0, &font));
                        return Describe(font) is { } match ? [match] : [];
                    }
                }
                finally { Release(ref font); Release(ref family); }
                text = " ";
            }
            using var source = new AnalysisSource(text, locale.Length == 0 ? System.Globalization.CultureInfo.CurrentUICulture.Name : locale);
            var pointer = (nint)ComInterfaceMarshaller<IAnalysisSource>.ConvertToUnmanaged(source);
            try
            {
                var matches = new List<SystemFontMatch>(); uint position = 0;
                while (position < text.Length && matches.Count < 64)
                {
                    uint mapped = 0; float scale = 0; nint font = 0;
                    try
                    {
                        fixed (char* name = familyName)
                            Check(((delegate* unmanaged[Stdcall]<nint, nint, uint, uint, nint, char*, int, int, int, uint*, nint*, float*, int>)Method(_fallback, 3))(_fallback, pointer, position, (uint)text.Length - position, _collection, name, weight, italic ? 2 : 0, stretch, &mapped, &font, &scale));
                        if (mapped == 0 || mapped > text.Length - position) throw new InvalidDataException("The system font catalog returned an invalid text range.");
                        position += mapped;
                        if (font != 0 && Describe(font) is { } match && !matches.Contains(match)) matches.Add(match);
                    }
                    finally { Release(ref font); }
                }
                return matches.ToArray();
            }
            finally { ComInterfaceMarshaller<IAnalysisSource>.Free((void*)pointer); }
        }

        private static int Width(int percent)
        {
            ReadOnlySpan<float> widths = [50, 62.5f, 75, 87.5f, 100, 112.5f, 125, 150, 200];
            var best = 0; for (var i = 1; i < widths.Length; i++) if (Math.Abs(widths[i] - percent) < Math.Abs(widths[best] - percent)) best = i;
            return best + 1;
        }

        private static SystemFontMatch? Describe(nint font)
        {
            nint face = 0, file = 0, loader = 0, local = 0, family = 0, names = 0;
            try
            {
                Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(font, 13))(font, &face));
                var type = ((delegate* unmanaged[Stdcall]<nint, int>)Method(face, 3))(face); if (type is < 0 or > 3) return null;
                uint count = 0; Check(((delegate* unmanaged[Stdcall]<nint, uint*, nint*, int>)Method(face, 4))(face, &count, null));
                if (count != 1) return null;
                Check(((delegate* unmanaged[Stdcall]<nint, uint*, nint*, int>)Method(face, 4))(face, &count, &file));
                Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(file, 4))(file, &loader));
                var iid = LocalLoaderInterface;
                if (((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(loader, 0))(loader, &iid, &local) < 0) return null;
                void* key = null; uint keySize = 0, length = 0;
                Check(((delegate* unmanaged[Stdcall]<nint, void**, uint*, int>)Method(file, 3))(file, &key, &keySize));
                Check(((delegate* unmanaged[Stdcall]<nint, void*, uint, uint*, int>)Method(local, 4))(local, key, keySize, &length)); Budget(length);
                var path = new char[length + 1];
                fixed (char* buffer = path) Check(((delegate* unmanaged[Stdcall]<nint, void*, uint, char*, uint, int>)Method(local, 5))(local, key, keySize, buffer, length + 1));
                Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(font, 3))(font, &family));
                Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(family, 6))(family, &names));
                uint slot = 0; int exists = 0;
                fixed (char* language = "en-us") Check(((delegate* unmanaged[Stdcall]<nint, char*, uint*, int*, int>)Method(names, 4))(names, language, &slot, &exists));
                var faceIndex = ((delegate* unmanaged[Stdcall]<nint, uint>)Method(face, 5))(face); Budget(faceIndex, int.MaxValue);
                return new(new string(path, 0, (int)length), (int)faceIndex, Name(names, exists != 0 ? slot : 0));
            }
            finally { Release(ref names); Release(ref family); Release(ref local); Release(ref loader); Release(ref file); Release(ref face); }
        }

        public void Dispose() { Release(ref _fallback); Release(ref _collection); Release(ref _factory); }

        [GeneratedComInterface, Guid("688E1A58-5094-47C8-ADC8-FBCEA60AE92B")]
        internal partial interface IAnalysisSource
        {
            [PreserveSig] int GetTextAtPosition(uint position, out nint text, out uint length);
            [PreserveSig] int GetTextBeforePosition(uint position, out nint text, out uint length);
            [PreserveSig] int GetParagraphReadingDirection();
            [PreserveSig] int GetLocaleName(uint position, out uint length, out nint locale);
            [PreserveSig] int GetNumberSubstitution(uint position, out uint length, out nint substitution);
        }

        [GeneratedComClass]
        internal sealed partial class AnalysisSource : IAnalysisSource, IDisposable
        {
            private GCHandle _text, _locale;
            private readonly uint _length;
            internal AnalysisSource(string text, string locale)
            {
                _length = (uint)text.Length; _text = GCHandle.Alloc(text, GCHandleType.Pinned);
                try { _locale = GCHandle.Alloc(locale, GCHandleType.Pinned); } catch { _text.Free(); throw; }
            }
            public int GetTextAtPosition(uint position, out nint text, out uint length) { length = position < _length ? _length - position : 0; text = length == 0 ? 0 : _text.AddrOfPinnedObject() + (nint)(position * 2); return 0; }
            public int GetTextBeforePosition(uint position, out nint text, out uint length) { length = position <= _length ? position : 0; text = length == 0 ? 0 : _text.AddrOfPinnedObject(); return 0; }
            public int GetParagraphReadingDirection() => 0;
            public int GetLocaleName(uint position, out uint length, out nint locale) { length = position < _length ? _length - position : 0; locale = _locale.AddrOfPinnedObject(); return 0; }
            public int GetNumberSubstitution(uint position, out uint length, out nint substitution) { length = position < _length ? _length - position : 0; substitution = 0; return 0; }
            public void Dispose() { if (_locale.IsAllocated) _locale.Free(); if (_text.IsAllocated) _text.Free(); }
        }

        [LibraryImport("dwrite.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int DWriteCreateFactory(int type, in Guid iid, out nint factory);
    }
#endif
}
