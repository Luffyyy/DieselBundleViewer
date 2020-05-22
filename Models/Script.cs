using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace DieselBundleViewer.Models
{
    public class Script
    {
        public string Name { get; set; }
        public DelegateCommand OpenScript { get; set; }

        public dynamic Object;

        public Script(string name, dynamic obj)
        {
            Name = name;
            Object = obj;
            OpenScript = new DelegateCommand(Execute);
        }

        public void Execute()
        {
            Object.execute();
        }
    }
}
