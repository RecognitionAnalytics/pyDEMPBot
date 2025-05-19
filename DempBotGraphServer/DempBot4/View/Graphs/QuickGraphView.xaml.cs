using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines;
using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using MeasureCommons.Messages;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Dempbot4.View.Graphs
{
    /// <summary>
    /// Interaction logic for QuickGraphViewModel.xaml
    /// </summary>
    public partial class QuickGraphView : UserControl
    {
        public QuickGraphView()
        {
            InitializeComponent();
            try
            {
                WeakReferenceMessenger.Default.Register<DataAvailable_MSG>(this, (host, msg) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ((QuickGraphView)host).Electronics_DataAvailable(msg.Data);
                    }));
                });

                WeakReferenceMessenger.Default.Register<StartData_MSG>(this, (host, msg) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ((QuickGraphView)host).Electronics_DataStarted();
                    }));
                });

                WeakReferenceMessenger.Default.Register<Label_MSG>(this, (host, msg) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ((QuickGraphView)host).LabelGraphs(msg.XLabel,msg.YLabel);
                    }));
                });

                WeakReferenceMessenger.Default.Register<Title_MSG>(this, (host, msg) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ((QuickGraphView)host).TitleGraphs(msg.Title);
                    }));
                });
            }
            catch { }
        }

        private void LabelGraphs(string xLabel, string yLabel)
        {
            foreach (var plot in PlotPile.Keys.ToArray())
            {
                plot.Plot.XLabel(xLabel);
                plot.Plot.YLabel(yLabel);

            }
            
        }

        private void TitleGraphs(string title)
        {
            foreach (var plot in PlotPile.Keys.ToArray())
            {
                plot.Plot.Title(title);
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
          //  BuildGrid(e.NewSize);
        }


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

        Dictionary<WpfPlot, ScottPlot.Plottable.ScatterPlotList<double>[]> PlotPile = new Dictionary<WpfPlot, ScottPlot.Plottable.ScatterPlotList<double>[]>();
        Dictionary<string, ScottPlot.Plottable.ScatterPlotList<double>> TrashPlots = new Dictionary<string, ScottPlot.Plottable.ScatterPlotList<double>>();

        private void Electronics_DataAvailable(Queue<ChannelDataChunk> queue)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (queue.Count > 0)
                {
                    // AnalyteName.Content = ExperimentStep.Analyte;
                    var plotHandles = PlotPile.Keys.ToArray();
                    var chunk = queue.Dequeue();
                    while (chunk != null)
                    {
                        ChannelData[] x = chunk.X_Block.ToArray();

                        if (x.Length > 0)
                        {
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
            hostGrid.Children.Clear();
            hostGrid.RowDefinitions.Clear();
            hostGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < nRows; i++)
            {
                hostGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int i = 0; i < nCols; i++)
            {
                hostGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
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
                    PlotPile.Add(graph, null);
                    graph.SetValue(Grid.RowProperty, i);
                    graph.SetValue(Grid.ColumnProperty, j);
                }
            }
            hostGrid.Height = size.Height;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
          //  BuildGrid(this.RenderSize);

            longPlot.Plot.Clear();
            longPlot.Plot.XLabel("Time (s)");
            longPlot.Plot.YLabel("Current (nA)");
        }

       
    }
}
