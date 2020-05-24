using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using System.Collections.Generic;

namespace DieselBundleViewer.Models
{
    public interface IEntry
    {
        string Name { get; }
        string Type { get; }
        uint Size { get; }
        string EntryPath { get; }
        FolderEntry Parent { get; set; }

        bool InBundle(Idstring name);
        bool InBundles(List<Idstring> names);
    }
}
