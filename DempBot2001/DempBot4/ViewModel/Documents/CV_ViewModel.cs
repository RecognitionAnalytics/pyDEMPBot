using DataControllers;
using DempBot3.Models.Aquisition;
using DempBot3.Models.Aquisition.Tasks;
using Dempbot4.Command;
using Dempbot4.ViewModel.Base;
using MeasureCommons.DataChannels;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Dempbot4.ViewModel
{
    internal class CV_ViewModel : DocumentTypePaneVM
    {
       
        ElectronicsProgram Data;
        protected IDataRig DataRig;
        TriangleWave_Source iv;
        public CV_ViewModel()
        {
            IsDirty = true;
            Title = "CV Wizard";
            ContentId = Title;
        }


        protected double _Slew = 100;
        public double Slew
        {
            get
            {
                return _Slew;
            }
            set
            {
                if (_Slew != value)
                {
                    _Slew = value;
                    RaisePropertyChanged("Slew");
                    IsDirty = true;
                }
            }
        }



        protected double _BottomVoltage = 2;
        public double BottomVoltage
        {
            get
            {
                return _BottomVoltage;
            }
            set
            {
                if (_BottomVoltage != value)
                {
                    _BottomVoltage = value;
                    RaisePropertyChanged("BottomVoltage");
                    IsDirty = true;
                }
            }
        }
        protected double _Cycles = 2;
        public double Cycles
        {
            get
            {
                return _Cycles;
            }
            set
            {
                if (_Cycles != value)
                {
                    _Cycles = value;
                    RaisePropertyChanged("Cycles");
                    IsDirty = true;
                }
            }
        }


        protected double _TopVoltage = 600;
        public double TopVoltage
        {
            get
            {
                return _TopVoltage;
            }
            set
            {
                if (_TopVoltage != value)
                {
                    _TopVoltage = value;
                    RaisePropertyChanged("TopVoltage");
                    IsDirty = true;
                }
            }
        }


        #region RunCommand
        RelayCommand _RunCommand = null;
        public ICommand RunCommand
        {
            get
            {
                if (_RunCommand == null)
                {
                    _RunCommand = new RelayCommand((p) => OnRun(), (p) => CanRun());
                }

                return _RunCommand;
            }
        }

        private bool _CanRun = true;
        private bool CanRun()
        {
            return _CanRun;
        }

        private void OnRun()
        {
            _CanRun = false;

            var amp = (_TopVoltage - _BottomVoltage) / 2000;
            var offset = (_TopVoltage + _BottomVoltage) / 2000;

            var freq = 1 / (amp / (_Slew / 1000) * 4);
            var cycles = _Cycles;

            string outFile = LibSettings.DataFolder + $"\\IVGraph.tdms";

            iv = new TriangleWave_Source("test", new List<ChannelFunctionEnum>()
            { ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasVoltage, ChannelFunctionEnum.BiasMonitor}, amp, freq, offset, cycles / freq,
                      logData: true, logFile: outFile);

            iv.DataRead += Data.Rt_DataRead;
            DataRig.EnqueueTask(iv);

        }
        #endregion



      
    }
}
