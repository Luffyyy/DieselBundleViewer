using DieselEngineFormats.Bundle;
using ImTools;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DieselBundleViewer.ViewModels
{
    public class ListBundle {
        public string Name { get; set; }
        public Idstring Ids { get; set; } 
        public bool IsSelected { get; set; }

        public ListBundle(Idstring ids)
        {
            Ids = ids;
            Name = ids.ToString();
        }
    }
    public class BundleSelectorDialogViewModel : DialogBase
    {
        public override string Title => "Select Bundles";

        public static List<ListBundle> Bundles;
        public ObservableCollection<ListBundle> BundlesToRender { get; set; }

        public BundleSelectorDialogViewModel() : base()
        {
            BundlesToRender = new ObservableCollection<ListBundle>();
        }

        private string search = "";
        public string Search {
            get => search; 
            set {
                SetProperty(ref search, value);
                RenderList();
            } 
        }

        private void RenderList()
        {
            BundlesToRender.Clear();
            foreach (var bundle in Bundles)
            {
                if(Search == "" || bundle.Name.Contains(Search))
                    BundlesToRender.Add(bundle);
            }
        }

        protected override void PostDialogOpened(IDialogParameters pms)
        {
            if(Bundles == null)
            {
                Bundles = new List<ListBundle>();

                var bundles = pms.GetValue<List<Idstring>>("Bundles");

                foreach (var bundle in bundles)
                {
                    Bundles.Add(new ListBundle(bundle));
                }
            }

            RenderList();
        }

        protected override void PreCloseDialog(string success)
        {
            if (success == "True")
            {
                List<Idstring> selectedBundles = new List<Idstring>();
                foreach(var bundle in Bundles)
                {
                    if(bundle.IsSelected)
                        selectedBundles.Add(bundle.Ids);
                }
                Params.Add("SelectedBundles", selectedBundles);
            }
        }
    }
}
