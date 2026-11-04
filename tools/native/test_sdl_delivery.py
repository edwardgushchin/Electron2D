"""Exercise owned SDL selection against packaged build/publish assets."""

import unittest
import test_native_delivery


class SDLDeliveryTests(test_native_delivery.NativeDeliveryTests):
    property_name = "Electron2DSDLNativeLibrary"
    linux_name = "libSDL3.so.0"
    macos_name = "libSDL3.0.dylib"


if __name__ == "__main__":
    unittest.main()
