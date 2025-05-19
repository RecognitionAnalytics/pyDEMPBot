using DataControllers;
using Dempbot4.ViewModel;
using ScottPlot;
using ScottPlot.Plottable;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Dempbot4.View.Graphs
{
    /// <summary>
    /// Interaction logic for QuickGraphViewModel.xaml
    /// </summary>
    public partial class SingleGraphView : UserControl
    {
        public SingleGraphView()
        {
            InitializeComponent();

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ((SingleGraphViewModel)this.DataContext).GraphLoaded += AnalysisGraphView_GraphLoaded;
            ((SingleGraphViewModel)this.DataContext).GraphClear += AnalysisGraphView_GraphClear;
        }

        private void AnalysisGraphView_GraphClear()
        {
            LinePile.Clear();
            PlotPile.Clear();
        }

        private void AnalysisGraphView_GraphLoaded(object sender, DataFile e)
        {
            Dispatcher.Invoke(() =>
            {
                BuildGrid(e);
            });
        }

        Dictionary<string, WpfPlot> PlotPile = new Dictionary<string, WpfPlot>();
        Dictionary<string, ScatterPlotList<double>> LinePile = new Dictionary<string, ScatterPlotList<double>>();

        private void AddData(DataFile data)
        {
            double[] x = data.Independants[0].Data.ToArray();
            foreach (var channel in data.Channels)
            {

                if (LinePile.ContainsKey(channel.Name) == false)
                    LinePile.Add(channel.Name, PlotPile[channel.Name].Plot.AddScatterList(label: channel.Name));

                LinePile[channel.Name].AddRange(x, channel.Data);

                PlotPile[channel.Name].Plot.XAxis.Label(data.Independants[0].Name);
                PlotPile[channel.Name].Plot.YAxis.Label("Current (nA)");

                PlotPile[channel.Name].Plot.AxisAuto();

                PlotPile[channel.Name].Refresh();
            }
        }


        private void BuildGrid(DataFile data)
        {
            hostGrid.Height = data.Channels.Count * 350;

            var nRows = data.Channels.Count;

            if (nRows == PlotPile.Count)
            {
                return;
            }

            PlotPile = new Dictionary<string, WpfPlot>();

            hostGrid.Children.Clear();
            hostGrid.RowDefinitions.Clear();
            hostGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < nRows; i++)
            {
                hostGrid.RowDefinitions.Add(new RowDefinition());
            }


            for (int i = 0; i < nRows; i++)
            {
                var graph = new WpfPlot();

                graph.Plot.Style(ScottPlot.Style.Seaborn);
                graph.Plot.Style(figureBackground: System.Drawing.Color.Transparent);
                graph.Plot.Legend().FontSize = 20;
                graph.Plot.YAxis.LabelStyle(color: System.Drawing.Color.White, fontSize: 20);
                graph.Plot.XAxis.LabelStyle(color: System.Drawing.Color.White, fontSize: 20);
                graph.Plot.YAxis.TickLabelStyle(color: System.Drawing.Color.White, fontSize: 18);
                graph.Plot.XAxis.TickLabelStyle(color: System.Drawing.Color.White, fontSize: 18);

                hostGrid.Children.Add(graph);

                graph.SetValue(Grid.RowProperty, i);
                PlotPile.Add(data.Channels[i].Name, graph);

            }
            AddData(data);
        }


    }
}
