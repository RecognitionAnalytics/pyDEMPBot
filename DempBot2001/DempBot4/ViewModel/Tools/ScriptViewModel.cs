using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using Python.Runtime;
using SimpleControls.MRU.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Dempbot4.ViewModel.Tools
{
    internal class ScriptViewModel : ToolViewModel
    {

        FileSystemWatcher Watcher;
        FileSystemWatcher Watcher2;

        MRUListVM _MruList = new MRUListVM();

        public MRUListVM MruList
        {
            get
            {

                return _MruList;
            }
        }


        MRUListVM _StepList = new MRUListVM();

        public MRUListVM StepList
        {
            get
            {

                return _StepList;
            }
        }

        MRUListVM _SnipList = new MRUListVM();

        public MRUListVM SnipList
        {
            get
            {

                return _SnipList;
            }
        }

        private void GetFiles()
        {
            var files = Directory.GetFiles(App.DataFolder, "*.py");
            System.Windows.Application.Current.Dispatcher.Invoke(
          (() =>
         {
             _MruList.ListOfMRUEntries.Clear();
             foreach (var file in files)
             {
                 _MruList.AddMRUEntry(file);
             }

             files = Directory.GetFiles(App.DataFolder, "*.step");
             _StepList.ListOfMRUEntries.Clear();
             foreach (var file in files)
             {
                 _StepList.AddMRUEntry(file);
             }

             files = Directory.GetFiles(App.DataFolder + "\\snippets", "*.py");
             _SnipList.ListOfMRUEntries.Clear();
             foreach (var file in files)
             {
                 _SnipList.AddMRUEntry(file);
             }
             RaisePropertyChanged("MruList");
             RaisePropertyChanged("StepList");
             RaisePropertyChanged("SnipList");
         }));
        }

        public ScriptViewModel()
      : base("Scripts")
        {
            Watcher = new FileSystemWatcher(App.DataFolder);
            Watcher.EnableRaisingEvents = true;
            Watcher.Changed += Watcher_Changed;

            Watcher2 = new FileSystemWatcher(App.DataFolder + "\\snippets");
            Watcher2.EnableRaisingEvents = true;
            Watcher2.Changed += Watcher_Changed;

            GetFiles();

        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            GetFiles();
        }
    }
}
