#!/usr/bin/env python3
"""Generate engine-owned Type 1 outlines and paired auxiliary kerning fixtures (fontTools 4.66.1)."""
from fontTools import t1Lib
from fontTools.misc.psCharStrings import T1CharString
from fontTools.encodings.StandardEncoding import StandardEncoding
from pathlib import Path
f=object.__new__(t1Lib.T1Font);f.encoding='ascii';f.font={
 'FontName':'Electron2DType1Test',
 'FontType':1,'PaintType':0,'FontMatrix':[.001,0,0,.001,0,0],'FontBBox':[0,-200,700,800],
 'FontInfo':{'version':'1.0','Notice':'Engine-owned test fixture.','FullName':'Electron2D Type 1 Test','FamilyName':'Electron2D Type 1 Test','Weight':'Regular','ItalicAngle':0,'isFixedPitch':False,'UnderlinePosition':-100,'UnderlineThickness':50},
 'Encoding':StandardEncoding,
 'Private':{'RD':('string','currentfile','exch','readstring','pop'),'ND':('noaccess','def'),'NP':('noaccess','put'),'lenIV':4,'Subrs':[],'BlueValues':[],'OtherSubrs':[]},
 'CharStrings':{}
}
for n in ['.notdef','space','A','V','O','g','fi']:
 prog=[0,650,'hsbw']
 if n not in ['.notdef','space']:prog += [50,0,'rmoveto',200,750,'rlineto',200,-750,'rlineto',-400,0,'rlineto','closepath']
 prog+=['endchar'];f.font['CharStrings'][n]=T1CharString(program=prog,subrs=[])
f.data=f.createData();d=Path(__file__).resolve().parents[2]/'tests/Electron2D.Tests/Fixtures/Fonts';f.saveAs(d/'type1.pfb','PFB');f.saveAs(d/'type1.pfa','OTHER',dohex=True);print('Generated Type 1 fixtures.')
import struct
(d/'type1.afm').write_text('StartFontMetrics 4.1\nFontName Electron2DType1Test\nStartKernData\nStartKernPairs 1\nKPX A V -120\nEndKernPairs\nEndKernData\nEndFontMetrics\n')
b=bytearray(141);struct.pack_into('<H',b,0,0x100);struct.pack_into('<I',b,2,len(b));struct.pack_into('<H',b,99,0);struct.pack_into('<H',b,117,18);struct.pack_into('<I',b,131,135);struct.pack_into('<H',b,135,1);b[137:139]=bytes([65,86]);struct.pack_into('<h',b,139,-120);(d/'type1.pfm').write_bytes(b)
