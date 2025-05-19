using Dempbot4.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dempbot4.View.Wizard
{
    /// <summary>
    /// Interaction logic for ExperimentWizardView.xaml
    /// </summary>
    public partial class ExperimentWizardView : UserControl
    {
        public ExperimentWizardView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            ((Experiment_ViewModel)DataContext).DeleteStep((StepViewModel)button.DataContext);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void CreateGrid(Grid hostGrid)
        {
            
            var step = hostGrid.DataContext as StepViewModel;

            if (step.SelectedOption.Options == null || step.SelectedOption.Options.Length == 0)
            {
                hostGrid.Children.Clear();
                return;
            }


            var nCols = step.SelectedOption.Options.Length;

            hostGrid.Children.Clear();
            hostGrid.RowDefinitions.Clear();
            hostGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < nCols; i++)
            {
                hostGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int j = 0; j < nCols; j++)
            {


                var col = new StackPanel();
                col.Width = 150;
                var optionVM = new OptionBinding(j) { Step = step };
                col.DataContext = optionVM;
                col.Orientation = Orientation.Vertical;
                col.Children.Add(new Label() { Content = optionVM.Option });
                var tb = new System.Windows.Controls.TextBox();
                tb.SetBinding(
                       TextBox.TextProperty,
                       new Binding
                       {
                           Path = new PropertyPath("Value"),
                           Mode = BindingMode.TwoWay
                       });
                col.Children.Add(tb);

                hostGrid.Children.Add(col);
                col.SetValue(Grid.ColumnProperty, j);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            var combo= ((ComboBox)sender);
            var step = combo.DataContext as StepViewModel;
            var grid = (Grid)((StackPanel)combo.Parent).Children[2];
           

            CreateGrid(grid);


        }
    }

   
}
