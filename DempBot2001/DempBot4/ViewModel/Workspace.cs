namespace Dempbot4.ViewModel
{
    using CommunityToolkit.Mvvm.Messaging;
    using DempBot3.Models.Aquisition;
    using Dempbot4.Command;
    using Dempbot4.Models.ScriptEngines;
    using Dempbot4.ViewModel.Base;
    using Dempbot4.ViewModel.Tools;
    using MeasureCommons.Messages;
    using Microsoft.Win32;
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;


    class Workspace : Base.ViewModelBase
    {
        LuaScripting LuaEngine = null;
        protected Workspace()
        {
            WeakReferenceMessenger.Default.Register<ShowCode_MSG>(this, (thisOne, msg) =>
            {
                ((Workspace)thisOne).ShowCode(msg);
            });

            WeakReferenceMessenger.Default.Register<PlayTitle_MSG>(this, (thisOne, msg) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(
                     (() =>
                     {
                         ((Workspace)thisOne).PlayTitle = (msg.Title);
                     }));
            });

            WeakReferenceMessenger.Default.Register<PlayStatus_MSG>(this, (thisOne, msg) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(
                     (() =>
                     {
                         switch (msg.Playing)
                         {
                             case PlayStatus.Playing:
                                 ((Workspace)thisOne).PlayColor = Brushes.Green;
                                 break;
                             case PlayStatus.Error:
                                 ((Workspace)thisOne).PlayColor = Brushes.Red;
                                 break;
                             case PlayStatus.Stopped:
                                 ((Workspace)thisOne).PlayColor = Brushes.Black;
                                 break;
                         }
                     }));
            });

            LuaEngine =(LuaScripting) App.Current.Services.GetService(typeof( LuaScripting ));

        }

       

        static Workspace _this = new Workspace();

        public static Workspace This
        {
            get { return _this; }
        }

        #region Files
        ObservableCollection<DocumentTypePaneVM> _files = new ObservableCollection<DocumentTypePaneVM>();
        ReadOnlyObservableCollection<DocumentTypePaneVM> _readonyFiles = null;
        public ReadOnlyObservableCollection<DocumentTypePaneVM> Files
        {
            get
            {
                if (_readonyFiles == null)
                    _readonyFiles = new ReadOnlyObservableCollection<DocumentTypePaneVM>(_files);

                return _readonyFiles;
            }
        }
        #endregion

        #region Tools
        ObservableCollection<ToolViewModel> _tools = new ObservableCollection<ToolViewModel>();
        ReadOnlyObservableCollection<ToolViewModel> _readonyTools = null;
        public ReadOnlyObservableCollection<ToolViewModel> Tools
        {
            get
            {
                if (_readonyFiles == null)
                    _readonyTools = new ReadOnlyObservableCollection<ToolViewModel>(_tools);

                return _readonyTools;
            }
        }
        #endregion

        #region OpenCommand
        RelayCommand _openCommand = null;
        public ICommand OpenCommand
        {
            get
            {
                if (_openCommand == null)
                {
                    _openCommand = new RelayCommand((p) => OnOpen(p), (p) => CanOpen(p));
                }

                return _openCommand;
            }
        }

        private bool CanOpen(object parameter)
        {
            return true;
        }

        private void OnOpen(object parameter)
        {
            var dlg = new OpenFileDialog();
            dlg.InitialDirectory = @"C:\DEMPBot_Settings\DempBotSettings";
            dlg.Filter = "Step Files|*.steps;*.py;*.lua;*.tdms";
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                var extension = Path.GetExtension(dlg.FileName.Trim()).ToLower();
                if (extension == (".py") || extension == ".lua")
                {
                    Open(dlg.FileName);

                }
                else if (extension == (".steps"))
                {
                    var exp = new Experiment_ViewModel();
                    exp.Load(dlg.FileName);
                    _files.Add(exp);
                    ActiveDocument = exp;
                }
                else if (extension == (".tdms"))
                {
                    var tdmsViewer = new SingleGraphViewModel();
                    tdmsViewer.Open(dlg.FileName);
                    _files.Add(tdmsViewer);
                    ActiveDocument = tdmsViewer;
                }
            }
        }

        public CodeEditorViewModel Open(string filepath)
        {
            var extension = Path.GetExtension(filepath).ToLower();
            if (extension == ".py" || extension == ".lua")
            {
                // Verify whether file is already open in editor, and if so, show it
                var fileViewModel = _files.FirstOrDefault(fm => fm.FilePath == filepath);

                if (fileViewModel != null && fileViewModel.GetType() == typeof(CodeEditorViewModel))
                {
                    this.ActiveDocument = (CodeEditorViewModel)fileViewModel; // File is already open so shiw it
                    return (CodeEditorViewModel)fileViewModel;
                }

                fileViewModel = _files.FirstOrDefault(fm => fm.FilePath == filepath);
                if (fileViewModel != null)
                {
                    this.ActiveDocument = (CodeEditorViewModel)fileViewModel;
                    return (CodeEditorViewModel)fileViewModel;
                }

                fileViewModel = new CodeEditorViewModel(filepath);
                this.ActiveDocument = (CodeEditorViewModel)fileViewModel;
                _files.Add(fileViewModel);

                return (CodeEditorViewModel)fileViewModel;
            }
            else if (extension == ".step")
            {
                var exp = new Experiment_ViewModel();
                exp.Load(filepath);
                _files.Add(exp);
                ActiveDocument = exp;
            }

            return null;
        }

        #endregion

        #region NewPyCommand
        RelayCommand _newPyCommand = null;
        public ICommand NewCommandPy
        {
            get
            {
                if (_newPyCommand == null)
                {
                    _newPyCommand = new RelayCommand((p) => OnNewPy(p), (p) => CanNewPy(p));
                }

                return _newPyCommand;
            }
        }

        private bool CanNewPy(object parameter)
        {
            return true;
        }

        private void OnNewPy(object parameter)
        {
            _files.Add(new CodeEditorViewModel(Path.Combine(App.DataFolder, "untitled.py"))); _files.Add(new CodeEditorViewModel());
            ActiveDocument = (CodeEditorViewModel)_files.Last();


        }


        RelayCommand _newCommandLua = null;
        public ICommand NewCommandLua
        {
            get
            {
                if (_newCommandLua == null)
                {
                    _newCommandLua = new RelayCommand((p) => OnNewLua(p), (p) => CanNewLua(p));
                }

                return _newCommandLua;
            }
        }

        private bool CanNewLua(object parameter)
        {
            return true;
        }

        private void OnNewLua(object parameter)
        {
            _files.Add(new CodeEditorViewModel( Path.Combine( App.DataFolder, "untitled.lua" )));
            ActiveDocument = (CodeEditorViewModel)_files.Last();


        }
        #endregion

        #region PlayCommand
        SolidColorBrush _PlayColor = Brushes.Black;
        public SolidColorBrush PlayColor
        {
            get
            { return _PlayColor; }
            set
            {
                _PlayColor = value;
                RaisePropertyChanged("PlayColor");
            }
        }

        private string _PlayTitle = "";
        public string PlayTitle
        {
            get
            {
                return _PlayTitle;
            }
            set
            {
                _PlayTitle = value;
                RaisePropertyChanged("PlayTitle");
            }
        }
        #endregion

        private void ShowCode(ShowCode_MSG msg)
        {
            var cev = new CodeEditorViewModel();
            cev.Title = msg.FileName;
            cev.TextContent = msg.Code;
            _files.Add(cev);
            ActiveDocument = (CodeEditorViewModel)_files.Last();
            RaisePropertyChanged("ActiveDocument");
        }

        #region CVCommand
        RelayCommand _CVCommand = null;
        public ICommand CVCommand
        {
            get
            {
                if (_CVCommand == null)
                {
                    _CVCommand = new RelayCommand((p) => OnCVCommand(p), (p) => CanCVCommand(p));
                }

                return _CVCommand;
            }
        }

        private bool CanCVCommand(object parameter)
        {
            return true;
        }

        private void OnCVCommand(object parameter)
        {

            _files.Add(new CV_ViewModel());
        }
        #endregion

        #region RTCommand
        RelayCommand _RTCommand = null;
        public ICommand RTCommand
        {
            get
            {
                if (_RTCommand == null)
                {
                    _RTCommand = new RelayCommand((p) => OnRTCommand(p), (p) => CanRTCommand(p));
                }

                return _RTCommand;
            }
        }

        private bool CanRTCommand(object parameter)
        {
            return true;
        }

        private void OnRTCommand(object parameter)
        {

            _files.Add(new RT_ViewModel());
        }
        #endregion

        #region IVCommand
        RelayCommand _IVCommand = null;
        public ICommand IVCommand
        {
            get
            {
                if (_IVCommand == null)
                {
                    _IVCommand = new RelayCommand((p) => OnIVCommand(p), (p) => CanIVCommand(p));
                }

                return _IVCommand;
            }
        }

        private bool CanIVCommand(object parameter)
        {
            return true;
        }

        private void OnIVCommand(object parameter)
        {

            _files.Add(new IV_ViewModel());
        }

        #endregion

        #region EXP_Command
        RelayCommand _ExperimentCommand = null;
        public ICommand ExperimentCommand
        {
            get
            {
                if (_ExperimentCommand == null)
                {
                    _ExperimentCommand = new RelayCommand((p) => OnExperimentCommand(p), (p) => CanExperimentCommand(p));
                }

                return _ExperimentCommand;
            }
        }

        private bool CanExperimentCommand(object parameter)
        {
            return true;
        }

        private void OnExperimentCommand(object parameter)
        {

            _files.Add(new Experiment_ViewModel());
        }

        #endregion

        #region ChannelCommand
        RelayCommand _ChannelCommand = null;
        public ICommand ChannelCommand
        {
            get
            {
                if (_ChannelCommand == null)
                {
                    _ChannelCommand = new RelayCommand((p) => OnChannelCommand(p), (p) => CanChannelCommand(p));
                }

                return _ChannelCommand;
            }
        }

        private bool CanChannelCommand(object parameter)
        {
            return true;
        }

        private void OnChannelCommand(object parameter)
        {

            _files.Add(new ChannelSetupViewModel());
        }

        #endregion

        #region ConsoleCommand
        RelayCommand _ConsoleCommand = null;
        public ICommand ConsoleCommand
        {
            get
            {
                if (_ConsoleCommand == null)
                {
                    _ConsoleCommand = new RelayCommand((p) => OnConsoleCommand(p), (p) => CanConsoleCommand(p));
                }

                return _ConsoleCommand;
            }
        }

        private bool CanConsoleCommand(object parameter)
        {
            return _tools.Where(x => x.GetType() == typeof(ConsoleViewModel)).Count() == 0;
        }

        private void OnConsoleCommand(object parameter)
        {

            _tools.Add(new ConsoleViewModel());
        }

        #endregion

        #region ScriptsCommand
        RelayCommand _ScriptsCommand = null;
        public ICommand ScriptsCommand
        {
            get
            {
                if (_ScriptsCommand == null)
                {
                    _ScriptsCommand = new RelayCommand((p) => OnScriptsCommand(p), (p) => CanScriptsCommand(p));
                }

                return _ScriptsCommand;
            }
        }

        private bool CanScriptsCommand(object parameter)
        {
            return true;
        }

        private void OnScriptsCommand(object parameter)
        {

            _tools.Add(new ScriptViewModel());
        }

        #endregion

        #region AnalysisCommand
        RelayCommand _AnalysisCommand = null;
        public ICommand AnalysisCommand
        {
            get
            {
                if (_AnalysisCommand == null)
                {
                    _AnalysisCommand = new RelayCommand((p) => OnAnalysisCommand(p), (p) => CanAnalysisCommand(p));
                }

                return _AnalysisCommand;
            }
        }

        private bool CanAnalysisCommand(object parameter)
        {
            return true;
        }

        private void OnAnalysisCommand(object parameter)
        {

            _files.Add(new AnalysisGraphViewModel());
        }

        #endregion

        #region SingleAnalysisCommand
        RelayCommand _SingleAnalysisCommand = null;
        public ICommand SingleAnalysisCommand
        {
            get
            {
                if (_SingleAnalysisCommand == null)
                {
                    _SingleAnalysisCommand = new RelayCommand((p) => OnSingleAnalysisCommand(p), (p) => CanSingleAnalysisCommand(p));
                }

                return _SingleAnalysisCommand;
            }
        }

        private bool CanSingleAnalysisCommand(object parameter)
        {
            return true;
        }

        private void OnSingleAnalysisCommand(object parameter)
        {

            _files.Add(new SingleGraphViewModel());
        }

        #endregion

        #region ZeroCommand
        RelayCommand _ZeroCommand = null;
        public ICommand ZeroCommand
        {
            get
            {
                if (_ZeroCommand == null)
                {
                    _ZeroCommand = new RelayCommand((p) => OnZeroCommand(p), (p) => CanZeroCommand(p));
                }

                return _ZeroCommand;
            }
        }

        private bool CanZeroCommand(object parameter)
        {
            return true;
        }

        private void OnZeroCommand(object parameter)
        {
            try
            {
                var electric = (ElectronicsProgram)App.Current.Services.GetService(typeof(ElectronicsProgram));
                electric.Zero(false);
            }
            catch { }
        }

        #endregion

        #region ActiveDocument

        private DocumentTypePaneVM _activeDocument = null;
        public DocumentTypePaneVM ActiveDocument
        {
            get { return _activeDocument; }
            set
            {
                if (_activeDocument != value)
                {
                    _activeDocument = value;
                    RaisePropertyChanged("ActiveDocument");
                    if (ActiveDocumentChanged != null)
                        ActiveDocumentChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler ActiveDocumentChanged;

        #endregion

        public object LoadContent(string contentID)
        {
            switch (contentID)
            {
                case "RT Wizard":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(RT_ViewModel))
                                return tool;
                        var vm = new RT_ViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "IV Wizard":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(IV_ViewModel))
                                return tool;
                        var vm = new IV_ViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "CV Wizard":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(CV_ViewModel))
                                return tool;
                        var vm = new CV_ViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "Console":
                    {
                        foreach (var tool in _tools)
                            if (tool.GetType() == typeof(ConsoleViewModel))
                                return tool;
                        var vm = new ConsoleViewModel();
                        _tools.Add(vm);
                        return vm;
                    }
                case "Variables":
                    {
                        foreach (var tool in _tools)
                            if (tool.GetType() == typeof(VariableViewModel))
                                return tool;
                        var vm = new VariableViewModel();
                        _tools.Add(vm);
                        return vm;
                    }
                case "Quick Graphs":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(QuickGraphViewModel))
                                return tool;
                        var vm = new QuickGraphViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "Channels":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(ChannelSetupViewModel))
                                return tool;
                        var vm = new ChannelSetupViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "Analysis Graphs":
                    {
                        var vm = new AnalysisGraphViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "Single Graphs":
                    {
                        var vm = new SingleGraphViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "Experiment Wizard":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(Experiment_ViewModel))
                                return tool;
                        var vm = new Experiment_ViewModel();
                        _files.Add(vm);
                        return vm;
                    }
                case "Scripts":
                    {
                        foreach (var tool in _files)
                            if (tool.GetType() == typeof(ScriptViewModel))
                                return tool;
                        var vm = new ScriptViewModel();
                        _tools.Add(vm);
                        return vm;
                    }
            }
            if (contentID.StartsWith("file:"))
            {
                var filename = contentID.Substring(5);
                if (filename.Trim().Length == 0)
                {
                    var file = new CodeEditorViewModel();
                    _files.Add(file);
                    ActiveDocument = (CodeEditorViewModel)_files.Last();
                    return file;
                }
                else
                    return Open(filename);
            }
            return null;
        }

        #region Close
        internal void Close(DocumentTypePaneVM fileToClose)
        {
            if (fileToClose.IsDirty)
            {
                if (fileToClose is CodeEditorViewModel)
                {
                    var code = fileToClose as CodeEditorViewModel;
                    var res = MessageBox.Show(string.Format("Save changes for file '{0}'?", fileToClose.FileName), "AvalonDock Test App", MessageBoxButton.YesNoCancel);
                    if (res == MessageBoxResult.Cancel)
                        return;
                    if (res == MessageBoxResult.Yes)
                    {
                        Save(code);

                    }
                }
            }

            _files.Remove(fileToClose);
        }

        internal void CloseTool(ToolViewModel tool)
        {
            _tools.Remove(tool);
        }

        internal void CloseWindow(Type windowType)
        {
            var window = _tools.Where(x => x.GetType() == windowType).FirstOrDefault();
            if (window != null)
            {
                _tools.Remove(window);
            }
            else
            {
                var window2 = _files.Where(x => x.GetType() == windowType).FirstOrDefault();
                if (_files != null)
                {
                    _files.Remove(window2);
                }
            }
        }
        #endregion

        internal void Save(CodeEditorViewModel fileToSave, bool saveAsFlag = false)
        {
            if (fileToSave.FilePath == null || saveAsFlag)
            {
                var dlg = new SaveFileDialog();
                dlg.InitialDirectory = @"C:\DEMPBot_Settings\DempBotSettings";
                dlg.Filter = "Python Files|*.py";
                if (dlg.ShowDialog().GetValueOrDefault())
                    fileToSave.FilePath = dlg.FileName;
            }
            if (fileToSave.FilePath != null)
            {
                File.WriteAllText(fileToSave.FilePath, fileToSave.TextContent.Replace("\t","    "));
                ActiveDocument.IsDirty = false;
            }
        }

        /// <summary>
        /// Bind a window to some commands to be executed by the viewmodel.
        /// </summary>
        /// <param name="win"></param>
        public void InitCommandBinding(Window win)
        {
            win.CommandBindings.Add(new CommandBinding(AppCommand.LoadFile,
            (s, e) =>
            {
                if (e == null)
                    return;

                string filename = e.Parameter as string;

                if (filename == null)
                    return;

                this.Open(filename);
            }));


        }
    }
}
