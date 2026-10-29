"""Exercise explicit mesh selection against newer packaged build/publish assets."""
import unittest
import test_webp_delivery

class MeshDeliveryTests(test_webp_delivery.WebPDeliveryTests):
    property_name = "Electron2DMeshNativeLibrary"
    linux_name = "libElectron2DMesh.so"
    macos_name = "libElectron2DMesh.dylib"

if __name__ == "__main__":
    unittest.main()
