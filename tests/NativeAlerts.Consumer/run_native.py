from pathlib import Path
import os,sys,subprocess,tempfile,time,re,select,json,shutil,ctypes
p=Path(sys.argv[2]).resolve();p.mkdir(parents=True,exist_ok=True);profile=sys.argv[1];records=[]
root=Path(__file__).resolve().parents[2]
binary=root/'tests/NativeAlerts.Consumer/bin/aot/NativeAlerts.Consumer' if profile=='aot' else root/'tests/NativeAlerts.Consumer/bin/Release/net10.0/linux-x64/NativeAlerts.Consumer.dll'
base=[str(binary)] if profile=='aot' else [shutil.which('dotnet'),str(binary)]
class MessageData(ctypes.Union):
 _fields_=[('l',ctypes.c_long*5)]
class ClientMessage(ctypes.Structure):
 _fields_=[('type',ctypes.c_int),('serial',ctypes.c_ulong),('send_event',ctypes.c_int),('display',ctypes.c_void_p),('window',ctypes.c_ulong),('message_type',ctypes.c_ulong),('format',ctypes.c_int),('data',MessageData)]
class Event(ctypes.Union):
 _fields_=[('client',ClientMessage),('padding',ctypes.c_long*24)]
def close_dialog(display,window):
 x=ctypes.CDLL('libX11.so.6');x.XOpenDisplay.argtypes=[ctypes.c_char_p];x.XOpenDisplay.restype=ctypes.c_void_p
 x.XInternAtom.argtypes=[ctypes.c_void_p,ctypes.c_char_p,ctypes.c_int];x.XInternAtom.restype=ctypes.c_ulong
 x.XSendEvent.argtypes=[ctypes.c_void_p,ctypes.c_ulong,ctypes.c_int,ctypes.c_long,ctypes.POINTER(Event)]
 x.XFlush.argtypes=[ctypes.c_void_p];x.XCloseDisplay.argtypes=[ctypes.c_void_p]
 # WM_DELETE_WINDOW requests a user close; XDestroyWindow would bypass modal completion.
 previous=os.environ.get('XAUTHORITY');os.environ['XAUTHORITY']=env['XAUTHORITY']
 try:connection=x.XOpenDisplay(display.encode())
 finally:
  if previous is None:os.environ.pop('XAUTHORITY',None)
  else:os.environ['XAUTHORITY']=previous
 assert connection
 try:
  event=Event();event.client.type=33;event.client.send_event=1;event.client.display=connection;event.client.window=int(window);event.client.message_type=x.XInternAtom(connection,b'WM_PROTOCOLS',0);event.client.format=32;event.client.data.l[0]=x.XInternAtom(connection,b'WM_DELETE_WINDOW',0)
  assert x.XSendEvent(connection,int(window),0,0,ctypes.byref(event));x.XFlush(connection)
 finally:x.XCloseDisplay(connection)
