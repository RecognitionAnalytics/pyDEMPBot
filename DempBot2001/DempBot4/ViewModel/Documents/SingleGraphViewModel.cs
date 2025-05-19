using CommunityToolkit.Mvvm.Messaging;
using DataControllers;
using Dempbot4.Command;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Input;

namespace Dempbot4.ViewModel
{
    internal class SingleGraphViewModel : DocumentTypePaneVM
    {
        List<DataChannel> _Channels = new List<DataChannel>();

        public SingleGraphViewModel()
        {
            IsDirty = true;
            Title = "Single Graphs";
            _CanClose = true;
            ContentId = Title;
        }

        
        public bool HasFile { get { return string.IsNullOrEmpty(_filePath) == false; } }

        #region LoadCommand
        RelayCommand _LoadCommand = null;
        public ICommand LoadCommand
        {
            get
            {
                if (_LoadCommand == null)
                {
                    _LoadCommand = new RelayCommand((p) => OnOpen(p), (p) => CanOpen(p));
                }

                return _LoadCommand;
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
            dlg.Filter = "Step Files|*.tdms";
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                if (dlg.FileName.Trim().EndsWith(".tdms"))
                {
                    Open(dlg.FileName);
                }
            }
        }

      

        #endregion


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
            _filePath = filename;
            RaisePropertyChanged("HasFile");
            token = tokenSource.Token;
            var loadTask = new System.Threading.Tasks.Task(() =>
            {
                IsLoading = true;
                try
                {
                    if (File.Exists(filename) == false)
                        return;

                    GraphClear?.Invoke();
                    var filedata = (new ElectricFileAdapter()).OpenFile(filename, UserMessageCallback,token);
                    token.ThrowIfCancellationRequested();
                    _Channels.Clear();
                    foreach (var channel in filedata.Channels)
                    {
                        _Channels.Add(channel);
                    }
                    GraphLoaded?.Invoke(this, filedata);
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
}
