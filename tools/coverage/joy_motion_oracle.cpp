#include "GamepadMotion.hpp"
#include <cstdio>

int main() {
    GamepadMotion motion;
    int operation;
    float ax, ay, az, gx, gy, gz, delta;
    constexpr float gravity = 9.80665f;
    while (std::scanf("%d,%f,%f,%f,%f,%f,%f,%f", &operation, &ax, &ay, &az, &gx, &gy, &gz, &delta) == 8) {
        if (operation == 1) { motion.ResetContinuousCalibration(); motion.StartContinuousCalibration(); }
        else if (operation == 2) motion.PauseContinuousCalibration();
        else if (operation == 3) motion.ResetContinuousCalibration();
        else if (operation == 4) motion.SetCalibrationOffset(gx * 180.f / M_PI, gy * 180.f / M_PI, gz * 180.f / M_PI, 1);
        else if (operation == 5) { motion.Reset(); motion.PauseContinuousCalibration(); }
        else motion.ProcessMotion(gx * 180.f / M_PI, gy * 180.f / M_PI, gz * 180.f / M_PI, -ax / gravity, -ay / gravity, -az / gravity, delta);
        float x, y, z;
        motion.GetCalibratedGyro(x, y, z);
        std::printf("%.9g,%.9g,%.9g,", x * (float)M_PI / 180.f, y * (float)M_PI / 180.f, z * (float)M_PI / 180.f);
        motion.GetGravity(x, y, z);
        auto length = std::sqrt(x*x + y*y + z*z);
        auto scale = length == 0 ? 0 : gravity / length;
        std::printf("%.9g,%.9g,%.9g,", x * scale, y * scale, z * scale);
        motion.GetCalibrationOffset(x, y, z);
        std::printf("%.9g,%.9g,%.9g\n", x * (float)M_PI / 180.f, y * (float)M_PI / 180.f, z * (float)M_PI / 180.f);
    }
}
