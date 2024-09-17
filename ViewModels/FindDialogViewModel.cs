namespace DieselBundleViewer.ViewModels
{
    public class FindDialogViewModel : DialogBase
    {
        public override string Title => "Find";

        public static string Search { get; set; }
        public static bool MatchWord { get; set; }
        public static bool UseRegex { get; set; }
        public static bool FullPath { get; set; }

        protected override void PreCloseDialog(string success)
        {
            Params.Add("Search", Search);
            Params.Add("MatchWord", MatchWord);
            Params.Add("UseRegex", UseRegex);
            Params.Add("FullPath", UseRegex);
        }
    }
}
