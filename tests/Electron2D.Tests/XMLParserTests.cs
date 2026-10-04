using System.Diagnostics;
using System.Text;
using Electron2D;

internal static class XMLParserTests
{
    internal static void Run()
    {
        const string source = "<?xml version='1.0'?><root a='one &amp; two' b=\"&#x1F600;\">\n<c/>é&amp;<![CDATA[x<y]]><!--note--><item k='v'>body</item></root>";
        var bytes = Encoding.UTF8.GetBytes(source);
        using var parser = new XMLParser();
        Check(parser.GetNodeType() == XMLParser.NodeType.None && parser.GetNodeOffset() == 0 &&
              parser.GetCurrentLine() == 0 && parser.GetAttributeCount() == 0 &&
              (int)XMLParser.NodeType.None == 0 && (int)XMLParser.NodeType.Element == 1 &&
              (int)XMLParser.NodeType.ElementEnd == 2 && (int)XMLParser.NodeType.Text == 3 &&
              (int)XMLParser.NodeType.Comment == 4 && (int)XMLParser.NodeType.CDATA == 5 &&
              (int)XMLParser.NodeType.Unknown == 6,
            "Unopened state and all token identities must match the pinned type contract.");
        Reject<InvalidOperationException>(() => parser.Read());
        Reject<ArgumentException>(() => parser.OpenBuffer([]));
        parser.OpenBuffer(bytes);
        bytes[0] = (byte)'x';

        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Unknown && parser.GetNodeName().StartsWith("?xml", StringComparison.Ordinal),
            "Processing instructions must remain distinct tokens and input must be copied.");
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "root" &&
              parser.GetAttributeCount() == 2 && parser.GetAttributeName(0) == "a" && parser.GetAttributeValue(0) == "one & two" &&
              parser.GetNamedAttributeValue("b") == "😀" && parser.HasAttribute("a") && !parser.HasAttribute("A") &&
              parser.GetNamedAttributeValueSafe("missing") == "" && !parser.IsEmpty() &&
              parser.GetNodeOffset() == source.IndexOf("<root", StringComparison.Ordinal),
            "Opening tokens must preserve attribute order, unescape entities and report byte offsets.");
        using (var diagnostics = new StringWriter())
        using (var listener = new TextWriterTraceListener(diagnostics))
        {
            Trace.Listeners.Add(listener);
            try
            {
                Check(parser.GetNamedAttributeValue("missing") == string.Empty &&
                      parser.GetAttributeName(2) == string.Empty && parser.GetAttributeValue(-1) == string.Empty &&
                      parser.GetNodeData() == string.Empty,
                    "Missing or invalid attribute and token reads must return empty text.");
                Trace.Flush();
                Check(diagnostics.ToString().Contains("XML attribute", StringComparison.Ordinal) &&
                      diagnostics.ToString().Contains("not text", StringComparison.Ordinal),
                    "Invalid getter calls must emit managed diagnostics.");
            }
            finally { Trace.Listeners.Remove(listener); }
        }

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
        Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetAttributeCount() == 0,
            "EOF must retain the final closing token and its cleared attribute state.");

        var offset = Encoding.UTF8.GetByteCount(source[..source.IndexOf("<item", StringComparison.Ordinal)]);
        Check(parser.Seek(offset) && parser.GetNodeName() == "item" && parser.GetNodeOffset() == offset, "Seek must parse from a byte offset.");
        Reject<ArgumentOutOfRangeException>(() => parser.Seek(long.MaxValue));

        Reject<ArgumentException>(() => parser.OpenBuffer([]));
        Check(parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "item" &&
              parser.GetNodeOffset() == offset && parser.GetAttributeCount() == 1,
            "A failed buffer open must preserve the previous token and input.");
        parser.OpenBuffer("<open><broken"u8.ToArray());
        Check(parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "item" &&
              parser.GetAttributeCount() == 1 && parser.GetCurrentLine() == 0 &&
              parser.Read() && parser.GetNodeName() == "open" && parser.GetAttributeCount() == 0 &&
              parser.Read() && parser.GetNodeName() == "broken" && !parser.Read(),
            "Reopening resets the cursor and line while retaining the last token until reading the new input.");

        parser.OpenBuffer("<outer><inner/><inner><leaf/></inner></outer>"u8.ToArray());
        Check(parser.Read() && parser.GetNodeName() == "outer", "Outer token must be available before skipping.");
        parser.SkipSection();
        Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetNodeName() == "outer" && !parser.Read(),
            "SkipSection must account for nested and self-closing elements.");
        parser.OpenBuffer("<x>   </x>"u8.ToArray());
        Check(parser.Read() && parser.Read() && parser.GetNodeData() == "   ", "Long whitespace must be preserved as text.");

        parser.OpenBuffer("<node x='1' x='2'>text<!--comment--><![CDATA[raw]]><?pi?></node>"u8.ToArray());
        Check(parser.Read() && parser.GetAttributeCount() == 2 &&
              parser.GetAttributeName(0) == "x" && parser.GetAttributeValue(1) == "2" &&
              parser.GetNamedAttributeValue("x") == "1",
            "Attributes retain source order and named lookup selects the first duplicate.");
        foreach (var kind in new[] { XMLParser.NodeType.Text, XMLParser.NodeType.Comment,
                     XMLParser.NodeType.CDATA, XMLParser.NodeType.Unknown })
        {
            Check(parser.Read() && parser.GetNodeType() == kind && parser.GetAttributeCount() == 2 &&
                  parser.HasAttribute("x") && parser.GetNamedAttributeValueSafe("x") == "1",
                "Non-element tokens must retain the last element's attribute list.");
        }
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.ElementEnd &&
              parser.GetAttributeCount() == 0 && !parser.HasAttribute("x"),
            "A closing element must clear the retained attribute list.");

        var path = System.IO.Path.GetTempFileName();
        try
        {
            Reject<IOException>(() => parser.Open(path));
            Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetNodeName() == "node",
                "Opening an empty file must preserve the previous source and token.");
            File.WriteAllText(path, "<file/>");
            parser.Open(path);
            Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetCurrentLine() == 0 &&
                  parser.Read() && parser.GetNodeName() == "file" && parser.IsEmpty(),
                "Ordinary file open must preserve the last token until a read and then use the new file.");
            Reject<FileNotFoundException>(() => parser.Open(path + ".missing"));
            Check(parser.GetNodeName() == "file" && parser.IsEmpty(),
                "A failed file read must leave the prior input and token available.");
        }
        finally { File.Delete(path); }

        var virtualName = $"xml-parser-{Guid.NewGuid():N}.xml";
        var virtualPath = System.IO.Path.Combine(ProjectSettings.ProjectRoot, virtualName);
        try
        {
            File.WriteAllText(virtualPath, "<virtual/>");
            parser.Open("res://" + virtualName);
            Check(parser.Read() && parser.GetNodeName() == "virtual" && parser.IsEmpty(),
                "Open must compose with the directory-backed resource path resolver.");
        }
        finally { File.Delete(virtualPath); }

        VerifyCursorEdges();
        parser.Dispose();
        Reject<ObjectDisposedException>(() => parser.Read());
        Console.WriteLine("XML token, offset, entity, seek, file and lifetime checks passed.");
    }

    private static void VerifyCursorEdges()
    {
        using var parser = new XMLParser();
        parser.SkipSection();
        Reject<InvalidOperationException>(() => parser.Seek(0));
        parser.OpenBuffer("x"u8.ToArray());
        Check(!parser.Read() && parser.GetNodeType() == XMLParser.NodeType.None,
            "A single-byte input must return EOF without producing a token.");
        parser.OpenBuffer("ab"u8.ToArray());
        Check(parser.Read() && parser.GetNodeData() == "ab" && !parser.Read(),
            "A two-byte non-whitespace input must still produce text.");

        parser.OpenBuffer("<x/> \n"u8.ToArray());
        Check(parser.Read() && parser.GetNodeName() == "x" && parser.IsEmpty() &&
              parser.Read() && parser.GetNodeName() == "x" && parser.GetNodeOffset() == 4 &&
              parser.GetCurrentLine() == 1 && !parser.Read(),
            "Short trailing whitespace must advance the cursor successfully without replacing the prior token.");
        parser.OpenBuffer("<a/>b"u8.ToArray());
        Check(parser.Read() && !parser.Seek(4) && parser.GetNodeName() == "a" && parser.IsEmpty(),
            "Seeking to the final byte is valid but its subsequent read reports EOF without changing the token.");
        Reject<ArgumentOutOfRangeException>(() => parser.Seek(5));
        Check(parser.GetNodeName() == "a" && parser.IsEmpty(),
            "An invalid seek must preserve the prior token.");
        parser.OpenBuffer("<a>\n<b/></a>"u8.ToArray());
        Check(parser.Read() && parser.Read() && parser.GetNodeName() == "b" &&
              parser.GetCurrentLine() == 1 && parser.Seek(0) && parser.GetNodeName() == "a" &&
              parser.GetCurrentLine() == 1,
            "Seeking backward must read from the requested byte while preserving consumed-line history.");

        parser.OpenBuffer("<a/><b/>"u8.ToArray());
        Check(parser.Read() && parser.IsEmpty(), "A self-closing element must be current before SkipSection.");
        parser.SkipSection();
        Check(parser.GetNodeName() == "a" && parser.Read() && parser.GetNodeName() == "b",
            "SkipSection must leave an empty element current without consuming its sibling.");
        parser.OpenBuffer("<a/>t</b>"u8.ToArray());
        Check(parser.Read() && parser.IsEmpty() && parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Text,
            "Text following an empty element retains the previous empty flag.");
        parser.SkipSection();
        Check(parser.GetNodeType() == XMLParser.NodeType.Text && parser.Read() && parser.GetNodeName() == "b",
            "SkipSection must not consume another token while the retained empty flag is set.");
        parser.OpenBuffer("<a>t<b/></a>"u8.ToArray());
        Check(parser.Read() && parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Text,
            "Text inside a section must be readable before skipping.");
        parser.SkipSection();
        Check(parser.GetNodeType() == XMLParser.NodeType.ElementEnd && parser.GetNodeName() == "a",
            "Skipping from an interior text token must reach the section's first closing level.");
        parser.OpenBuffer("<a><b>"u8.ToArray());
        Check(parser.Read(), "An incomplete outer element must open.");
        parser.SkipSection();
        Check(parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "b" && !parser.Read(),
            "Missing closing tags must stop section skipping at EOF without fabricating a close.");

        parser.OpenBuffer("<x a>tail</x>"u8.ToArray());
        Check(parser.Read() && parser.GetNodeName() == "x" && parser.GetAttributeCount() == 0 && !parser.Read(),
            "An unterminated attribute can consume malformed trailing markup without a fabricated token.");
        parser.OpenBuffer("<![notdata"u8.ToArray());
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.CDATA && parser.GetNodeName() == "a" && !parser.Read(),
            "A bracket declaration follows the permissive fixed-prefix CDATA path.");
        parser.OpenBuffer("<!DOCTYPE a <b>><?unfinished"u8.ToArray());
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Comment &&
              parser.GetNodeName() == "DOCTYPE a <b>" &&
              parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Unknown &&
              parser.GetNodeName() == "?unfinished" && !parser.Read(),
            "Nested declaration brackets and an incomplete instruction remain independent permissive tokens.");
        parser.OpenBuffer("<a></a"u8.ToArray());
        Check(parser.Read() && parser.Read() && parser.GetNodeType() == XMLParser.NodeType.ElementEnd &&
              parser.GetNodeName() == "a" && !parser.Read(),
            "A closing element without a final bracket remains readable before EOF.");
        parser.OpenBuffer("<x>&#0;&#x0;&#X41;&#x41;&#xD800;&#x110000;&bogus;&amp;</x>"u8.ToArray());
        Check(parser.Read() && parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Text &&
              parser.GetNodeData() == "&#0;&#x0;&#X41;A\uD800&#x110000;&bogus;&",
            "Numeric entities preserve invalid zero and unrepresentable scalar text while decoding valid scalar and surrogate values.");
        parser.OpenBuffer(Encoding.UTF8.GetBytes("<x a='\uFEFFvalue'>\uFEFFtext</x>"));
        Check(parser.Read() && parser.GetAttributeValue(0) == "value" &&
              parser.Read() && parser.GetNodeData() == "text",
            "UTF-8 byte-order marks at the start of decoded attribute and text slices are skipped.");
        parser.OpenBuffer(Encoding.UTF8.GetBytes("\uFEFF<x/>"));
        Check(parser.Read() && parser.GetNodeType() == XMLParser.NodeType.Text &&
              parser.GetNodeData() == string.Empty && parser.Read() && parser.GetNodeName() == "x",
            "A leading byte-order mark remains a token boundary while its decoded text is empty.");
        parser.OpenBuffer([(byte)'<', (byte)'x', (byte)'>', 0xE2, 0x82, (byte)'<', (byte)'/', (byte)'x', (byte)'>']);
        Check(parser.Read() && parser.Read() && parser.GetNodeData() == "\uFFFD",
            "An incomplete UTF-8 sequence is replaced once without changing byte cursor positions.");
        parser.OpenBuffer([(byte)'<', (byte)'x', (byte)'>', (byte)'a', 0, (byte)'<', (byte)'y', (byte)'/', (byte)'>']);
        Check(parser.Read() && parser.Read() && parser.GetNodeData() == "a" && !parser.Read(),
            "An embedded zero byte terminates the token stream before later markup.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
