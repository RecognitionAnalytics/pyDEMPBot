using Dempbot4.ViewModel;
using SimpleControls.MRU.ViewModel;
using System.IO;
using System.Windows.Controls;

namespace Dempbot4.View.Experiment
{
    /// <summary>
    /// Interaction logic for ConsoleWindow.xaml
    /// </summary>
    public partial class ScriptWindow : UserControl
    {

        public ScriptWindow()
        {
            InitializeComponent();

        }

        private void Label_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MRUEntryVM dataContext;
            if (sender.GetType() == typeof(Label))
                dataContext = (MRUEntryVM)((Label)sender).DataContext;
            else
                dataContext = (MRUEntryVM)((Button)sender).DataContext;
            Workspace.This.Open(dataContext.PathFileName);
        }

        private void Snip_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MRUEntryVM dataContext;
            if (sender.GetType() == typeof(Label))
                dataContext = (MRUEntryVM)((Label)sender).DataContext;
            else
                dataContext = (MRUEntryVM)((Button)sender).DataContext;

            if (Workspace.This.ActiveDocument.GetType() == typeof(CodeEditorViewModel))
            {
                var snippet = File.ReadAllText(dataContext.PathFileName);
                ((CodeEditorViewModel)Workspace.This.ActiveDocument).AddSnippet(snippet);
            }
        }
    }
}