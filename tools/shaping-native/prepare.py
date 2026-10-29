"""Prepare a checked shaping source copy; refused lazy state must never become a cache entry."""

import hashlib
from pathlib import Path
import shutil
import sys

SOURCE_SHA256 = "38851838c6a2ed1caf09e9ab0074fa69dc516f5ce802c4cf2a7a3a6d425b53bb"
ORIGINAL = """      p = this->template call_create<Stored, Funcs> ();
      if (unlikely (!p))"""
REPLACEMENT = """      const uint64_t allocation_epoch = e2d_memory_failure_epoch ();
      p = this->template call_create<Stored, Funcs> ();
      if (unlikely (allocation_epoch != e2d_memory_failure_epoch ()))
      {
        do_destroy (p);
        return const_cast<Stored *> (Funcs::get_null ());
      }
      if (unlikely (!p))"""


def prepare(source, destination, epoch="e2d_memory_failure_epoch"):
    if epoch not in ("e2d_memory_failure_epoch", "e2d_ft_hb_failure_epoch"):
        raise ValueError("Unknown shaping allocator error producer")
    raw = (source / "hb-machinery.hh").read_bytes()
    if hashlib.sha256(raw).hexdigest() != SOURCE_SHA256:
        raise ValueError("The pinned shaping lazy-loader source changed")
    text = raw.decode("utf-8")
    if text.count(ORIGINAL) != 1 or text.count('#include "hb.hh"') != 1:
        raise ValueError("The shaping lazy-loader allocation boundary changed")
    shutil.copytree(source, destination, dirs_exist_ok=True)
    text = text.replace('#include "hb.hh"', '#include "hb.hh"\nextern "C" uint64_t ' + epoch + ' (void);')
    (destination / "hb-machinery.hh").write_text(
        "/* Generated Electron2D shaping allocation ownership profile. */\n" + text.replace(ORIGINAL, REPLACEMENT.replace("e2d_memory_failure_epoch", epoch)), encoding="utf-8")


if __name__ == "__main__":
    prepare(Path(sys.argv[1]), Path(sys.argv[2]), *(sys.argv[3:]))
