using DieselBundleViewer.ViewModels;
using DieselEngineFormats.Bundle;
using System.Collections.Generic;

namespace DieselBundleViewer.Models
{
    public interface IEntry
    {
        string Name { get; }
        string Type { get; }
        string SizeStr { get; }
        string EntryPath { get; }
    }

    public interface IChild
    {
        IParent Parent { get; set; }
        MainWindowViewModel DataContext { get; set; }
    }

    public interface IParent
    {
        bool ContainsAnyBundleEntries(Idstring package = null);
        //void AddToTree(TreeItem item, Idstring pck = null);
        void GetSubFileEntries(Dictionary<string, FileEntry> fileList);
        SortedDictionary<string, IEntry> Children { get; set; }
        void AddToTree(FolderEntry item, Idstring pck = null);
    }
}
