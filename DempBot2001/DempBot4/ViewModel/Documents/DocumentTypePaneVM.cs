using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Dempbot4.Command;

namespace Dempbot4.ViewModel.Base
{
     class DocumentTypePaneVM : PaneViewModel
    {
        static ImageSourceConverter ISC = new ImageSourceConverter();
        public DocumentTypePaneVM ViewModel
        {
            get { return this; }
        }
        #region FilePath
        protected string _filePath = null;
        public virtual string FilePath
        {
            get { return _filePath; }
            set
            {
               
            }
        }
        #endregion

        #region TextContent

        protected string _textContent = "";
        public string TextContent
        {
            get
            {
                return _textContent;
            }
            set
            {
                if (_textContent != value)
                {
                    _textContent = value;
                    RaisePropertyChanged("TextContent");
                    IsDirty = true;
                }
            }
        }

        #endregion


        #region IsDirty

        private bool _isDirty = false;
        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    RaisePropertyChanged("IsDirty");
                    RaisePropertyChanged("FileName");
                }
            }
        }

        #endregion

        #region CloseCommand
        RelayCommand _closeCommand = null;
        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand == null)
                {
                    _closeCommand = new RelayCommand((p) => OnClose(), (p) => CanClose());
                }

                return _closeCommand;
            }
        }
        protected bool _CanClose = true;
        private bool CanClose()
        {
            return _CanClose;
        }

        public bool Closable { get { return _CanClose; } }

        protected virtual void OnClose()
        {
            Workspace.This.Close(this);
        }
        #endregion
        public override Uri IconSource
        {
            get
            {
                return new Uri("pack://application:,,,/Dempbot4;component/Images/property-blue.png", UriKind.RelativeOrAbsolute);
            }
        }
        public string FileName
        {
            get
            {
                if (FilePath == null)
                    return "Noname" + (IsDirty ? "*" : "");

                return System.IO.Path.GetFileName(FilePath) + (IsDirty ? "*" : "");
            }
        }
    }
}
