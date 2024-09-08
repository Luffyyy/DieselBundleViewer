using DieselEngineFormats.Bundle;
using Prism.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DieselBundleViewer.ViewModels
{
    public class ListBundle(Idstring ids) : ListItemViewModelBase
    {
        public string Name { get; set; } = ids.ToString();
        public Idstring Ids { get; set; } = ids;
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
