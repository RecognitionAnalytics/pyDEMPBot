namespace Dempbot4.View
{
    using CommunityToolkit.Mvvm.Messaging;
    using Dempbot4.Command;
    using Dempbot4.Models.ScriptEngines.Messages;
    using MahApps.Metro.Controls;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

          
        }

        #region LoadLayoutCommand
        RelayCommand _loadLayoutCommand = null;
        public ICommand LoadLayoutCommand
        {
            get
            {
                if (_loadLayoutCommand == null)
                {
                    _loadLayoutCommand = new RelayCommand((p) => OnLoadLayout(p), (p) => CanLoadLayout(p));
                }

                return _loadLayoutCommand;
            }
        }

        private bool CanLoadLayout(object parameter)
        {
            return File.Exists(@"C:\DEMPBot_Settings\AvalonDock.Layout.config");
        }

        private void OnLoadLayout(object parameter)
        {
          
        }

        #endregion 

        #region SaveLayoutCommand
        RelayCommand _saveLayoutCommand = null;
        public ICommand SaveLayoutCommand
        {
            get
            {
                if (_saveLayoutCommand == null)
                {
                    _saveLayoutCommand = new RelayCommand((p) => OnSaveLayout(p), (p) => CanSaveLayout(p));
                }

                return _saveLayoutCommand;
            }
        }

        private bool CanSaveLayout(object parameter)
        {
            return true;
        }

        private void OnSaveLayout(object parameter)
        {
           
        }

        #endregion 

        private void OnDumpToConsole(object sender, RoutedEventArgs e)
        {

        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WeakReferenceMessenger.Default.Send (new End_MSG());
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            OnLoadLayout(null);
        }
    }
}
