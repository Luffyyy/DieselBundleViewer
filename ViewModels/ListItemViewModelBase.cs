using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace DieselBundleViewer.ViewModels
{
    /// <summary>
    /// A class to have a base for list item based items so we can deal with them without being too specific
    /// This is generally needed because the extended selection mode in lists doesn't really work as one would expect.
    /// Instead of selecting everything via Ctrl+A, it selects only what is visible. This is fine in terms of virtualization,
    /// but awful in pratice and so we obviously need to fix.
    /// </summary>
    public class ListItemViewModelBase : BindableBase
    {
        protected bool isSelected;
        public virtual bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }
    }
}
