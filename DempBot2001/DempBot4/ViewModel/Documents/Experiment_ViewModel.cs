using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Command;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel.Base;
using MeasureCommons.Messages;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Dempbot4.ViewModel
{
    internal class Experiment_ViewModel : DocumentTypePaneVM
    {
        public Experiment_ViewModel()
        {
            IsDirty = true;
            Title = "Experiment Wizard";
            ContentId = Title;


            Options = new OptionsViewModel[]
            {
                 new OptionsViewModel{ Program ="Air"},
                 new OptionsViewModel{ Program ="TE Monitor"},
                 new OptionsViewModel{ Program ="RCA Monitor"},
                 new OptionsViewModel{ Program ="EtOH Rinse"},
                 new OptionsViewModel{ Program ="DI Rinse"},
                 new OptionsViewModel{ Program ="RCA Rinse"},
                 new OptionsViewModel{ Program ="SA Rinse"},
                 new OptionsViewModel{ Program ="Phi29"},
                 new OptionsViewModel{ Program ="DNA"},
                 new OptionsViewModel{ Program ="CTPR Monitor"},
                 new OptionsViewModel{ Program ="DNA Monitor"},
                 new OptionsViewModel{ Program ="Phi Template"},
                 new OptionsViewModel{ Program ="RT", Options = new string[] { "Analyte","Time (s)","Voltage (mV)" } },
                 new OptionsViewModel{ Program ="IV", Options = new string[] { "Analyte","Slew (mV/s)","Voltage (mV)" } },
                 new OptionsViewModel{ Program ="VoltageStack", Options = new string[] { "Analyte","Time (s)"  } },
                 new OptionsViewModel{ Program ="Monitor", Options = new string[] { "Analyte","Hours","Break Time (min)","Measure Time (s)", "Voltage (mV)"  } },
            };

            _Steps.Add(new StepViewModel { SelectedOption = Options[0], Program = "Air" });

        }

        OptionsViewModel[] Options = null;

        public OptionsViewModel[] Commands
        {
            get
            {
                return Options;
            }
        }

        protected bool _Playable = false;
        public bool Playable
        {
            get
            {
                return CanRun(null);
            }

        }

        protected bool _IsPlaying = false;
        public bool IsPlaying
        {
            get
            {
                return _IsPlaying;
            }
            set
            {
                if (_IsPlaying != value)
                {
                    _IsPlaying = value;
                    RaisePropertyChanged("IsPlaying");
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
                    CalcPrompt();
                    RaisePropertyChanged("Wafer");
                    RaisePropertyChanged("Playable");
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
                    CalcPrompt();
                    RaisePropertyChanged("Chip");
                    RaisePropertyChanged("Playable");
                    IsDirty = true;
                }
            }
        }

        protected string _Notes = "";
        public string Notes
        {
            get
            {
                return _Notes;
            }
            set
            {
                if (_Notes != value)
                {
                    _Notes = value;
                    RaisePropertyChanged("Notes");
                    IsDirty = true;
                }
            }
        }

        protected string _Tags = "";
        public string Tags
        {
            get
            {
                return _Tags;
            }
            set
            {
                if (_Tags != value)
                {
                    _Tags = value;
                    CalcPrompt();
                    RaisePropertyChanged("Tags");
                    IsDirty = true;
                }
            }
        }

        protected string _Resistor = "Monitor:.1";
        public string Resistor
        {
            get
            {
                return _Resistor;
            }
            set
            {
                if (_Resistor != value)
                {
                    _Resistor = value;
                    RaisePropertyChanged("Resistor");
                    IsDirty = true;
                }
            }
        }


        private void CalcPrompt()
        {
            string prompts = "";
            var needs = new List<string>();
            if (string.IsNullOrEmpty(_Wafer)) needs.Add("Wafer");
            if (string.IsNullOrEmpty(_Chip)) needs.Add("Chip");
            if (string.IsNullOrEmpty(_Tags)) needs.Add("Tags");
            if (needs.Count > 0)
            {
                prompts = $"Please enter {string.Join(",", needs)} to get started.";
            }
            if (Steps.Count == 0)
            {
                prompts += "Please enter the measurement steps you want.";
            }
            if (prompts.Length == 0)
            {
                prompts = "Please make sure to unshunt the chip and then run";
            }
            Prompts = prompts;
        }

        protected string _Prompts = @"Please enter Wafer, Chip, and Tags to get started.
Please enter the measurement steps you want.";
        public string Prompts
        {
            get
            {
                return _Prompts;
            }
            set
            {
                if (_Prompts != value)
                {
                    _Prompts = value;
                    RaisePropertyChanged("Prompts");
                    IsDirty = true;
                }
            }
        }

        public void DeleteStep(StepViewModel step)
        {
            _Steps.Remove(step);
            RaisePropertyChanged("Steps");
            IsDirty = true;
        }

        ObservableCollection<StepViewModel> _Steps = new ObservableCollection<StepViewModel>();
        ReadOnlyObservableCollection<StepViewModel> _readonySteps = null;
        public ReadOnlyObservableCollection<StepViewModel> Steps
        {
            get
            {
                if (_readonySteps == null)
                    _readonySteps = new ReadOnlyObservableCollection<StepViewModel>(_Steps);

                return _readonySteps;
            }
        }

        #region AddStepCommand
        RelayCommand _AddStepCommand = null;
        public ICommand AddStepCommand
        {
            get
            {
                if (_AddStepCommand == null)
                {
                    _AddStepCommand = new RelayCommand((p) => OnAddStepCommand(p), (p) => CanAddStepCommand(p));
                }

                return _AddStepCommand;
            }
        }

        private bool CanAddStepCommand(object parameter)
        {
            return true;
        }

        private void OnAddStepCommand(object parameter)
        {
            _Steps.Add(new StepViewModel());
            CalcPrompt();
        }
        #endregion


        #region RunCommand
        RelayCommand _RunCommand = null;
        public ICommand RunCommand
        {
            get
            {
                if (_RunCommand == null)
                {
                    _RunCommand = new RelayCommand((p) => OnRun(p), (p) => CanRun(p));
                }

                return _RunCommand;
            }
        }

        private bool CanRun(object parameter)
        {
            return _Wafer != "" && _Wafer != null && _Chip != "" && _Chip != null;
        }

        private void OnRun(object parameter)
        {
            Prompts = "Starting Code\nCheck Console for updates";
            IsPlaying = true;
            WeakReferenceMessenger.Default.Send(new RunCode_MSG { Code = CreateCode() });
        }
        #endregion
        private string CreateCode()
        {
            var code = @"
import Machine
import Macros

Experiment = Macros.ExperimentTemplate(Experiment)
Experiment.Wafer = '" + _Wafer + @"'
Experiment.Chip = '" + _Chip + @"'
Experiment.ResistorChannel ='" + _Resistor + @"'
Experiment.Tags = """"""" + _Tags + @"""""""
Experiment.Notes = """"""" + _Notes + @"""""""
Experiment.Save()

measure = Macros.Measurement( ElectricAdapt,  Experiment, Intenet, Display)
tests = Macros.TestStepClass(measure)
measure.Zero()

 
def monitor(analyte, voltage_mv, mTime ):
    global count
    print(str(voltage_mv) + ':' + str(mTime))
    measure.runRT(analyte + str(count) + 'hr', voltage_mv, mTime,  slowTurn=True, mode='Default')
    measure.runIV(analyte + str(count) + 'hr', slewRate=50, voltage_mV=100, cycles=2)
    measure.Zero()
    count+=1

printnow('Starting')
";
            var reg = new Regex("((-)|(\\d)|(\\.)|( ))+");
            int cc = 0;
            foreach (var step in _Steps)
            {
                string parameters = "";
                if (step.SelectedOption.Options != null)
                {
                    var itemList = new List<string>();
                    for (var i = 0; i < step.SelectedOption.Options.Length; i++)
                    {
                        string value;
                        if (step.Values[i] == null)
                        {
                            value = "None";
                        }
                        else
                        {
                            value = step.Values[i].Trim();
                            var match = reg.Match(value).Value;
                            if (match != value)
                            {
                                value = "\"" + value + "\"";
                            }
                        }
                        itemList.Add(step.SelectedOption.Options[i].Replace(" ", "").Replace("(", "_").Replace(")", "").ToLower() + "=" + value);
                    }
                    parameters = string.Join(",", itemList);
                }
                code += $@"
count=0
printnow('Step {cc}')
tests.{step.SelectedOption.Program.Replace(" ", "_")}({parameters})
";
                cc += 1;
            }

            code += "printnow('Done')\n";
            return code;
        }

        #region CopyCommand
        RelayCommand _CopyCommand = null;
        public ICommand CopyCommand
        {
            get
            {
                if (_CopyCommand == null)
                {
                    _CopyCommand = new RelayCommand((p) => OnCopyCommand(p), (p) => CanCopyCommand(p));
                }

                return _CopyCommand;
            }
        }

        private bool CanCopyCommand(object parameter)
        {
            return true;
        }

        private void OnCopyCommand(object parameter)
        {
            WeakReferenceMessenger.Default.Send(new ShowCode_MSG { Code = CreateCode(), FileName = $"Code_{_Wafer}_{_Chip}" });
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
            return true;
        }

        private string Filename = "";

        private void OnSave(object parameter)
        {

            var text = JsonConvert.SerializeObject(_Steps.ToArray());

            if (string.IsNullOrEmpty(Filename))
            {
                var dlg = new SaveFileDialog();
                dlg.InitialDirectory = @"C:\DEMPBot_Settings\DempBotSettings";
                dlg.Filter = "Step Files|*.steps";
                if (dlg.ShowDialog().GetValueOrDefault())
                    Filename = dlg.FileName;
            }
            if (!string.IsNullOrEmpty(Filename))
            {
                File.WriteAllText(Filename, text);
            }
            Title = System.IO.Path.GetFileNameWithoutExtension(Filename);
        }

        #endregion
        public void Load(string filename)
        {
            var settingsJson = File.ReadAllText(filename);

            var settings = JsonConvert.DeserializeObject<StepViewModel[]>(settingsJson);
            _Steps.Clear();
            foreach (var step in settings)
            {
                var option = Options.Where(x => x.Program == step.SelectedOption.Program).First();
                step.SelectedOption = option;
                _Steps.Add(step);
            }

            Title = System.IO.Path.GetFileNameWithoutExtension(filename);
            RaisePropertyChanged("Steps");
        }

       
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
            return true;
        }



        private void OnSaveAs(object parameter)
        {

            var text = JsonConvert.SerializeObject(_Steps.ToArray());


            var dlg = new SaveFileDialog();
            dlg.InitialDirectory = @"C:\DEMPBot_Settings\DempBotSettings";
            dlg.Filter = "Step Files|*.steps";
            if (dlg.ShowDialog().GetValueOrDefault())
                Filename = dlg.FileName;

            if (!string.IsNullOrEmpty(Filename))
            {
                File.WriteAllText(Filename, text);
            }
            Title = System.IO.Path.GetFileNameWithoutExtension(Filename);
        }
        #endregion
    }


    class OptionsViewModel
    {
        public string Program { get; set; }
        public string[] Options { get; set; } = null;


        public override string ToString()
        {
            return Program;
        }
    }

    class OptionBinding : Base.ViewModelBase
    {
        private int Index;
        public OptionBinding(int index)
        {
            Index = index;
        }
        public StepViewModel Step { get; set; }
        public string Option { get { return Step.SelectedOption.Options[Index]; } }

        public string Value
        {
            set
            {
                Step.Values[Index] = value;
                RaisePropertyChanged("Value");
            }
            get
            {
                return Step.Values[Index];
            }
        }

    }

    class StepViewModel : Base.ViewModelBase
    {

        protected OptionsViewModel _SelectedOption = null;
        public OptionsViewModel SelectedOption
        {
            get
            {
                return _SelectedOption;
            }
            set
            {
                if (_SelectedOption != value)
                {
                    _SelectedOption = value;
                    if (value.Options != null && Values != null)
                    {
                        var nValues = new string[value.Options.Length];
                        for (int i = 0; i < value.Options.Length && i < Values.Length; i++)
                        {
                            nValues[i] = Values[i];
                        }
                        Values = nValues;
                    }
                    else if (value.Options != null && value.Options.Length > 0)
                    {
                        Values = value.Options;
                    }
                    else
                    {
                        Values = new string[0];
                    }
                    RaisePropertyChanged("SelectedOption");
                }
            }
        }

        protected string[] _Values = new string[] { "" };
        public string[] Values
        {
            get
            {
                return _Values;
            }
            set
            {
                if (_Values != value)
                {
                    _Values = value;
                    RaisePropertyChanged("Values");

                }
            }
        }

        protected string _Program = "";
        public string Program
        {
            get
            {
                return _Program;
            }
            set
            {
                if (_Program != value)
                {
                    _Program = value;
                    RaisePropertyChanged("Program");

                }
            }
        }

    }
}
