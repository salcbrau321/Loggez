using System.Diagnostics;
using Loggez.Core.Interfaces;
using Loggez.UI;

namespace Loggez.Infrastructure.External;

public class ExternalOpener : IExternalOpener
{
    public void Open(string path, int lineNumber)
    {
        var cmd  = Settings.ExternalOpener; 
        var args = $"+{lineNumber} \"{path}\"";
        Process.Start(new ProcessStartInfo(cmd, args)
        {
            UseShellExecute = true
        });
    }
}
