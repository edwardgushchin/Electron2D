"""Exercise explicit video bridge selection against packaged build/publish assets."""
import unittest
import test_webp_delivery


class OGVDeliveryTests(test_webp_delivery.WebPDeliveryTests):
    property_name = "Electron2DOGVNativeLibrary"
    linux_name = "libElectron2DOGV.so"
    macos_name = "libElectron2DOGV.dylib"


if __name__ == "__main__":
    unittest.main()
