namespace Electron2D;

#region WindowConfig

/// <summary>
/// Конфигурация окна приложения.
/// </summary>
public sealed class WindowConfig
{
    #region Properties

    /// <summary>Заголовок окна.</summary>
    public string Title { get; set; } = "Electron2D";

    /// <summary>Ширина клиентской области окна в пикселях.</summary>
    public int Width { get; set; } = 800;

    /// <summary>Высота клиентской области окна в пикселях.</summary>
    public int Height { get; set; } = 600;

    /// <summary>Режим окна (оконный/полноэкранный и т. п.).</summary>
    public WindowMode Mode { get; set; } = WindowMode.Windowed;

    /// <summary>Состояние окна (нормальное/свернутое/развернутое и т. п.).</summary>
    public WindowState State { get; set; } = WindowState.Normal;
    
    public bool Resizable { get; set; } = false;

    #endregion
}

#endregion