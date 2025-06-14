namespace Loggez.Core.Interfaces;

public interface IExternalOpener
{
    void Open(string path, int lineNumber);
}