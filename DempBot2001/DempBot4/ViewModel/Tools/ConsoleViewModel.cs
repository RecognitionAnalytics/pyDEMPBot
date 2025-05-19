using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using MeasureCommons.Messages;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Dempbot4.ViewModel.Tools
{
    internal class ConsoleViewModel : ToolViewModel 
    {

        public ConsoleViewModel()
      : base("Console")
        {
            Workspace.This.ActiveDocumentChanged += new EventHandler(OnActiveDocumentChanged);
            _CanClose = false;
        }
       

        private bool Registered = false;

        public void RegisterFormInput(Dispatcher dispatcher) {
            if (!Registered)
            {

                Registered = true;
                WeakReferenceMessenger.Default.Register<PythonError_MSG>(this, (consoleContent, msg) =>
                {
                    dispatcher.Invoke(new Action(() =>
                    {
                        ((ConsoleViewModel)consoleContent).ConsoleOutput.Add(msg.Messsage);
                    }));

                });
                WeakReferenceMessenger.Default.Register<Console_MSG>(this, (consoleContent, msg) =>
                {
                    dispatcher.Invoke(new Action(() =>
                    {
                        ((ConsoleViewModel)consoleContent).ConsoleOutput.Add(msg.Command);
                    }));
                });
            }
        }

        string consoleInput = string.Empty;
        ObservableCollection<string> consoleOutput = new ObservableCollection<string>() { "Fluidics console" };

        public string ConsoleInput
        {
            get
            {
                return consoleInput;
            }
            set
            {
                consoleInput = value;
                RaisePropertyChanged("ConsoleInput");
            }
        }

        public ObservableCollection<string> ConsoleOutput
        {
            get
            {
                return consoleOutput;
            }
            set
            {
                consoleOutput = value;
                RaisePropertyChanged("ConsoleOutput");
            }
        }

        public void RunCommand()
        {
            ConsoleOutput.Add(">>" + ConsoleInput);
            WeakReferenceMessenger.Default.Send(new RunScript_MSG { Language= RunLanguages.Python, Command = ConsoleInput });
            // do your stuff here.
            ConsoleInput = String.Empty;
        }


        public void OnClose()
        {
            Workspace.This.CloseTool(this);
        }
        void OnActiveDocumentChanged(object sender, EventArgs e)
        {
            if (Workspace.This.ActiveDocument != null)
            {

            }
        }
    }
}
