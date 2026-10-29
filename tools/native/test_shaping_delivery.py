"""Select the owned shaping library over packaged build/publish assets."""

import unittest
import test_webp_delivery


class ShapingDeliveryTests(test_webp_delivery.WebPDeliveryTests):
    property_name = "Electron2DHarfBuzzNativeLibrary"
    linux_name = "libHarfBuzzSharp.so"
    macos_name = "libHarfBuzzSharp.dylib"


if __name__ == "__main__":
    unittest.main()
