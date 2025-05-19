using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using System;
using System.Collections.ObjectModel;

namespace Dempbot4.ViewModel.Tools
{
    internal class VariableViewModel : ToolViewModel
    {
       
        public class VariablePair
        {
            public string Name { get; set; }
            public object Value { get; set; }

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        ObservableCollection<VariablePair> _Vars = new ObservableCollection<VariablePair>();
        ReadOnlyObservableCollection<VariablePair> _readonyVars = null;
        public ReadOnlyObservableCollection<VariablePair> AvailableVariables
        {
            get
            {
                if (_readonyVars == null)
                    _readonyVars = new ReadOnlyObservableCollection<VariablePair>(_Vars);

                return _readonyVars;
            }
        }


        public VariableViewModel()
      : base("Variables")
        {
            Workspace.This.ActiveDocumentChanged += new EventHandler(OnActiveDocumentChanged);
            WeakReferenceMessenger.Default.Register<Variable_MSG>(this, (codeEditor, msg) =>
            {
                //Application.Current.Dispatcher.Invoke(() =>
                //{
                //    var view = ((VariableViewModel)codeEditor);
                //    view._Vars.Clear();
                //    foreach (var kvp in msg.Variables)
                //    {
                //        VariablePair vp = new VariablePair { Name = kvp.Item1, Value = kvp.Item2 };
                //        view._Vars.Add(vp);
                //    }
                //});
            });
        }

        void OnActiveDocumentChanged(object sender, EventArgs e)
        {
            if (Workspace.This.ActiveDocument != null )
            {
               
            }
        }
    }
}
