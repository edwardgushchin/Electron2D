"""Exercise the public project controller and compiled scenario SDK; no third-party Python modules."""
from pathlib import Path
import argparse, hashlib, json, os, signal, struct, subprocess, time, zlib

parser = argparse.ArgumentParser()
parser.add_argument('--tool', required=True)
parser.add_argument('--sdk', default=str(Path(__file__).resolve().parents[2]))
parser.add_argument('--output', required=True)
parser.add_argument('--native', action='store_true')
args = parser.parse_args()
root = Path(args.output).resolve(); root.mkdir(parents=True, exist_ok=False)
project = root / 'client with spaces'; tool = ['dotnet', str(Path(args.tool).resolve())]
rows = []

def command(name, arguments, expected=0, headless=True):
    env = os.environ.copy()
    if headless:
        env['SDL_VIDEO_DRIVER'] = env['SDL_VIDEODRIVER'] = 'dummy'
        env['VK_DRIVER_FILES'] = env['VK_ICD_FILENAMES'] = str(root / 'missing-vulkan.json')
    env['SDL_AUDIO_DRIVER'] = 'dummy'
    started = time.monotonic()
    run = subprocess.run(tool + list(map(str, arguments)), env=env, capture_output=True, text=True, timeout=360)
    (root / (name + '.stdout.log')).write_text(run.stdout)
    (root / (name + '.stderr.log')).write_text(run.stderr)
    result = json.loads(run.stdout)
    rows.append({'name':name,'exit':run.returncode,'expected':expected,'seconds':round(time.monotonic()-started,3),'result':result})
    (root / 'results.json').write_text(json.dumps(rows,indent=2)+'\n')
    assert run.returncode == expected and result['exitCode'] == expected and result['success'] == (expected == 0), (name, result)
    print(name, run.returncode, flush=True)
    return result

def run(name, request=None, expected=0, native=False):
    request = dict(base if request is None else request)
    source = root / (name + '.json'); source.write_text(json.dumps(request)+'\n')
    result = command(name, ['run',project,'--request',source,'--output',root/name],expected,not native)
    assert json.loads((root/name/'report.json').read_text()) == result
    return result

def number(result, name, step=6):
    return next(x['number'] for x in result['observations'] if x['name']==name and x['step']==step)

