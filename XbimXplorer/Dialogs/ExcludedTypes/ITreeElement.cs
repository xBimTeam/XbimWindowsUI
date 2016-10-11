using System.Collections.Generic;

namespace XbimXplorer.Dialogs.ExcludedTypes
{
    internal interface ITreeElement
    {
        IEnumerable<ObjectViewModel> GetChildren();
    }
}