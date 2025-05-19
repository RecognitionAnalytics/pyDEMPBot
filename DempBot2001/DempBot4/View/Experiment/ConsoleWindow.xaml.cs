using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines;
using Dempbot4.ViewModel;
using Dempbot4.ViewModel.Tools;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Dempbot4.View.Experiment
{
    /// <summary>
    /// Interaction logic for ConsoleWindow.xaml
    /// </summary>
    public partial class ConsoleWindow : UserControl
    {
         
        
        public ConsoleWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InputBlock.KeyDown += InputBlock_KeyDown;
            InputBlock.Focus();
            ((ConsoleViewModel)DataContext).RegisterFormInput(Dispatcher);
        }

        void InputBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //dc.ConsoleInput = InputBlock.Text;
                //dc.RunCommand();
                InputBlock.Focus();
                Scroller.ScrollToBottom();
            }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            InputBlock.Focus();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ((ConsoleViewModel)DataContext).OnClose();
        }
    }
}