def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def png(path):
    data=Path(path).read_bytes(); assert data[:8]==b'\x89PNG\r\n\x1a\n'; pos=8; pixels=b''
    while pos<len(data):
        size=struct.unpack('>I',data[pos:pos+4])[0]; kind=data[pos+4:pos+8]; body=data[pos+8:pos+8+size]
        assert zlib.crc32(kind+body)&0xffffffff==struct.unpack('>I',data[pos+8+size:pos+12+size])[0]
        if kind==b'IHDR': width,height,depth,color,compression,filtering,interlace=struct.unpack('>IIBBBBB',body)
        if kind==b'IDAT': pixels+=body
        pos+=size+12
    assert depth==8 and color in [2,6] and not interlace
    channels=3 if color==2 else 4; raw=zlib.decompress(pixels); stride=width*channels; decoded=[]; previous=[0]*stride
    for y in range(height):
        start=y*(stride+1); mode=raw[start]; row=list(raw[start+1:start+1+stride]); assert mode in range(5)
        for i in range(stride):
            a=row[i-channels] if i>=channels else 0; b=previous[i]; c=previous[i-channels] if i>=channels else 0
            p=a+b-c; da,db,dc=abs(p-a),abs(p-b),abs(p-c); paeth=a if da<=db and da<=dc else b if db<=dc else c
            row[i]=(row[i]+[0,a,b,(a+b)//2,paeth][mode])&255
        decoded.append(row); previous=row
    return width,height,decoded[10][12*channels:12*channels+3]

command('create',['new',project,'--sdk',Path(args.sdk).resolve()])
command('open',['open',project])
command('invalid-project-name',['new',root/('x'*129),'--sdk',args.sdk],2); assert not (root/('x'*129)).exists()
command('trailing-create',['new',str(root/'trailing project')+'/', '--sdk', args.sdk]); command('trailing-open',['open',str(root/'trailing project')+'/'])
base=json.loads((project/'scenario.json').read_text()); source=project/'Program.cs'; original=source.read_text()
command('project-preserved',['new',project,'--sdk',args.sdk],2); assert source.read_text()==original
first=run('headless'); assert first['completedSteps']==6 and first['renderer']=='none' and number(first,'position-x')==9.5
old_hash=first['projectAssemblySHA256']
changed=original.replace('private const float DefaultSpeed = 30;', 'private const float DefaultSpeed = 60;')
assert changed!=original; source.write_text(changed)
second=run('focused-edit'); assert number(second,'position-x')==11 and second['projectAssemblySHA256']!=old_hash
assert number(first,'seeded-value')==number(second,'seeded-value')
saved=run('author',dict(base,mode='Authoring',inputs=[])); assert saved['completedSteps']==0 and saved['assertions'][0]['passed']
scene=project/'scene.e2dscene'; before=digest(scene)
loaded=run('fresh-scene',dict(base,scene='scene.e2dscene')); assert number(loaded,'position-x')==11 and loaded['sceneSHA256']==before
run('scene-preserved',dict(base,mode='Authoring',inputs=[]),2); assert digest(scene)==before
report_hash=digest(root/'headless'/'report.json')
command('output-preserved',['run',project,'--output',root/'headless'],2); assert digest(root/'headless'/'report.json')==report_hash
for name, request, expected in [('version',dict(base,version=2),2),('unknown-field',dict(base,unexpected=True),2),('no-fake-capture',dict(base,captureSteps=[1]),7),('unordered-input',dict(base,inputs=list(reversed(base['inputs']))),2),('missing-scene',dict(base,scene='missing.e2dscene'),2),('invalid-scene-path',dict(base,scene='\0'),2),('budget',dict(base,steps=2,inputs=base['inputs'][:1]),6)]:
    failed=run(name,request,expected); assert not any(a['kind']=='image' for a in failed['artifacts'])
key_before='''    protected override void BeforeStep(BatchContext context)
    {
        if (context.Step == 1) context.Check("key-press", Input.IsKeyPressed(Key.A), "Key state must commit before callbacks.");
        if (context.Step == 4) context.Check("key-release", !Input.IsKeyPressed(Key.A), "Key release must commit before callbacks.");
    }
'''
source.write_text(changed.replace('    protected override void AfterStep',key_before+'    protected override void AfterStep'))
keys=[base['inputs'][0],{'step':1,'key':'A','pressed':True},base['inputs'][1],{'step':4,'key':'A','pressed':False}]
run('key-input',dict(base,inputs=keys))
callback='    protected override void BeforeStep(BatchContext context) => throw new InvalidOperationException("scenario boom");\n'
source.write_text(changed.replace('    protected override void AfterStep',callback+'    protected override void AfterStep'))
failed=run('callback-failure',expected=4); assert any('scenario boom' in x['message'] for x in failed['diagnostics'])
cleanup=changed.replace('internal sealed class Scenario : BatchScenario\n{','internal sealed class Scenario : BatchScenario\n{\n    private BatchContext? _context;').replace('ResourceFileTypes.RegisterNode("sample.Actor", Actor.Create);','_context = context; ResourceFileTypes.RegisterNode("sample.Actor", Actor.Create);')
cleanup=cleanup.replace('    protected override void AfterStep',callback+'    public override void Dispose() { _context!.Observe("cleanup-disposed", _context.Root.IsDisposed); throw new InvalidOperationException("dispose boom"); }\n    protected override void AfterStep')
source.write_text(cleanup);failed=run('cleanup-failure',expected=4)
assert len(failed['diagnostics'])==2 and any(x['name']=='cleanup-disposed' and x['boolean'] for x in failed['observations'])
foreign=changed.replace('internal sealed class Scenario : BatchScenario\n{','internal sealed class Scenario : BatchScenario\n{\n    private BatchContext? _context; private Node? _foreign; private SceneTree? _tree;')
foreign=foreign.replace('ResourceFileTypes.RegisterNode("sample.Actor", Actor.Create);','_context = context; _foreign = new Node(); _tree = new SceneTree(_foreign); ResourceFileTypes.RegisterNode("sample.Actor", Actor.Create);')
foreign=foreign.replace('        var root = new Node { Name = "Main" };','        return _foreign!;\n        #pragma warning disable CS0162\n        var root = new Node { Name = "Main" };').replace('    protected override void AfterStep','    public override void Dispose() { _context!.Observe("foreign-preserved", !_foreign!.IsDisposed); _tree!.Dispose(); }\n    protected override void AfterStep')
source.write_text(foreign);failed=run('attached-factory-preserved',expected=2)
assert any(x['name']=='foreign-preserved' and x['boolean'] for x in failed['observations'])
source.write_text(changed.replace('context.Complete();','context.Check("intentional", false, "intentional assertion");'))
run('assertion-failure',expected=5)
source.write_text(changed+'\nthis is invalid CSharp;\n')
failed=run('build-failure',expected=3); assert any(x['code']=='BUILD_FAILED' for x in failed['diagnostics']) and not (root/'build-failure'/'game.stdout.log').exists()
hanging='''    protected override void BeforeStep(BatchContext context)
    {
        var info = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false };
        info.ArgumentList.Add(System.Reflection.Assembly.GetEntryAssembly()!.Location); info.ArgumentList.Add("--hold");
        System.Diagnostics.Process.Start(info);
        System.IO.File.WriteAllText("parent-pid.txt", Environment.ProcessId.ToString());
        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
    }
'''
source.write_text(changed.replace('return BatchHost.Run', '''if (args.Length == 1 && args[0] == "--hold") { System.IO.File.WriteAllText("descendant-pid.txt", Environment.ProcessId.ToString()); System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite); return 0; }
return BatchHost.Run''').replace('    protected override void AfterStep',hanging+'    protected override void AfterStep'))
run('deadline',dict(base,deadlineMilliseconds=3000),124)
for filename in ['parent-pid.txt','descendant-pid.txt']:
    pid=int((project/filename).read_text()); status=Path('/proc')/str(pid)/'stat'
    assert not status.exists() or status.read_text().split()[2]=='Z', (filename,pid)
# Cancellation and a terminal job leader must also retire inherited workers.
for filename in ['parent-pid.txt','descendant-pid.txt']:
    (project/filename).unlink(missing_ok=True)
request=root/'cancel.json'; request.write_text(json.dumps(dict(base,deadlineMilliseconds=30000)))
out=root/'cancel';env=os.environ.copy();env['SDL_VIDEO_DRIVER']=env['SDL_VIDEODRIVER']='dummy';env['VK_DRIVER_FILES']=env['VK_ICD_FILENAMES']=str(root/'missing-vulkan.json')
process=subprocess.Popen(tool+list(map(str,['run',project,'--request',request,'--output',out])),env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
try:
    deadline=time.monotonic()+120
    while not (project/'descendant-pid.txt').exists():
        assert process.poll() is None and time.monotonic()<deadline, 'Cancellation fixture did not start.'
        time.sleep(.02)
    process.send_signal(signal.SIGINT);stdout,stderr=process.communicate(timeout=15);result=json.loads(stdout)
    assert process.returncode==130 and result['exitCode']==130 and not result['success']
    rows.append({'name':'cancel','exit':process.returncode,'expected':130,'result':result});print('cancel',130,flush=True)
finally:
    if process.poll() is None:process.kill();process.wait()
for filename in ['parent-pid.txt','descendant-pid.txt']:
    pid=int((project/filename).read_text());status=Path('/proc')/str(pid)/'stat'
    assert not status.exists() or status.read_text().split()[2]=='Z', (filename,pid)
# Exiting the game while a worker holds its output cannot leave that worker alive.
terminal=source.read_text().replace('System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);\n    }','while (!System.IO.File.Exists("descendant-pid.txt")) System.Threading.Thread.Sleep(1); Environment.Exit(0);\n    }')
assert terminal!=source.read_text()
for filename in ['parent-pid.txt','descendant-pid.txt']:(project/filename).unlink(missing_ok=True)
source.write_text(terminal);run('terminal-parent',dict(base,deadlineMilliseconds=5000),4)
pid=int((project/'descendant-pid.txt').read_text());status=Path('/proc')/str(pid)/'stat';assert not status.exists() or status.read_text().split()[2]=='Z'
# Bounded reporting, logs and cleanup after a user callback error.
source.write_text(changed.replace('context.Complete();','Console.Write(new string(\'x\', 2 * 1024 * 1024)); context.Complete();'))
run('bounded-log');log=root/'bounded-log'/'game.stdout.log';assert 1024*1024<=log.stat().st_size<1024*1024+100 and 'truncated' in log.read_text()[-100:]
source.write_text(changed.replace('    protected override void AfterStep','    protected override void BeforeStep(BatchContext context) { for (var i=0;i<4096;i++) context.Observe("large", new string(\'x\',4096)); }\n    protected override void AfterStep'))
run('bounded-report',expected=4)
source.write_text(changed)
project_file=project/'Game.csproj'; original_project=project_file.read_text()
from xml.sax.saxutils import quoteattr
project_file.write_text(original_project.replace('</Project>', '<Target Name="BlockGameLog" AfterTargets="Build"><MakeDir Directories='+quoteattr(str(root/'log-path-failure'/'game.stdout.log'))+' /></Target></Project>'))
try:
    failed=run('log-path-failure',expected=4); assert any(x['code']=='LOG_FAILED' for x in failed['diagnostics'])
finally: project_file.write_text(original_project)
(project/'linked.e2dscene').symlink_to(scene)
run('linked-scene',dict(base,scene='linked.e2dscene'),2);assert digest(scene)==before
source.write_text(changed)
(root/'results.json').write_text(json.dumps(rows,indent=2)+'\n')
if args.native:
    for backend in ['gpu','compatibility']:
        result=run('rendered-'+backend,dict(base,mode='Rendered',backend=backend,captureSteps=[1,6]),native=True)
        assert result['completedSteps']==6 and result['renderer']==backend and number(result,'position-x')==11
        images=[a for a in result['artifacts'] if a['kind']=='image']; assert [a['step'] for a in images]==[1,6]
        for artifact in images:
            assert digest(artifact['path'])==artifact['sha256']; width,height,color=png(artifact['path']); assert (width,height)==(64,64) and color[1]>150 and color[0]<100
        assert images[0]['sha256']!=images[1]['sha256']
print('Project batch workflow:',len(rows),'cases passed.',flush=True)
