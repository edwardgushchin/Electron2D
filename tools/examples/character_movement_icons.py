"""Export platform packaging assets from the approved app-icon raster, without changing its artwork."""
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
GAME = ROOT / "examples/CharacterMovement"
INFO = {"author": "org.electron2d.charactermovement", "version": 1}
SOURCE = Image.open(ROOT / "editor/Assets/Electron2D.png").convert("RGBA")
assert SOURCE.size == (512, 512)


def metadata(folder, **content):
    folder.mkdir(parents=True, exist_ok=True)
    (folder / "Contents.json").write_text(json.dumps({"info": INFO, **content}, indent=2) + "\n")


def canvas(size, foreground=True):
    result = Image.new("RGBA", size, "#F9F3EE")
    if foreground:
        side = int(min(size) * .8)
        mark = SOURCE.resize((side, side), Image.Resampling.NEAREST)
        result.alpha_composite(mark, ((size[0] - side) // 2, (size[1] - side) // 2))
    return result.convert("RGB")


android = GAME / "Platforms/Android/Resources/drawable"
android.mkdir(parents=True, exist_ok=True)
canvas((320, 180)).save(android / "tv_banner.png")

ios = GAME / "Platforms/Apple/iOS/Assets.xcassets"
metadata(ios)
icon = ios / "AppIcon.appiconset"
metadata(icon, images=[{"idiom": "universal", "platform": "ios", "size": "1024x1024", "filename": "app-icon.png"}])
opaque = Image.new("RGBA", (1024, 1024), "#F9F3EE")
opaque.alpha_composite(SOURCE.resize((1024, 1024), Image.Resampling.NEAREST))
opaque.convert("RGB").save(icon / "app-icon.png")

tv = GAME / "Platforms/Apple/tvOS/Assets.xcassets"
metadata(tv)
brand = tv / "AppIcon.brandassets"
assets = []
for name, size in (("Small", (400, 240)), ("Large", (1280, 768))):
    stack = brand / f"{name}.imagestack"
    layers = ["Foreground.imagestacklayer", "Background.imagestacklayer"]
    metadata(stack, layers=[{"filename": name} for name in layers])
    for layer in layers:
        directory = stack / layer / "Content.imageset"
        images = []
        for scale in (1, 2):
            filename = f"app-icon-{scale}x.png"
            target = (size[0] * scale, size[1] * scale)
            if layer.startswith("Foreground"):
                pixels = Image.new("RGBA", target)
                side = int(min(target) * .8)
                pixels.alpha_composite(SOURCE.resize((side, side), Image.Resampling.NEAREST), ((target[0] - side) // 2, (target[1] - side) // 2))
            else:
                pixels = canvas(target, foreground=False)
            directory.mkdir(parents=True, exist_ok=True)
            pixels.save(directory / filename)
            images.append({"idiom": "tv", "scale": f"{scale}x", "filename": filename})
        metadata(directory, images=images)
    assets.append({"idiom": "tv", "size": f"{size[0]}x{size[1]}", "role": "primary-app-icon", "filename": stack.name})

for name, size, role in (("TopShelf", (1920, 720), "top-shelf-image"), ("TopShelfWide", (2320, 720), "top-shelf-image-wide")):
    directory = brand / f"{name}.imageset"
    images = []
    for scale in (1, 2):
        filename = f"app-icon-{scale}x.png"
        directory.mkdir(parents=True, exist_ok=True)
        canvas((size[0] * scale, size[1] * scale)).save(directory / filename)
        images.append({"idiom": "tv", "scale": f"{scale}x", "filename": filename})
    metadata(directory, images=images)
    assets.append({"idiom": "tv", "size": f"{size[0]}x{size[1]}", "role": role, "filename": directory.name})
metadata(brand, assets=assets)

for catalog in (ios, tv):
    for path in catalog.rglob("Contents.json"):
        data = json.loads(path.read_text())
        for row in data.get("images", []) + data.get("layers", []) + data.get("assets", []):
            assert (path.parent / row["filename"]).exists(), path
print("CharacterMovement Android banner, opaque iOS icon and layered tvOS icons/top shelf exported and checked.")
