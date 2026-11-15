using System.Text;
using Electron2D;
using Path = System.IO.Path;

var fixture = Directory.CreateTempSubdirectory("electron2d-errors-");
try
{
    CheckConfigFile(fixture.FullName);
    CheckDocuments();
    CheckResourceLoad(fixture.FullName);
    CheckNetworkState();
    Console.WriteLine("Public error contracts passed: configuration, document cursors, threaded resources, HTTP state.");
}
finally
{
    fixture.Delete(recursive: true);
}

static void CheckConfigFile(string directory)
{
    using var config = new ConfigFile();
    var path = Path.Combine(directory, "settings.cfg");
    var key = new ConfigKey<string>("game", "title");
    Throws<FileNotFoundException>(() => config.Load(path));

    var text = "[game]\ntitle=\"Électron\"\n";
    File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    config.Load(path);
    Require(config.GetValue(key) == "Électron", "Valid UTF-8 BOM and non-ASCII text");

    var syntax = Throws<FormatException>(() => config.Parse("[game]\ntitle=\"changed\"\nbroken"));
    Require(syntax.Message.Contains("line 3", StringComparison.Ordinal), "Configuration syntax line");

    File.WriteAllBytes(path, [0xef, 0xbb, 0xbf, .. "[game]\ntitle=\""u8, 0xff, .. "\"\n"u8]);
    Throws<DecoderFallbackException>(() => config.Load(path));

    File.WriteAllText(path, "[game]\ntitle=\"changed\"\n", Encoding.Unicode);
    Throws<DecoderFallbackException>(() => config.Load(path));
    Require(config.GetValue(key) == "Électron", "Failed syntax and encoding leave configuration intact");

    File.WriteAllText(path, "[game]\ntitle=\"updated\"\n");
    config.Load(path);
    Require(config.GetValue(key) == "updated", "UTF-8 without a BOM");
}

static void CheckDocuments()
{
    using var json = new JSON();
    Require(!json.Parse("{\n\"x\": ]", keepText: true), "JSON malformed result");
    Require(json.GetErrorLine() == 1 && json.GetErrorMessage().Length > 0, "JSON parse diagnostics");
    Require(json.Parse("null") && json.Data is null, "Valid JSON null result");
    Require(json.GetErrorMessage() == "" && json.GetErrorLine() == 0, "JSON diagnostics clear on success");

    using var xml = new XMLParser();
    Throws<InvalidOperationException>(() => xml.Read());
    xml.OpenBuffer("<root/>"u8.ToArray());
    Require(xml.Read() && xml.GetNodeName() == "root", "XML reads a token");
    Require(!xml.Read() && xml.GetNodeName() == "root", "XML EOF retains the token");
    Require(Throws<ArgumentOutOfRangeException>(() => xml.Seek(-1)).ParamName == "position", "XML invalid byte offset");
}

static void CheckResourceLoad(string directory)
{
    var path = Path.Combine(directory, "gradient.e2dres");
    Require(ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Unrequested load");
    ResourceLoader.LoadThreadedRequest<Gradient>(path);
    Require(SpinWait.SpinUntil(
        () => ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Failed,
        TimeSpan.FromSeconds(10)), "Threaded resource failure status");
    Throws<FileNotFoundException>(() => ResourceLoader.LoadThreadedGet<Gradient>(path));
    Require(ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Failed result collected");

    using var gradient = new Gradient { ResourceName = "Loaded gradient" };
    ResourceSaver.Save(gradient, path);
    ResourceLoader.LoadThreadedRequest<Gradient>(path);
    using var loaded = ResourceLoader.LoadThreadedGet<Gradient>(path);
    Require(loaded.ResourceName == "Loaded gradient", "Typed resource result after retry");
}

static void CheckNetworkState()
{
    using var client = new HTTPClient();
    Throws<InvalidOperationException>(() => client.Request(HTTPMethod.Get, "/", []));
    Require(client.GetStatus() == HTTPStatus.Disconnected, "A request without a connection preserves the HTTP phase");

    using var peer = new PacketPeerUDP();
    var failure = Throws<InvalidOperationException>(() => peer.GetPacket());
    Require(peer.GetPacketError() == PacketReadStatus.Unavailable && ReferenceEquals(peer.LastReadException, failure),
        "Empty packet reads preserve their typed result and exception");
}

static T Throws<T>(Action operation) where T : Exception
{
    try { operation(); }
    catch (T error) { return error; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Require(bool result, string scenario)
{
    if (!result) throw new InvalidOperationException(scenario);
}
