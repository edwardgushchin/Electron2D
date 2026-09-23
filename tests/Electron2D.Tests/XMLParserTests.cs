using System.Text;
using Electron2D;

internal static class XMLParserTests
{
    internal static void Run()
    {
        const string source = "<?xml version='1.0'?><root a='one &amp; two' b=\"&#x1F600;\">\n<c/>é&amp;<![CDATA[x<y]]><!--note--><item k='v'>body</item></root>";
        var bytes = Encoding.UTF8.GetBytes(source);
        using var parser = new XMLParser();
        Reject<InvalidOperationException>(() => parser.Read());
        Reject<ArgumentException>(() => parser.OpenBuffer([]));
        parser.OpenBuffer(bytes);
        bytes[0] = (byte)'x';

        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Unknown && parser.GetNodeName().StartsWith("?xml", StringComparison.Ordinal),
            "Processing instructions must remain distinct tokens and input must be copied.");
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "root" &&
              parser.GetAttributeCount() == 2 && parser.GetAttributeName(0) == "a" && parser.GetAttributeValue(0) == "one & two" &&
              parser.GetNamedAttributeValue("b") == "😀" && parser.HasAttribute("a") && !parser.HasAttribute("A") &&
              parser.GetNamedAttributeValueSafe("missing") == "" && parser.GetNodeOffset() == source.IndexOf("<root", StringComparison.Ordinal),
            "Opening tokens must preserve attribute order, unescape entities and report byte offsets.");
        Reject<KeyNotFoundException>(() => parser.GetNamedAttributeValue("missing"));
        Reject<ArgumentOutOfRangeException>(() => parser.GetAttributeName(2));

        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "c" && parser.IsEmpty() && parser.GetCurrentLine() == 1,
            "A short whitespace run must be skipped and a self-closing element recognized.");
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Text && parser.GetNodeData() == "é&" &&
              parser.GetNodeName() == "" && parser.GetNodeOffset() == Encoding.UTF8.GetByteCount(source[..source.IndexOf('é')]),
            "Text must decode UTF-8 while offsets remain in bytes.");
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.CDATA && parser.GetNodeName() == "x<y" && parser.GetNodeData() == "",
            "CDATA must not unescape or parse its contents.");
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Comment && parser.GetNodeName() == "note",
            "Comments must be returned without delimiters.");
        Check(parser.Read() && parser.GetNodeName() == "item" && parser.GetNamedAttributeValue("k") == "v", "Nested element must be readable.");
        parser.SkipSection();
        Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetNodeName() == "item", "SkipSection must land on the matching closing element.");
        Check(parser.Read() && parser.GetNodeName() == "root" && parser.GetNodeType() == XMLParser.NodeType.ElementEnd && !parser.Read(),
            "Closing element and EOF must be distinct.");

        var offset = Encoding.UTF8.GetByteCount(source[..source.IndexOf("<item", StringComparison.Ordinal)]);
        Check(parser.Seek(offset) && parser.GetNodeName() == "item" && parser.GetNodeOffset() == offset, "Seek must parse from a byte offset.");
        Reject<ArgumentOutOfRangeException>(() => parser.Seek(long.MaxValue));

        parser.OpenBuffer("<open><broken"u8.ToArray());
        Check(parser.GetNodeType() == XMLParser.NodeType.None && parser.Read() && parser.GetNodeName() == "open" &&
              parser.Read() && parser.GetNodeName() == "broken" && !parser.Read(),
            "Reopening must reset state and incomplete XML must remain tokenizable.");

        parser.OpenBuffer("<outer><inner/><inner><leaf/></inner></outer>"u8.ToArray());
        Check(parser.Read() && parser.GetNodeName() == "outer", "Outer token must be available before skipping.");
        parser.SkipSection();
        Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetNodeName() == "outer" && !parser.Read(),
            "SkipSection must account for nested and self-closing elements.");
        parser.OpenBuffer("<x>   </x>"u8.ToArray());
        Check(parser.Read() && parser.Read() && parser.GetNodeData() == "   ", "Long whitespace must be preserved as text.");

        var path = System.IO.Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<file/>");
            parser.Open(path);
            Check(parser.Read() && parser.GetNodeName() == "file" && parser.IsEmpty(), "Open must compose with file access.");
        }
        finally { File.Delete(path); }

        parser.Dispose();
        Reject<ObjectDisposedException>(() => parser.Read());
        Console.WriteLine("XML token, offset, entity, seek, file and lifetime checks passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
