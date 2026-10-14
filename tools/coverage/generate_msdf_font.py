#!/usr/bin/env python3
"""Generate an engine-owned CFF cubic-outline acceptance fixture."""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.t2CharStringPen import T2CharStringPen

names = ['.notdef', 'space', 'A', 'O', 'g']
font = FontBuilder(1000, isTTF=False)
font.setupGlyphOrder(names)
font.setupCharacterMap({32: 'space', 65: 'A', 79: 'O', 103: 'g'})
strings = {}
for name in names:
    pen = T2CharStringPen(650, None)
    if name not in ['.notdef', 'space']:
        pen.moveTo((60, 0))
        pen.curveTo((20, 250), (40, 670), (300, 730))
        pen.curveTo((620, 660), (600, 300), (570, 0))
        pen.lineTo((60, 0))
        pen.closePath()
        if name == 'O':
            pen.moveTo((180, 160))
            pen.lineTo((460, 160))
            pen.curveTo((480, 370), (420, 590), (310, 590))
            pen.curveTo((180, 590), (150, 360), (180, 160))
            pen.closePath()
    strings[name] = pen.getCharString()
font.setupCFF('Electron2DMSDFTest', {'FullName': 'Electron2D MSDF Test', 'FamilyName': 'Electron2D MSDF Test', 'Weight': 'Regular'}, strings, {})
font.setupHorizontalMetrics({name: (650, 0) for name in names})
font.setupHorizontalHeader(ascent=800, descent=-200)
font.setupNameTable({'familyName': 'Electron2D MSDF Test', 'styleName': 'Regular', 'uniqueFontIdentifier': 'Electron2DMSDFTest', 'fullName': 'Electron2D MSDF Test', 'psName': 'Electron2DMSDFTest'})
font.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
font.setupPost()
font.font.recalcTimestamp = False
font.font['head'].created = font.font['head'].modified = 2082844800
font.save(Path(__file__).resolve().parents[2] / 'tests/Electron2D.Tests/Fixtures/Fonts/MSDFTest.otf')
