using CommunityToolkit.Mvvm.Messaging;
using DataControllers;
using Dempbot4.Command;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace Dempbot4.ViewModel
{
    internal class AnalysisGraphViewModel : DocumentTypePaneVM
    {

        List<DataChannel> _Channels = new List<DataChannel>();

        ObservableCollection<string> _Analytes = new ObservableCollection<string>();
        ReadOnlyObservableCollection<string> _readonyAnalytes = null;
        public ReadOnlyObservableCollection<string> Analytes
        {
            get
            {
                if (_readonyAnalytes == null)
                    _readonyAnalytes = new ReadOnlyObservableCollection<string>(_Analytes);

                return _readonyAnalytes;
            }
        }

        public string[] Machines { get { return new string[] { "Alice", "Bob", "Fender" }; } }

        ObservableCollection<string> _Wafers = new ObservableCollection<string>();
        ReadOnlyObservableCollection<string> _readonyWafers = null;
        public ReadOnlyObservableCollection<string> Wafers
        {
            get
            {
                if (_readonyWafers == null)
                    _readonyWafers = new ReadOnlyObservableCollection<string>(_Wafers);

                return _readonyWafers;
            }
        }

        ObservableCollection<string> _Chips = new ObservableCollection<string>();
        ReadOnlyObservableCollection<string> _readonyChips = null;
        public ReadOnlyObservableCollection<string> Chips
        {
            get
            {
                if (_readonyChips == null)
                    _readonyChips = new ReadOnlyObservableCollection<string>(_Chips);

                return _readonyChips;
            }
        }

        private void UpdateFiles()
        {
            if (string.IsNullOrEmpty(_Wafer) || string.IsNullOrEmpty(_Chip))
                return;

            try
            {
                var files = System.IO.Directory.GetFiles(Path.Combine(baseFolder, _Wafer, _Chip));
                if (files.Length == 0)
                    return;

                var possibleAnalytes = new List<string>();
                foreach (var file in files)
                {
                    var analyte = System.IO.Path.GetFileNameWithoutExtension(file);
                    var parts = analyte.Trim().Split('_');
                    var analyteName = "";
                    for (int i = 2; i < parts.Length - 1; i++)
                    {
                        analyteName += parts[i] + "_";
                    }
                    possibleAnalytes.Add(analyteName.TrimEnd('_'));
                }
                _Analytes.Clear();
                foreach (var ana in possibleAnalytes.Distinct())
                    _Analytes.Add(ana.Replace("_IV", "").Replace("_Default", "").Replace("_RT", ""));

                RaisePropertyChanged("Analytes");
            }
            catch { }
        }

        string baseFolder = "";
        private void UpdateSelections()
        {


            switch (_Machine)
            {
                case "Bob":
                    {
                        baseFolder = @"\\biod2079\C2100";

                        _Wafers.Clear();
                        foreach (var folder in Directory.GetDirectories(baseFolder))
                            _Wafers.Add(Path.GetFileName(folder));
                        break;
                    }
                case "Alice":
                    {
                        baseFolder = @"\\biod1343\DEMPBot_data";

                        _Wafers.Clear();
                        foreach (var folder in Directory.GetDirectories(baseFolder))
                            _Wafers.Add(Path.GetFileName(folder));
                        break;
                    }
                case "Fender":
                    {
                        baseFolder = @"\\BIOD1404\DempBot_Data";

                        _Wafers.Clear();
                        foreach (var folder in Directory.GetDirectories(baseFolder))
                            _Wafers.Add(Path.GetFileName(folder));
                        break;
                    }
            }
            RaisePropertyChanged("Wafers");



        }

        protected string _Machine = "";
        public string Machine
        {
            get
            {
                return _Machine;
            }
            set
            {
                if (_Machine != value)
                {
                    _Machine = value;
                    UpdateSelections();
                    UpdateFiles();
                    RaisePropertyChanged("Machine");
                    IsDirty = true;
                }
            }
        }

        protected string _Wafer = "";
        public string Wafer
        {
            get
            {
                return _Wafer;
            }
            set
            {
                if (_Wafer != value)
                {
                    _Wafer = value;
                    if (string.IsNullOrEmpty(_Wafer) == false)
                    {
                        _Chips.Clear();
                        foreach (var folder in Directory.GetDirectories(Path.Combine(baseFolder, _Wafer)))
                            _Chips.Add(Path.GetFileName(folder));
                        RaisePropertyChanged("Chips");
                    }
                    RaisePropertyChanged("Chips");
                    UpdateFiles();
                    RaisePropertyChanged("Wafer");
                    IsDirty = true;
                }
            }
        }

        protected string _Chip = "";
        public string Chip
        {
            get
            {
                return _Chip;
            }
            set
            {
                if (_Chip != value)
                {
                    _Chip = value;
                    UpdateFiles();
                    RaisePropertyChanged("Chip");

                    IsDirty = true;
                }
            }
        }

        protected string _Analyte = "";
        public string Analyte
        {
            get
            {
                return _Analyte;
            }
            set
            {
                if (_Analyte != value)
                {
                    _Analyte = value;

                    RaisePropertyChanged("Analyte");
                    if (IsLoading)
                        tokenSource.Cancel();
                    Open(Path.Combine(baseFolder, _Wafer, _Chip));

                    IsDirty = true;
                }
            }
        }
        public AnalysisGraphViewModel()
        {
            IsDirty = true;
            Title = "Analysis Graphs";
            _CanClose = true;
            ContentId = Title;


        }

        CancellationTokenSource tokenSource = new CancellationTokenSource();
        CancellationToken token;
        bool IsLoading = false;

        private void UserMessageCallback(string message)
        {
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = message });
        }
        public void Open(string filename)
        {
            if (IsLoading)
            {
                try
                {
                    tokenSource.Cancel();
                }
                catch { }
            }
            token = tokenSource.Token;
            var loadTask = new System.Threading.Tasks.Task(() =>
            {
                IsLoading = true;
                try
                {
                    var files = System.IO.Directory.GetFiles(filename);
                    if (files.Length == 0)
                        return;

                    var possibleFiles = new List<string>();
                    foreach (var file in files)
                    {
                        if (file.Contains(_Analyte))
                            possibleFiles.Add(file);
                    }

                    GraphClear?.Invoke();
                    foreach (var pFile in possibleFiles)
                    {
                        var fileData = (new ElectricFileAdapter()).OpenFile(pFile, UserMessageCallback, token);
                        token.ThrowIfCancellationRequested();
                        _Channels.Clear();
                        foreach (var channel in fileData.Channels)
                        {
                            _Channels.Add(channel);
                        }
                        GraphLoaded?.Invoke(this, fileData);
                    }
                }
                catch { }
                finally
                {
                    IsLoading = false;
                }
            }, token);
            loadTask.Start();
        }


        public event GraphLoadedEventHandler GraphLoaded;
        public event GraphClear GraphClear;
    }
    internal delegate void GraphLoadedEventHandler(object sender, DataFile e);
    internal delegate void GraphClear();
}
