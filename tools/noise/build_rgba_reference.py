#!/usr/bin/env python3
"""Execute the pinned RGBA seamless kernel with byte-container adapters only."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
from urllib.request import urlopen

PIN = "ed1daf0bf001b61586d9930840f2f1394092c079"
parser = argparse.ArgumentParser()
parser.add_argument("build_directory", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
args.build_directory.mkdir(parents=True, exist_ok=True)
header_file = args.build_directory / "noise.h"
if not header_file.exists():
    header_file.write_bytes(urlopen(f"https://raw.githubusercontent.com/godotengine/godot/{PIN}/modules/noise/noise.h", timeout=30).read())
header = header_file.read_text()
body = header[header.index("\ttemplate <typename T>\n\tstruct img_buff"):header.index("\nprotected:")]
license_text = header[:header.index("#pragma once")]
main = r'''
int main() {
    Noise noise;
    bool comma = false;
    std::cout << '[';
    for (auto dimensions : {std::pair{1, 1}, std::pair{7, 9}, std::pair{20, 14}, std::pair{8, 8}})
    for (float skirt : {0.0f, 0.1f, 0.25f, 0.35f, 0.5f}) {
        auto [w, h] = dimensions;
        int sw = w + std::max(1, int(w * skirt)), sh = h + std::max(1, int(h * skirt));
        Vector<uint8_t> pixels; pixels.resize(sw * sh * 4);
        for (int y = 0; y < sh; y++) for (int x = 0; x < sw; x++) for (int c = 0; c < 4; c++) pixels.write[(y * sw + x) * 4 + c] = uint8_t(x * 37 + y * 53 + c * 17);
        Vector<Ref<Image>> source; source.resize(1); source.write[0] = memnew(Image(sw, sh, false, Image::FORMAT_RGBA8, pixels));
        auto result = noise._generate_seamless_image<uint32_t>(source, w, h, 1, false, skirt)[0]->get_data();
        if (comma) std::cout << ','; comma = true;
        std::cout << "{\"width\":" << w << ",\"height\":" << h << ",\"skirt\":" << std::setprecision(9) << skirt << ",\"data\":\"";
        constexpr char hex[] = "0123456789abcdef";
        for (int i = 0; i < result.size(); i++) std::cout << hex[result[i] >> 4] << hex[result[i] & 15];
        std::cout << "\"}";
    }
    std::cout << ']';
}
'''
source = args.build_directory / "rgba-reference.cpp"
source.write_text(license_text + Path(__file__).with_name("reference_image_shim.h").read_text() + "\nclass Noise { public:\n" + body + "\n};\n" + main)
binary = args.build_directory / "rgba-reference"
subprocess.run(["c++", "-std=c++17", "-O2", "-ffp-contract=off", str(source), "-o", str(binary)], check=True)
cases = json.loads(subprocess.check_output([str(binary)], text=True))
args.output.write_text(json.dumps({"commit": PIN, "header_sha256": hashlib.sha256(header_file.read_bytes()).hexdigest(), "cases": cases}, indent=2) + "\n")
print(f"{len(cases)} RGBA seamless cases from unchanged pinned templates")
