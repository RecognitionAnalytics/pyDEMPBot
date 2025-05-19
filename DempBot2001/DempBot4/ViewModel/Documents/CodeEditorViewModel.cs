using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Command;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace Dempbot4.ViewModel
{
    internal class CodeEditorViewModel : DocumentTypePaneVM
    {
       
        public CodeEditorViewModel(string filePath):this()
        {
            FilePath = filePath;
            Title = Path.GetFileNameWithoutExtension(filePath);
            ContentId = "file:" + filePath;
            if (FilePath.ToLower().Contains(".lua"))
            {
                Syntax = "Lua";
            }
        }


        public CodeEditorViewModel()
        {
            IsDirty = true;
            Title = FileName;
            ContentId = "file:";
            WeakReferenceMessenger.Default.Register<PlayDone_MSG>(this, (thisOne, msg) =>
            {
                ((CodeEditorViewModel)thisOne).IsPlaying = false;
            });
        }


        protected bool _isPlaying = false;
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    RaisePropertyChanged("IsPlaying");
                    IsDirty = true;
                }
               
            }
        }



        public override string FilePath
        {
            get { return _filePath; }
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    Title = Path.GetFileNameWithoutExtension(_filePath);
                    RaisePropertyChanged("FilePath");
                    RaisePropertyChanged("FileName");
                    RaisePropertyChanged("Title");

                    if (File.Exists(_filePath))
                    {
                        _textContent = File.ReadAllText(_filePath);
                        ContentId ="file:" + _filePath;
                    }
                }
            }
        }

        #region PlayCommand
        RelayCommand _playCommand = null;
        public ICommand PlayCommand
        {
            get
            {
                if (_playCommand == null)
                {
                    _playCommand = new RelayCommand((p) => OnPlay(p), (p) => CanPlay(p));
                }

                return _playCommand;
            }
        }

        private bool CanPlay(object parameter)
        {
            return true;
        }

        private void OnPlay(object parameter)
        {
            IsPlaying=!IsPlaying;
            if (IsPlaying)
            {
                WeakReferenceMessenger.Default.Send(new RunCode_MSG() { Language = Syntax=="Lua"?RunLanguages.Lua:RunLanguages.Python, Code = TextContent });
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new Interrupt_MSG());
            }
        }

        #endregion

        #region SaveCommand
        RelayCommand _saveCommand = null;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand((p) => OnSave(p), (p) => CanSave(p));
                }

                return _saveCommand;
            }
        }

        private bool CanSave(object parameter)
        {
            return IsDirty;
        }

        public void OnSave(object parameter)
        {
            Workspace.This.Save(this, false);
        }

        #endregion
       

        #region SaveAsCommand
        RelayCommand _saveAsCommand = null;
        public ICommand SaveAsCommand
        {
            get
            {
                if (_saveAsCommand == null)
                {
                    _saveAsCommand = new RelayCommand((p) => OnSaveAs(p), (p) => CanSaveAs(p));
                }

                return _saveAsCommand;
            }
        }

        private bool CanSaveAs(object parameter)
        {
            return IsDirty;
        }

        private void OnSaveAs(object parameter)
        {
            Workspace.This.Save(this, true);
        }

        #endregion

        public List<string> Fonts
        {
            get
            {
                var fonts = new List<string>();
                for (int i = 8; i < 20; i++)
                {
                    fonts.Add(i.ToString());
                }
                return fonts;

            }
        }

        private string _Font="13";
        public string Font
        {
            get
            {
                return _Font;
            }
            set
            {
                _Font=value;
                RaisePropertyChanged("Font");
                RaisePropertyChanged("FontSize");

            }
        }

        private string _Syntax = "Python";
        public string Syntax
        {
            get
            {
                return _Syntax;
            }
            set
            {
                _Syntax = value;
                RaisePropertyChanged("Syntax");
            }
        }

        public double  FontSize
        {
            get
            {
                return double.Parse( _Font) ;
            }
        }


        public void AddSnippet(string snippet)
        {
            TextContent +=  "\n" + snippet + "\n";
        }

    }
}
