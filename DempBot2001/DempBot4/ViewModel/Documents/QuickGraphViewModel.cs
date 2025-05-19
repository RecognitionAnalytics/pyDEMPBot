using Dempbot4.Command;
using Dempbot4.ViewModel.Base;
using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

namespace Dempbot4.ViewModel
{
    internal class QuickGraphViewModel : DocumentTypePaneVM
    {

 
        public QuickGraphViewModel()
        {
            IsDirty = true;
            Title = "Quick Graphs";
            _CanClose = false;
            ContentId = Title;
        }

       

 
    }
}
