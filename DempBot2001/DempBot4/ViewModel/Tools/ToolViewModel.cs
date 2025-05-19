namespace Dempbot4.ViewModel.Base
{
    using Dempbot4.Command;
    using System;
    using System.Windows.Input;

    class ToolViewModel : PaneViewModel
    {

        public ToolViewModel ViewModel
        {
            get { return this; }
        }

        public ToolViewModel(string name)
        {
            Name = name;
            Title = name;
            ContentId = Title;
        }

        public string Name
        {
            get;
            private set;
        }


        #region IsVisible

        private bool _isVisible = true;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    RaisePropertyChanged("IsVisible");
                }
            }
        }

        #endregion


        protected bool _CanClose = true;
       

        public bool Closable { get { return _CanClose; } }

        public override Uri IconSource
        {
            get
            {
                return new Uri("pack://application:,,,/Dempbot4;component/Images/property-blue.png", UriKind.RelativeOrAbsolute);
            }
        }
    }
}
