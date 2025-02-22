namespace Electron2D;

public class ElectronException : Exception
{
    public ElectronException() { }

    public ElectronException(string message) : base(message)
    {
        Logger.Error(message);
    }
    
    public ElectronException(string message, Exception innerException) : base(message, innerException) { }
}