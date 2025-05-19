using ScottPlot;
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
    /// Interaction logic for IVWizardView.xaml
    /// </summary>
    public partial class IVWizardView : UserControl
    {
        public IVWizardView()
        {
            InitializeComponent(); wpfPlot1.Plot.Style(ScottPlot.Style.Seaborn);
            wpfPlot1.Plot.Style(figureBackground: System.Drawing.Color.Transparent);
        }
    }
}
