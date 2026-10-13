#!/usr/bin/env python3
"""Generate owned name/feature fixtures with an actual alternate glyph consumer."""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString

root = Path(__file__).resolve().parents[2]
builder = FontBuilder(1000, isTTF=True)
order = ['.notdef', 'space', 'A', 'B', 'C']
builder.setupGlyphOrder(order)
builder.setupCharacterMap({32: 'space', 65: 'A', 66: 'B', 67: 'C'})
glyphs = {'.notdef': TTGlyphPen(None).glyph(), 'space': TTGlyphPen(None).glyph()}
for name, width in [('A', 300), ('B', 700), ('C', 500)]:
    pen = TTGlyphPen(None)
    pen.moveTo((0, 0)); pen.lineTo((width, 0)); pen.lineTo((width, 750)); pen.lineTo((0, 750)); pen.closePath()
    glyphs[name] = pen.glyph()
builder.setupGlyf(glyphs)
builder.setupHorizontalMetrics({name: (width, 0) for name, width in zip(order, [400, 250, 400, 800, 600])})
builder.setupHorizontalHeader(ascent=750, descent=-250)
builder.setupNameTable({'familyName': 'Electron2D Metadata Test', 'styleName': 'Regular',
                       'uniqueFontIdentifier': 'Electron2DMetadataTest', 'fullName': 'Electron2D Metadata Test',
                       'psName': 'Electron2DMetadataTest', 'version': 'Version 1.0',
                       'licenseDescription': 'Engine-owned test font', 'sampleText': 'ABC'})
builder.font['name'].setName('メタデータ試験', 1, 3, 1, 0x411)
builder.font['name'].setName('Unknown role Ω', 400, 3, 1, 0x409)
builder.font['name'].setName('', 401, 3, 1, 0x409)
builder.setupOS2(sTypoAscender=750, sTypoDescender=-250, usWinAscent=750, usWinDescent=250)
builder.setupPost(); builder.setupMaxp()
features = '''
languagesystem DFLT dflt;
languagesystem latn dflt;
feature ss01 {
    featureNames { name "Wide A"; name 3 1 0x411 "幅広A"; };
    sub A by B;
} ss01;
feature rlig { sub C by B; } rlig;
feature zzzz { sub A by C; } zzzz;
feature kern { pos A A -100; } kern;
'''
addOpenTypeFeaturesFromString(builder.font, features)
directory = root / 'tests/Electron2D.Tests/Fixtures/Fonts'
builder.font['head'].created = builder.font['head'].modified = 2082844800
builder.save(directory / 'MetadataTest.ttf')
addOpenTypeFeaturesFromString(builder.font, features + 'feature ss01 { pos A A -50; } ss01;')
for record in builder.font['GPOS'].table.FeatureList.FeatureRecord:
    if record.FeatureTag == 'ss01':
        record.Feature.FeatureParams = None
builder.font['head'].created = builder.font['head'].modified = 2082844800
builder.save(directory / 'MetadataDuplicateTest.ttf')

for name_id in range(1000, 2200):
    builder.font['name'].setName('X' * 30000, name_id, 3, 1, 0x409)
builder.font['head'].created = builder.font['head'].modified = 2082844800
builder.save(directory / 'MetadataBudgetTest.ttf')
