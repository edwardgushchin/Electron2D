#!/usr/bin/env python3
"""Generate an engine-owned scalable face with a distinct monochrome bitmap strike."""
from pathlib import Path
import copy, struct, zlib
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import newTable, TTFont
from fontTools.ttLib.tables import E_B_L_C_, E_B_D_T_, C_B_D_T_
from fontTools.ttLib.tables.BitmapGlyphMetrics import SmallGlyphMetrics
from fontTools.misc import sstruct

root = Path(__file__).resolve().parents[2]
font = FontBuilder(1000, isTTF=True)
font.setupGlyphOrder(['.notdef', 'space', 'A', 'B'])
font.setupCharacterMap({32: 'space', 65: 'A', 66: 'B'})
pen = TTGlyphPen(None)
pen.moveTo((0, 0)); pen.lineTo((600, 0)); pen.lineTo((600, 750)); pen.lineTo((0, 750)); pen.closePath()
font.setupGlyf({'.notdef': TTGlyphPen(None).glyph(), 'space': TTGlyphPen(None).glyph(), 'A': pen.glyph(), 'B': pen.glyph()})
font.setupHorizontalMetrics({name: (700, 0) for name in ['.notdef', 'space', 'A', 'B']})
font.setupHorizontalHeader(ascent=750, descent=-250)
font.setupNameTable({'familyName': 'Electron2D Strike Test', 'styleName': 'Regular', 'uniqueFontIdentifier': 'Electron2DStrikeTest', 'fullName': 'Electron2D Strike Test', 'psName': 'Electron2DStrikeTest'})
font.setupOS2(sTypoAscender=750, sTypoDescender=-250, usWinAscent=750, usWinDescent=250)
font.setupPost(); font.setupMaxp()
font.font['head'].created = font.font['head'].modified = 0
font.font.recalcTimestamp = False
locator = newTable('EBLC'); locator.version = 2.0
strike = E_B_L_C_.Strike(); size = strike.bitmapSizeTable
for name in size._getXMLMetricNames(): setattr(size, name, 0)
size.ppemX = size.ppemY = 16; size.bitDepth = 1; size.flags = 1
for direction in ['hori', 'vert']:
    metrics = E_B_L_C_.SbitLineMetrics()
    for name in sstruct.getformat(E_B_L_C_.sbitLineMetricsFormat)[1]: setattr(metrics, name, 0)
    metrics.ascender = 7; metrics.descender = 0; metrics.widthMax = 5; metrics.caretSlopeNumerator = 1
    setattr(size, direction, metrics)
index = E_B_L_C_.eblc_index_sub_table_1(None, font.font)
index.indexFormat = 1; index.imageFormat = 1; index.names = ['A']
strike.indexSubTables = [index]; locator.strikes = [strike]
data = newTable('EBDT'); data.version = 2.0
bitmap = E_B_D_T_.ebdt_bitmap_format_1(None, font.font); bitmap.metrics = SmallGlyphMetrics()
for name, value in dict(height=7, width=5, BearingX=0, BearingY=7, Advance=6).items(): setattr(bitmap.metrics, name, value)
bitmap.imageData = bytes([0x70, 0x88, 0x88, 0xF8, 0x88, 0x88, 0x88])
data.strikeData = [{'A': bitmap}]
font.font['EBLC'] = locator; font.font['EBDT'] = data
path = root / 'tests/Electron2D.Tests/Fixtures/Fonts/StrikeTest.ttf'
font.save(path)
loaded = TTFont(path); assert loaded['EBDT'].strikeData[0]['A'].imageData == bitmap.imageData
assert loaded['EBLC'].strikes[0].bitmapSizeTable.ppemY == 16
print(path.relative_to(root), path.stat().st_size, 'bytes')

color = TTFont(path); color.recalcTimestamp = False
del color['EBLC']; del color['EBDT']
color_locator = newTable('CBLC'); color_locator.version = 3.0
color_strike = copy.deepcopy(strike); color_strike.bitmapSizeTable.bitDepth = 32; color_strike.indexSubTables[0].imageFormat = 17
color_locator.strikes = [color_strike]
color_data = newTable('CBDT'); color_data.version = 3.0
color_glyph = C_B_D_T_.cbdt_bitmap_format_17(None, color); color_glyph.metrics = copy.deepcopy(bitmap.metrics)
def chunk(kind, payload):
    return struct.pack('>I', len(payload)) + kind + payload + struct.pack('>I', zlib.crc32(kind + payload))
rows = b''.join(b'\0' + b''.join(bytes([255, 0, 0, 255 if row & (0x80 >> x) else 0]) for x in range(5)) for row in bitmap.imageData)
color_glyph.imageData = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 5, 7, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(rows)) + chunk(b'IEND', b'')
color_data.strikeData = [{'A': color_glyph}]; color['CBLC'] = color_locator; color['CBDT'] = color_data
color_path = path.with_name('ColorStrikeTest.ttf'); color.save(color_path)
check = TTFont(color_path); assert check['CBDT'].strikeData[0]['A'].imageData == color_glyph.imageData
print(color_path.relative_to(root), color_path.stat().st_size, 'bytes')
