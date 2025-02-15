using SDL3;

namespace Electron2D.Input;

public enum SensorType
{
    /// <summary>
    /// Returned for an invalid sensor
    /// </summary>
    Invalid = SDL.SensorType.Invalid,
        
    /// <summary>
    /// Unknown sensor type
    /// </summary>
    Unknown = SDL.SensorType.Unknown,
        
    /// <summary>
    /// Accelerometer
    /// </summary>
    Accel = SDL.SensorType.Accel,
        
    /// <summary>
    /// Gyroscope
    /// </summary>
    Gyro = SDL.SensorType.Gyro,
        
    /// <summary>
    /// Accelerometer for left Joy-Con controller and Wii nunchuk
    /// </summary>
    AccelL = SDL.SensorType.AccelL,
        
    /// <summary>
    /// Gyroscope for left Joy-Con controller
    /// </summary>
    GyroL = SDL.SensorType.GyroL,
        
    /// <summary>
    /// Accelerometer for right Joy-Con controller
    /// </summary>
    AccelR = SDL.SensorType.AccelR,
        
    /// <summary>
    /// Gyroscope for right Joy-Con controller
    /// </summary>
    GyroR = SDL.SensorType.GyroR,
}