"""Exercise owned SDL selection against packaged build/publish assets."""

import unittest
import test_webp_delivery


class SDLDeliveryTests(test_webp_delivery.WebPDeliveryTests):
    property_name = "Electron2DSDLNativeLibrary"
    linux_name = "libSDL3.so.0"
    macos_name = "libSDL3.0.dylib"


if __name__ == "__main__":
    unittest.main()
