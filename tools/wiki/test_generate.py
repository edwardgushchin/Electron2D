#!/usr/bin/env python3
"""Small regression checks for wiki member rendering."""

from xml.etree import ElementTree as ET

from generate import member_heading, plain


assert plain(ET.fromstring('<returns><see langword="true"/> when <paramref name="other"/> overlaps.</returns>')) == "true when other overlaps."
assert member_heading({"kind": "constructor", "declaringType": "Electron2D.Rect2i", "name": ".ctor",
                       "parameters": [{"type": "System.Int32"}]}) == "Rect2i(System.Int32)"
