using System.Collections.Generic;

namespace Loggez.Core.Interfaces;
using System.Threading.Tasks;

public interface IDialogService
{
    Task<string[]?> ShowOpenFileDialogAsync(string title, IEnumerable<string> filters, bool allowMultiple); 
}