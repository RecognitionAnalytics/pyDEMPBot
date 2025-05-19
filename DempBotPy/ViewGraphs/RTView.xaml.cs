using MeasureCommons.Data;
using MeasureCommons.Data.Experiments;
using MeasureCommons.DataChannels;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ViewGraphs
{
    /// <summary>
    /// Interaction logic for RTView.xaml
    /// </summary>
    public partial class RTView : UserControl
    {
        public RTView()
        {
            InitializeComponent();


            BuildGrid(this.RenderSize);

            longPlot.Plot.Clear();
            longPlot.Plot.XLabel("Time (s)");
            longPlot.Plot.YLabel("Current (nA)");


         

        }
      
        Experiment Experiment;

        private void Electronics_DataStarted()
        {
            ClearPile();
        }


        public void ClearPile()
        {
            foreach (var plot in PlotPile.Keys.ToArray())
            {
                PlotPile[plot] = null;
            }
            foreach (var plot in TrashPlots.Keys.ToArray())
            {
                TrashPlots[plot].Clear();
            }
        }

        Dictionary<ScottPlot.WpfPlot, ScottPlot.Plottable.ScatterPlotList<double>[]> PlotPile = new Dictionary<WpfPlot, ScottPlot.Plottable.ScatterPlotList<double>[]>();
        Dictionary<string, ScottPlot.Plottable.ScatterPlotList<double>> TrashPlots = new Dictionary<string, ScottPlot.Plottable.ScatterPlotList<double>>();

        private void Electronics_DataAvailable(Queue<ChannelDataChunk> queue)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (queue.Count > 0)
                {
                    AnalyteName.Content = "dd";
                    var plotHandles = PlotPile.Keys.ToArray();
                    var chunk = queue.Dequeue();
                    while (chunk != null)
                    {
                        ChannelData[] x = chunk.X_Block.ToArray();
                        var data = chunk.DataBlock;

                        var nChannels = data.Count;
                        var skip = (int)Math.Ceiling(nChannels / (double)plotHandles.Length);

                        if (DateTime.Now.Subtract(x[0].StartTime).TotalSeconds < 5)
                        {
                            for (int i = 0; i < data.Count; i++)
                            {
                                int skipPlot = (int)(i % skip);
                                var wpfPlot1 = plotHandles[(int)(i / skip)];
                                var plotList = PlotPile[wpfPlot1];
                                if (plotList == null)
                                {
                                    wpfPlot1.Plot.Clear();
                                    wpfPlot1.Plot.XLabel("Time (s)");
                                    wpfPlot1.Plot.YLabel("Current (nA)");

                                    plotList = new ScottPlot.Plottable.ScatterPlotList<double>[skip];
                                    for (int j = 0; j < skip && data.Count > (i + j); j++)
                                        plotList[j] = wpfPlot1.Plot.AddScatterList(label: data[i + j].Name.Name, markerShape: MarkerShape.none);
                                    PlotPile[wpfPlot1] = plotList;
                                }

                                if (data[i].Samples.Length > 10 && cbClipOverflow.IsChecked == true && Math.Abs(data[i].Samples[data[i].Samples.Length - 5]) > 13)
                                {
                                    plotList[skipPlot].Clear();
                                    if (TrashPlots.ContainsKey(data[i].Name.Name) == false)
                                    {
                                        TrashPlots.Add(data[i].Name.Name, plotHandles[plotHandles.Length - 1].Plot.AddScatterList(label: data[i].Name.Name));
                                    }
                                    TrashPlots[data[i].Name.Name].AddRange(x[0].Samples, data[i].Samples);
                                    if (TrashPlots[data[i].Name.Name].Count > 7000)
                                        TrashPlots[data[i].Name.Name].Clear();
                                }
                                else
                                {
                                    plotList[skipPlot].AddRange(x[0].Samples, data[i].Samples);
                                    if (plotList[skipPlot].Count > 50000)
                                        plotList[skipPlot].Clear();
                                }
                            }
                        }
                        if (queue.Count > 0)
                            chunk = queue.Dequeue();
                        else
                            break;
                    }
                    foreach (var plot in plotHandles)
                    {
                        plot.Plot.AxisAuto();
                        plot.Plot.Legend(true, Alignment.MiddleRight);
                        plot.RenderRequest(RenderType.LowQuality);
                    }
                }
            }));
        }

        Dictionary<string, ScottPlot.Plottable.ScatterPlotList<double>> longPlotList = new Dictionary<string, ScottPlot.Plottable.ScatterPlotList<double>>();

        public void AddTimeDataPoint(double value, string name)
        {
            AddLongDataPoint(DateTime.Now.Ticks, value, name);
        }
        public void AddLongDataPoint(double time, double value, string name)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                var wpfPlot1 = longPlot;

                if (longPlotList.ContainsKey(name) == false)
                {

                    longPlotList.Add(name, wpfPlot1.Plot.AddScatterList(label: name, markerShape: MarkerShape.none));
                }
                longPlotList[name].Add(time, value);
                longPlot.Plot.AxisAuto();
                longPlot.Plot.Legend(true, Alignment.MiddleRight);
                longPlot.RenderRequest(RenderType.LowQuality);

            }));
        }

        private void BuildGrid(Size size)
        {
            contentGrid.Height = size.Height * 2;
            PlotPile = new Dictionary<WpfPlot, ScottPlot.Plottable.ScatterPlotList<double>[]>();
            var nCols = (int)(size.Width / 500);
            var nRows = (int)(size.Height / 300);
            hostgrid.Children.Clear();
            hostgrid.RowDefinitions.Clear();
            hostgrid.ColumnDefinitions.Clear();

            for (int i = 0; i < nRows; i++)
            {
                hostgrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int i = 0; i < nRows; i++)
            {
                hostgrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    var graph = new ScottPlot.WpfPlot();
                    hostgrid.Children.Add(graph);
                    PlotPile.Add(graph, null);
                    graph.SetValue(Grid.RowProperty, i);
                    graph.SetValue(Grid.ColumnProperty, j);
                }
            }
            hostgrid.Height = size.Height;
        }


    }
}
