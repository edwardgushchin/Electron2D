from pathlib import Path
import gi
import os,json,subprocess,tempfile,time,queue,sys,threading,hashlib

gi.require_version('Gio','2.0')
from gi.repository import Gio,GLib
root=Path(__file__).resolve().parents[2]
backend=sys.argv[1];profile=sys.argv[2];proof=Path(sys.argv[3]).resolve()
only=sys.argv[4] if len(sys.argv)>4 else None
assert backend in ['gpu','compatibility'] and profile in ['clr','aot']
proof.mkdir(parents=True,exist_ok=True)
cmd=['dotnet',str(root/'tests/ClipboardImages.Consumer/bin/Release/net10.0/linux-x64/ClipboardImages.Consumer.dll')] if profile=='clr' else [str(root/'tests/ClipboardImages.Consumer/bin/aot/ClipboardImages.Consumer')]
images=proof/'images'
records=[]
with tempfile.TemporaryDirectory(prefix='e2d-clipboard-') as runtime:
 os.environ['XDG_RUNTIME_DIR']=runtime;os.environ['WAYLAND_DISPLAY']='e2d-clipboard';os.environ.pop('DISPLAY',None)
 os.environ['SDL_VIDEODRIVER']='wayland';os.environ['SDL_VIDEO_DRIVER']='wayland'
 os.environ['SDL_GPU_DRIVER']='vulkan';os.environ['LIBGL_ALWAYS_SOFTWARE']='1'
 os.environ['GTK_USE_PORTAL']='0';os.environ['GIO_USE_VFS']='local'
 with (proof/f'mutter-{profile}-{backend}.log').open('w') as out:
  compositor=subprocess.Popen(['mutter','--headless','--virtual-monitor','640x480','--no-x11','--wayland-display','e2d-clipboard'],stdout=out,stderr=subprocess.STDOUT)
  client=None
  try:
   bus=Gio.bus_get_sync(Gio.BusType.SESSION,None)
   for attempt in range(100):
    try:
     path=bus.call_sync('org.gnome.Mutter.RemoteDesktop','/org/gnome/Mutter/RemoteDesktop','org.gnome.Mutter.RemoteDesktop','CreateSession',None,None,Gio.DBusCallFlags.NONE,1000,None).unpack()[0];break
    except GLib.Error:
     if compositor.poll() is not None:raise RuntimeError('Compositor ended '+str(compositor.returncode))
     time.sleep(.1)
   else:raise RuntimeError('No compositor remote desktop service')
   iface='org.gnome.Mutter.RemoteDesktop.Session';destination='org.gnome.Mutter.RemoteDesktop'
   def call(method,params=None):return bus.call_sync(destination,path,iface,method,params,None,Gio.DBusCallFlags.NONE,5000,None)
   call('Start');call('EnableClipboard',GLib.Variant('(a{sv})',({},)))
   context=GLib.MainContext.default();offers={};transfers=[];errors=[];writers=[];cancelled=[];retiring=False
   def record_error(error,mime,serial,operation):
    message=str(error)
    if retiring and f"Transfer serial {serial} doesn't match any transfer request" in message:
     cancelled.append({'mime':mime,'serial':serial,'operation':operation,'reason':'source retired before native request completed'})
    else:errors.append(repr(error))
   def selection(bus,sender,obj,interface,signal,params,data):
    mime,serial=params.unpack()
    try:
     reply,fds=bus.call_with_unix_fd_list_sync(destination,path,iface,'SelectionWrite',GLib.Variant('(u)',(serial,)),None,Gio.DBusCallFlags.NONE,5000,None,None)
     fd=fds.get(reply.unpack()[0]);payload=offers.get(mime,b'')
     def write():
      try:
       with os.fdopen(fd,'wb',buffering=0) as output:
        data=memoryview(payload)
        while data:
         n=output.write(data);data=data[n:]
       call('SelectionWriteDone',GLib.Variant('(ub)',(serial,True)))
      except Exception as e:record_error(e,mime,serial,'complete')
     writer=threading.Thread(target=write,daemon=True);writers.append(writer);writer.start();transfers.append({'mime':mime,'bytes':len(payload),'sha256':hashlib.sha256(payload).hexdigest()})
    except Exception as e:record_error(e,mime,serial,'open')
   subscription=bus.signal_subscribe(destination,iface,'SelectionTransfer',path,None,Gio.DBusSignalFlags.NONE,selection,None)
   def publish(data):
    offers.clear();offers.update(data);call('SetSelection',GLib.Variant('(a{sv})',({'mime-types':GLib.Variant('as',list(data))},)))
   cases=[('png','image/png','png'),('jpeg','image/jpeg','jpeg'),('webp','image/webp','webp'),('svg','image/svg+xml','svg'),('bmp','image/bmp','bmp'),('tga','image/x-tga','tga'),('targa','image/x-targa','tga'),('ktx','image/ktx','ktx'),('exr','image/x-exr','exr'),('none','text/uri-list',None),('invalid','image/png',None),('empty','image/png',None),('priority','image/png',None),('oversize','image/png',None)]
   for scenario,mime,source in cases:
    if only and scenario!=only:continue
    transfers.clear();errors.clear();writers.clear();cancelled.clear();retiring=False;data={mime:(images/source).read_bytes() if source else b'file:///tmp/example.png\r\n'}
    if scenario in ['invalid','priority']:data[mime]=b'not a PNG'
    if scenario=='priority':data['image/jpeg']=(images/'jpeg').read_bytes()
    if scenario=='empty':data[mime]=b''
    if scenario=='oversize':data[mime]=bytes(64*1024*1024+1)
    data['text/plain;charset=utf-8']=('e2d-clipboard-'+scenario).encode();publish(data)
    with (proof/f'{profile}-{backend}-{scenario}.log').open('w') as log:
     client=subprocess.Popen(cmd+[backend,scenario],cwd=root,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,bufsize=1)
     lines=queue.Queue()
     def read_lines():
      for line in client.stdout:lines.put(line)
     reader=threading.Thread(target=read_lines,daemon=True);reader.start()
     def pending_lines():
      while True:
       try:yield lines.get_nowait()
       except queue.Empty:return
     started=time.monotonic();revoked=False;traced=False
     while client.poll() is None:
      while context.pending():context.iteration(False)
      for line in pending_lines():
       log.write(line);log.flush()
       if line.strip()=='READY':
        call('NotifyPointerMotionRelative',GLib.Variant('(dd)',(1.,1.)))
        call('NotifyKeyboardKeysym',GLib.Variant('(ub)',(0xffe1,True)));call('NotifyKeyboardKeysym',GLib.Variant('(ub)',(0xffe1,False)))
       if line.strip()=='DECODED':
        retiring=True;publish({'text/plain;charset=utf-8':b'revoked'});revoked=True
      if time.monotonic()-started>6 and not traced:
       traced=True
       (proof/f'{profile}-{backend}-{scenario}-waiting.json').write_text(json.dumps({'pid':client.pid,'revoked':revoked,'transfers':list(transfers),'errors':list(errors),'wchan':Path(f'/proc/{client.pid}/wchan').read_text()},indent=2)+'\n')
      if time.monotonic()-started>35:
       client.kill();client.wait();raise TimeoutError(f'{profile}/{backend}/{scenario}')
      time.sleep(.005)
     reader.join(timeout=5)
     if reader.is_alive():raise TimeoutError('Client output reader did not complete')
     for line in pending_lines():log.write(line)
    while context.pending():context.iteration(False)
    for writer in writers:
     writer.join(timeout=5)
     if writer.is_alive():raise TimeoutError('Clipboard producer did not complete')
    row={'scenario':scenario,'exit':client.returncode,'seconds':time.monotonic()-started,'revoked':revoked,'transfers':list(transfers),'errors':list(errors),'cancelled_after_retirement':list(cancelled)};records.append(row)
    (proof/f'{profile}-{backend}.json').write_text(json.dumps(records,indent=2)+'\n')
    print(profile,backend,scenario,client.returncode,'transfers',len(transfers),'errors',errors,flush=True)
    if client.returncode or errors:raise RuntimeError((proof/f'{profile}-{backend}-{scenario}.log').read_text()[-2500:])
   bus.signal_unsubscribe(subscription);call('Stop')
  finally:
   if client and client.poll() is None:client.kill();client.wait()
   compositor.terminate()
   try:compositor.wait(timeout=5)
   except subprocess.TimeoutExpired:compositor.kill();compositor.wait()
