#!/usr/bin/env python3
"""Rebuild the tiny engine-owned COLR/CPAL glyph fixture with fontTools."""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.colorLib.builder import buildCOLR, buildCPAL


def box(left, right):
    pen = TTGlyphPen(None)
    pen.moveTo((left, 0)); pen.lineTo((right, 0)); pen.lineTo((right, 750)); pen.lineTo((left, 750)); pen.closePath()
    return pen.glyph()


font = FontBuilder(1000, isTTF=True)
font.setupGlyphOrder(['.notdef', 'A', 'red', 'blue'])
font.setupCharacterMap({65: 'A'})
font.setupGlyf({'.notdef': TTGlyphPen(None).glyph(), 'A': box(0, 1000), 'red': box(0, 500), 'blue': box(500, 1000)})
font.setupHorizontalMetrics({name: (1000, 500 if name == 'blue' else 0) for name in ['.notdef', 'A', 'red', 'blue']})
font.setupHorizontalHeader(ascent=750, descent=0)
font.setupNameTable({'familyName': 'Electron2D Color Test', 'styleName': 'Regular', 'uniqueFontIdentifier': 'Electron2DColorTest', 'fullName': 'Electron2D Color Test', 'psName': 'Electron2DColorTest'})
font.setupOS2(sTypoAscender=750, sTypoDescender=0, usWinAscent=750, usWinDescent=0)
font.setupPost(); font.setupMaxp()
font.font['COLR'] = buildCOLR({'A': [('red', 0), ('blue', 1)]}, version=0)
font.font['CPAL'] = buildCPAL([[(1, 0, 0, 1), (0, 0, 1, 1)]])
font.font['head'].created = font.font['head'].modified = 0
font.font.recalcTimestamp = False
font.save(Path(__file__).resolve().parents[2] / 'tests/Electron2D.Tests/Fixtures/Fonts/ColorTest.ttf')
