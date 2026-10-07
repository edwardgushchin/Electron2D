#!/usr/bin/env python3
"""Build engine-owned variable, palette and collection acceptance fixtures."""
from pathlib import Path
from tempfile import TemporaryDirectory
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.designspaceLib import DesignSpaceDocument, AxisDescriptor, SourceDescriptor
from fontTools.varLib import build
from fontTools.ttLib import TTCollection
from fontTools.colorLib.builder import buildCOLR, buildCPAL

DEST = Path(__file__).resolve().parents[2] / 'tests/Electron2D.Tests/Fixtures/Fonts'


def box(left, right):
    pen = TTGlyphPen(None)
    pen.moveTo((left, 0)); pen.lineTo((right, 0)); pen.lineTo((right, 750)); pen.lineTo((left, 750)); pen.closePath()
    return pen.glyph()


def master(width, name):
    font = FontBuilder(1000, isTTF=True)
    names = ['.notdef', 'space', 'A', 'left', 'right']
    font.setupGlyphOrder(names); font.setupCharacterMap({32: 'space', 65: 'A'})
    font.setupGlyf({'.notdef': TTGlyphPen(None).glyph(), 'space': TTGlyphPen(None).glyph(), 'A': box(0, width), 'left': box(0, width // 2), 'right': box(width // 2, width)})
    font.setupHorizontalMetrics({n: (width + 100, width // 2 if n == 'right' else 0) for n in names})
    font.setupHorizontalHeader(ascent=750, descent=-250)
    font.setupNameTable({'familyName': 'Electron2D Variation Test', 'styleName': name, 'uniqueFontIdentifier': 'Electron2DVariation' + name, 'fullName': 'Electron2D Variation ' + name, 'psName': 'Electron2DVariation' + name})
    font.setupOS2(sTypoAscender=750, sTypoDescender=-250, usWinAscent=750, usWinDescent=250)
    font.setupPost(); font.setupMaxp(); font.font['head'].created = font.font['head'].modified = 0
    font.font.recalcTimestamp = False
    return font.font


with TemporaryDirectory() as folder:
    document = DesignSpaceDocument()
    axis = AxisDescriptor(); axis.name = 'Weight'; axis.tag = 'wght'; axis.minimum = 100; axis.default = 100; axis.maximum = 900
    document.addAxis(axis)
    for weight, width, name in [(100, 400, 'Thin'), (900, 900, 'Heavy')]:
        font = master(width, name); path = Path(folder) / (name + '.ttf'); font.save(path)
        source = SourceDescriptor(); source.path = str(path); source.name = name; source.location = {'Weight': weight}; source.copyInfo = source.copyLib = source.copyFeatures = weight == 100
        document.addSource(source)
    variable, _, _ = build(document); variable.recalcTimestamp = False; variable['head'].created = variable['head'].modified = 0; variable.save(DEST / 'VariationTest.ttf')

color = master(1000, 'Color')
color['COLR'] = buildCOLR({'A': [('left', 0), ('right', 1)]}, version=0)
color['CPAL'] = buildCPAL([[(1, 0, 0, 1), (0, 0, 1, 1)], [(0, 1, 0, 1), (1, 1, 0, 1)]], paletteLabels=['Primary', 'Alternate'], nameTable=color['name'])
color.save(DEST / 'PaletteTest.ttf')
collection = TTCollection(); collection.fonts = [master(400, 'Thin'), master(900, 'Heavy')]; collection.save(DEST / 'CollectionTest.ttc')
