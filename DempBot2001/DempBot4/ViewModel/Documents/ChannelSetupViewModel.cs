using DempBot3.Models.Aquisition;
using Dempbot4.ViewModel.Base;
using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Dempbot4.ViewModel
{
    internal class ChannelSetupViewModel : DocumentTypePaneVM
    {
        public ChannelSetupViewModel()
        {
            IsDirty = true;
            Title = "Channels";
            ContentId = Title;
            DataRig = (IDataRig)App.Current.Services.GetService(typeof(IDataRig));
            ComboMonitorSource.Add("CurrentMonitor");
            ComboMonitorSource.Add("VoltageMonitor");
            ComboMonitorSource.Add("BiasVoltage");
            ComboMonitorSource.Add("ReferenceVoltage");
            saveCommand = new DelegateCommand(Save);
            restoreCommand = new DelegateCommand(Restore);
            if (DataRig != null)
            {
                LoadInfo();
            }
        }
      
      
        private void LoadInfo()
        {
            var selectedChannels = DataRig.SelectedChannels;
            NamedChannels.Clear();
            DriveChannels.Clear();

            foreach (var channel in DataRig.DeviceMonitorChannels())
            {
                var nc = new NamedChannelVM() { Name = channel, ChannelID = channel, Function = "Current Monitor", IsSelected = false };
                if (selectedChannels != null && selectedChannels.ContainsKey(channel))
                {
                    nc.IsSelected = true;
                    nc.Name = selectedChannels[channel].Name;
                    nc.Function = LoadChannel(selectedChannels[channel].ChannelFunction);
                    SampleRate = (selectedChannels[channel].SampleRate / 1000.0).ToString();
                }
                NamedChannels.Add(nc);
            }

           
            ComboDriveSource.Add("BiasVoltage");
            ComboDriveSource.Add("ReferenceVoltage");


            foreach (var channel in DataRig.DeviceOutputChannels())
            {
                var nc = new NamedChannelVM() { Name = channel, ChannelID = channel, Function = "Bias Voltage", IsSelected = false };

                if (selectedChannels != null && selectedChannels.ContainsKey(channel))
                {
                    nc.IsSelected = true;
                    nc.Name = selectedChannels[channel].Name;
                    nc.Function = LoadChannel(selectedChannels[channel].ChannelFunction);
                    OutSampleRate = (selectedChannels[channel].SampleRate / 1000.0).ToString();
                }
                DriveChannels.Add(nc);
            }
        }

        public ObservableCollection<NamedChannelVM> NamedChannels { get; set; } = new ObservableCollection<NamedChannelVM>();
        public ObservableCollection<NamedChannelVM> DriveChannels { get; set; } = new ObservableCollection<NamedChannelVM>();

        public ObservableCollection<string> ComboMonitorSource { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ComboDriveSource { get; set; } = new ObservableCollection<string>();

        private readonly DelegateCommand saveCommand;
        protected IDataRig DataRig;
        public DelegateCommand SaveCommand
        {
            get
            {
                return saveCommand;
            }
        }
        private readonly DelegateCommand restoreCommand;
      
        public DelegateCommand RestoreCommand
        {
            get
            {
                return restoreCommand;
            }
        }

        public void Save(object parameter)
        {
            var saveChannels = new Dictionary<string, NamedChannels>();
            foreach (var item in NamedChannels)
            {
                if (item.IsSelected)
                {
                    saveChannels.Add(item.ChannelID, new NamedChannels
                    {
                        ChannelFunction = SaveChannel(item.Function),
                        Device_Handle = item.ChannelID,
                        Name = item.Name,
                        SampleRate = double.Parse(SampleRate) * 1000
                    });
                }
            }
            foreach (var item in DriveChannels)
            {
                if (item.IsSelected)
                {
                    saveChannels.Add(item.ChannelID, new NamedChannels
                    {
                        ChannelFunction = SaveChannel(item.Function),
                        Device_Handle = item.ChannelID,
                        Name = item.Name,
                        SampleRate = double.Parse(OutSampleRate) * 1000
                    });
                }
            }
            DataRig.Save(saveChannels);
        }

        private void Restore(object parameter)
        {
            DataRig.LoadSpecificChannels(@"C:\DEMPBot_Settings\DempBotSettings\ChannelNames.json");
            LoadInfo();
            Save(parameter);
        }
        private string LoadChannel(ChannelFunctionEnum channelFunction)
        {
            switch (channelFunction)
            {
                case ChannelFunctionEnum.Other:
                    return "Other";
                case ChannelFunctionEnum.CurrentMonitor:
                    return "Current Monitor";
                case ChannelFunctionEnum.BiasMonitor:
                    return "Bias Monitor";
                case ChannelFunctionEnum.BiasVoltage:
                    return "Bias Voltage";
                case ChannelFunctionEnum.ReferenceVoltage:
                    return "Reference Voltage";
                case ChannelFunctionEnum.ReferenceMonitor:
                    return "Reference Monitor";
                case ChannelFunctionEnum.OtherVoltage:
                    return "Other Voltage";
                default:
                    return "Other";
            }
        }
        private ChannelFunctionEnum SaveChannel(string channelFunction)
        {
            switch (channelFunction)
            {
                case "Other":
                    return ChannelFunctionEnum.Other;
                case "Current Monitor":
                    return ChannelFunctionEnum.CurrentMonitor;
                case "Bias Monitor":
                    return ChannelFunctionEnum.BiasMonitor;
                case "Bias Voltage":
                    return ChannelFunctionEnum.BiasVoltage;
                case "Reference Voltage":
                    return ChannelFunctionEnum.ReferenceVoltage;
                case "Reference Monitor":
                    return ChannelFunctionEnum.ReferenceMonitor;
                case "Other Voltage":
                    return ChannelFunctionEnum.OtherVoltage;
                default:
                    return ChannelFunctionEnum.OtherVoltage;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private double _SampleRate = 10;
        public string SampleRate
        {
            get
            {
                return _SampleRate.ToString();
            }
            set
            {
                _SampleRate = double.Parse(value);
                NotifyPropertyChanged();
            }
        }

        private double _OutSampleRate = 10;
        public string OutSampleRate
        {
            get
            {
                return _OutSampleRate.ToString();
            }
            set
            {
                _OutSampleRate = double.Parse(value);
                NotifyPropertyChanged();
            }
        }
    }
     public class DriveChannelVM : INotifyPropertyChanged
    {
        private string name = "";
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value; NotifyPropertyChanged();
            }
        }
        public string ChannelID { get; set; }

        public bool IsSelected { get; set; }

        public string cFunction = "BiasVoltage";
        public string Function
        {
            get
            {
                return cFunction;
            }
            set
            {
                cFunction = value; NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return ChannelID;
        }


    }

    public class NamedChannelVM : INotifyPropertyChanged
    {
        private string name = "";
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value; NotifyPropertyChanged();
            }
        }
        public string ChannelID { get; set; }

        public bool IsSelected { get; set; }




        public string _Function = "Current Monitor";
        public string Function
        {
            get
            {
                return _Function;
            }
            set
            {
                _Function = value; NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return ChannelID;
        }


    }
}