with tempfile.TemporaryDirectory(prefix='e2d-alerts-') as runtime:
 env=os.environ.copy();env.update(XDG_RUNTIME_DIR=runtime,WAYLAND_DISPLAY='e2d-alerts',LIBGL_ALWAYS_SOFTWARE='1',XWAYLAND_NO_GLAMOR='1',NO_AT_BRIDGE='1',GTK_A11Y='none',GSK_RENDERER='cairo',GDK_BACKEND='x11',GTK_USE_PORTAL='0',GIO_USE_VFS='local');env.pop('DISPLAY',None);env.pop('LD_LIBRARY_PATH',None);env.pop('WAYLAND_SOCKET',None)
 with (p/('native-'+profile+'-mutter.log')).open('w') as out:
  compositor=subprocess.Popen(['mutter','--headless','--virtual-monitor','640x480','--wayland-display','e2d-alerts'],env=env,stdout=out,stderr=subprocess.STDOUT)
  child=None
  try:
   deadline=time.monotonic()+10;display=None
   while time.monotonic()<deadline:
    if compositor.poll() is not None:raise RuntimeError('Private compositor exited')
    log=(p/('native-'+profile+'-mutter.log')).read_text();match=re.search(r'Using public X11 display (:\d+)',log)
    auth=list(Path(runtime).glob('.mutter-Xwaylandauth.*'))
    if match and auth:display=match[1];env.update(DISPLAY=display,XAUTHORITY=str(auth[0]),SDL_VIDEO_DRIVER='x11',SDL_VIDEODRIVER='x11');break
    time.sleep(.05)
   else:raise RuntimeError('Private X11 desktop not ready')
   subprocess.run(['xdotool','getdisplaygeometry'],env=env,capture_output=True,check=True,timeout=10)
   child=subprocess.Popen([*base,'native'],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,bufsize=1)
   with (p/('native-'+profile+'.log')).open('w') as log:
    deadline=time.monotonic()+90
    while time.monotonic()<deadline:
     ready,_,_=select.select([child.stdout],[],[],.2)
     if not ready:
      if child.poll() is not None:break
      continue
     line=child.stdout.readline()
     if not line:
      if child.poll() is not None:break
      continue
     log.write(line);log.flush()
     if not line.startswith('READY\t'):continue
     _,phase,title=line.rstrip('\n').split('\t',2);window=None
     lookup_deadline=time.monotonic()+10
     while time.monotonic()<lookup_deadline:
      query=['--name','^'+re.escape(title)+'$'] if title else ['--class','zenity']
      result=subprocess.run(['xdotool','search','--onlyvisible',*query],env=env,capture_output=True,text=True,timeout=3)
      if result.returncode==0:
       for candidate in result.stdout.splitlines():
        properties=subprocess.run(['xprop','-id',candidate,'_NET_WM_WINDOW_TYPE','WM_PROTOCOLS','WM_CLASS'],env=env,capture_output=True,text=True).stdout
        if 'WM_DELETE_WINDOW' in properties and ('zenity' in properties.lower() or '_NET_WM_WINDOW_TYPE_DIALOG' in properties):window=candidate;break
       if window is not None:break
      if child.poll() is not None:break
      time.sleep(.05)
     if window is None:raise RuntimeError('Dialog not found: '+phase)
     time.sleep(.25)
     actual=subprocess.check_output(['xdotool','getwindowname',window],env=env,text=True).rstrip('\n');assert not title or actual==title,(actual,title)
     transient=subprocess.run(['xprop','-id',window,'WM_TRANSIENT_FOR'],env=env,capture_output=True,text=True,check=True).stdout
     if phase in ['owned','callback','nested']:
      parent=subprocess.check_output(['xdotool','search','--name','^E2D alert parent$'],env=env,text=True).splitlines()[0]
      if 'window id' in transient:assert hex(int(parent)).lower() in transient.lower(),transient
     else:assert 'window id' not in transient,transient
     geometry=subprocess.check_output(['xdotool','getwindowgeometry','--shell',window],env=env,text=True)
     screenshot=None
     if phase in ['before','owned']:
      screenshot=str(p/('native-'+profile+'-'+phase+'.png'))
      subprocess.run(['import','-display',display,'-window',window,screenshot],env=env,capture_output=True,check=True,timeout=10)
     records.append({'phase':phase,'title':actual,'native_window':window,'transient':transient.strip(),'geometry':geometry.strip(),'screenshot':screenshot});print(profile,phase,'native window',window,flush=True)
     close_dialog(display,window)
    else:raise RuntimeError('Native alert consumer deadline expired')
    exit_code=child.wait(10);log.write(child.stdout.read());assert exit_code==0,exit_code;assert len(records)==7,len(records)
   (p/('native-'+profile+'.json')).write_text(json.dumps({'command':[*base,'native'],'private_desktop':True,'backend':'SDL system message-box provider; private X11, software GTK rendering','dialogs':records,'exit':exit_code},indent=2)+'\n');print('PASS',profile,'seven real native dialogs',flush=True)
  finally:
   if child is not None and child.poll() is None:child.terminate();child.wait(10)
   compositor.terminate()
   try:compositor.wait(10)
   except subprocess.TimeoutExpired:compositor.kill();compositor.wait()
