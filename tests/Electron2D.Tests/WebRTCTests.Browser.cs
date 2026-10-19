using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Electron2D;
internal static partial class WebRTCTests
{
    internal static void Browser()
    {
        using var peer = WebRTCPeerConnection.Create(); using var channel = peer.CreateDataChannel("interop"); var candidates = new List<object>(); string? offer = null;
        peer.SessionDescriptionCreated += (type, sdp) => { peer.SetLocalDescription(type, sdp); offer = sdp; }; peer.ICECandidateCreated += (mid, index, value) => candidates.Add(new { candidate = value, sdpMid = mid, sdpMLineIndex = index });
        var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); var port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        using var listener = new HttpListener(); listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        var requests = new ConcurrentQueue<HttpListenerContext>(); var stopped = false;
        var worker = Task.Run(async () => { while (!Volatile.Read(ref stopped)) try { requests.Enqueue(await listener.GetContextAsync()); } catch (HttpListenerException) { break; } catch (ObjectDisposedException) { break; } });
        peer.CreateOffer(); Console.WriteLine("WEBRTC_BROWSER_URL=http://127.0.0.1:" + port + "/"); Console.Out.Flush(); var watch = Stopwatch.StartNew(); var answered = false; var sent = false; var packets = 0;
        try
        {
            while (watch.Elapsed.TotalSeconds < 90)
            {
                peer.Poll();
                while (requests.TryDequeue(out var context))
                {
                    try
                    {
                        if (context.Request.Url!.AbsolutePath == "/offer")
                        {
                            if (offer is null || peer.GetGatheringState() != WebRTCPeerConnection.GatheringState.Complete) { context.Response.StatusCode = 503; Reply(context, "wait", "text/plain"); }
                            else Reply(context, System.Text.Json.JsonSerializer.Serialize(new { type = "offer", sdp = offer, candidates }), "application/json");
                        }
                        else if (context.Request.Url.AbsolutePath == "/answer")
                        { using var input = new StreamReader(context.Request.InputStream, Encoding.UTF8); var sdp = input.ReadToEnd(); peer.SetRemoteDescription("answer", sdp); answered = true; Reply(context, "ok", "text/plain"); }
                        else Reply(context, Page, "text/html");
                    }
                    catch (Exception error) { context.Response.StatusCode = 500; Reply(context, error.Message, "text/plain"); throw; }
                }
                if (answered && !sent && channel.GetReadyState() == WebRTCDataChannel.ChannelState.Open) { channel.WriteMode = PacketWriteMode.Text; channel.PutPacket(Encoding.UTF8.GetBytes("native\0snowman\u2603")); sent = true; }
                while (channel.GetAvailablePacketCount() > 0)
                {
                    var data = channel.GetPacket();
                    if (packets == 0) Check(channel.WasStringPacket() && Encoding.UTF8.GetString(data) == "browser\0snowman\u2603", "Independent browser UTF-8/NUL response.");
                    else if (packets == 1) Check(!channel.WasStringPacket() && data.AsSpan().SequenceEqual(new byte[] { 0, 1, 255 }), "Independent browser binary response.");
                    else if (packets == 2) Check(!channel.WasStringPacket() && data.Length == 0, "Independent browser empty binary response.");
                    else throw new InvalidOperationException("Unexpected browser packet."); packets++;
                }
                if (sent && packets == 3) { Console.WriteLine("Independent browser/native WebRTC UTF-8 NUL, binary and empty packet interoperability passed."); return; }
                Thread.Sleep(2);
            }
            throw new TimeoutException("Independent browser did not finish: " + peer.GetConnectionState() + ", packets=" + packets);
        }
        finally { Volatile.Write(ref stopped, true); listener.Stop(); worker.GetAwaiter().GetResult(); }
    }
    private static void Reply(HttpListenerContext context, string value, string type) { var data = Encoding.UTF8.GetBytes(value); context.Response.ContentType = type; context.Response.ContentLength64 = data.Length; context.Response.OutputStream.Write(data); context.Response.Close(); }
    private const string Page = """
<!doctype html><meta charset="utf-8"><title>WebRTC native interoperability</title><pre id="result">Starting independent browser peer…</pre>
<script>
(async()=>{const result=document.getElementById('result');try{
let proposal;for(let i=0;i<100;i++){const response=await fetch('/offer');if(response.ok){proposal=await response.json();break;}await new Promise(r=>setTimeout(r,100));}if(!proposal)throw Error('No native offer');
const pc=new RTCPeerConnection();window.testPeer=pc;pc.onconnectionstatechange=()=>result.textContent='Browser connection: '+pc.connectionState;
pc.ondatachannel=event=>{const dc=event.channel;dc.binaryType='arraybuffer';dc.onmessage=message=>{if(message.data!=='native\0snowman☃')throw Error('Native text/NUL mismatch');dc.send('browser\0snowman☃');dc.send(new Uint8Array([0,1,255]));dc.send(new Uint8Array(0));result.textContent='PASS: independent browser received native UTF-8/NUL and sent text, binary, empty packets';};};
await pc.setRemoteDescription({type:proposal.type,sdp:proposal.sdp});for(const candidate of proposal.candidates)await pc.addIceCandidate(candidate);
await pc.setLocalDescription(await pc.createAnswer());if(pc.iceGatheringState!=='complete')await new Promise(resolve=>{pc.onicegatheringstatechange=()=>{if(pc.iceGatheringState==='complete')resolve();};});
const response=await fetch('/answer',{method:'POST',body:pc.localDescription.sdp});if(!response.ok)throw Error(await response.text());
}catch(error){result.textContent='FAIL: '+error.stack;}})();
</script>
""";
}
