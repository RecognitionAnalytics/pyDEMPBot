using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

using System.Windows.Shapes;
namespace GraphServer
{
    /// <summary>
    /// Interaction logic for QuickGraphs.xaml
    /// </summary>
    public partial class QuickGraphs : UserControl
    {
        public QuickGraphs()
        {
            InitializeComponent();

        }

        public void AutoScaleAll()
        {
            foreach (var p in PlotPile.Keys)
            {
                PlotPile[p].Item1.Plot.Axes.Margins(0.5, 0.5);
            }
        }
        private Random rnd = new Random();

        public Dictionary<string, Tuple<List<double>, List<double>>> scatterDatas = new Dictionary<string, Tuple<List<double>, List<double>>>();
        public Dictionary<string, List<double>> signalDatas = new Dictionary<string, List<double>>();
        public string AddLineGraph(string title, string xLabel, string yLabel, string graphType)
        {
            var handle = title + rnd.Next();
            Dispatcher.Invoke(() =>
            {
                if (PlotPile.ContainsKey(title) == false)
                {
                    var plot = new WpfPlot();
                    plot.Plot.Title(title);
                    plot.Plot.XLabel(xLabel);
                    plot.Plot.YLabel(yLabel);
                    plot.Width = 500;
                    plot.Height = 200;

                    wrap.Children.Add(plot);

                    Scatter scatter = null;


                    PlotPile.Add(handle, new Tuple<WpfPlot, IPlottable>(plot, scatter));
                }
            });
            return handle;
        }
        public string AddGraph(string title, string xLabel, string yLabel, string graphType, float timestep = 1)
        {
            var handle = title + rnd.Next();
            Dispatcher.Invoke(() =>
            {
                if (PlotPile.ContainsKey(title) == false)
                {
                    var plot = new WpfPlot();
                    plot.Plot.Title(title);
                    plot.Plot.XLabel(xLabel);
                    plot.Plot.YLabel(yLabel);
                    plot.Width = 500;
                    plot.Height = 250;

                    wrap.Children.Add(plot);

                    if (graphType == "DataStream")
                    {
                        var streamer = plot.Plot.Add.DataStreamer(10000);
                        streamer.Period = timestep;
                        PlotPile.Add(handle, new Tuple<WpfPlot, IPlottable>(plot, streamer));
                    }
                    else if (graphType == "Scatter")
                    {
                        var listX = new List<double>();
                        var listY = new List<double>();
                        listX.Add(0);
                        listY.Add(0);


                        scatterDatas.Add(handle, new Tuple<List<double>, List<double>>(listX, listY));

                        var scatter = plot.Plot.Add.Scatter(scatterDatas[handle].Item1, scatterDatas[handle].Item2);
                        scatter.MarkerStyle = MarkerStyle.None;
                        //PlotPile[handle] = new Tuple<WpfPlot, IPlottable>(plot, scatter);
                        PlotPile.Add(handle, new Tuple<WpfPlot, IPlottable>(plot, scatter));
                    }
                    else if (graphType == "Long")
                    {
                        var listX = new List<double>();
                        var listY = new List<double>();
                        listX.Add(0);
                        listY.Add(0);

                        scatterDatas.Add(handle + "base", new Tuple<List<double>, List<double>>(listX, listY));

                        var scatter = plot.Plot.Add.Scatter(scatterDatas[handle + "base"].Item1, scatterDatas[handle + "base"].Item2);

                        PlotPile.Add(handle,new Tuple<WpfPlot, IPlottable>(plot, scatter));
                    }
                    else if (graphType == "Signal")
                    {
                        signalDatas.Add(handle, new List<double>());
                        signalDatas[handle].Add(0);

                        var signal = plot.Plot.Add.Signal(signalDatas[handle]);
                        signal.Data.Period = timestep;
                        PlotPile.Add(handle, new Tuple<WpfPlot, IPlottable>(plot, signal));
                    }
                }
            });
            return handle;
        }

        public void StreamSignal(string handle, double[] y)
        {
            Dispatcher.Invoke(() =>
            {
                var datas = signalDatas[handle];
                if (datas.Count == 1)
                    datas.Clear();
                datas.AddRange(y);
                PlotPile[handle].Item1.Plot.Axes.Margins(0.5, 0.5);
                PlotPile[handle].Item1.Refresh();
            });
        }

        public void StreamLong(string handle, string dataSet, double[] x, double[] y)
        {
            Dispatcher.Invoke(() =>
            {
                if (scatterDatas.ContainsKey(handle + "base"))
                {
                    scatterDatas.Remove(handle + "base");
                    PlotPile[handle].Item1.Plot.Clear();
                }
                if (scatterDatas.ContainsKey(handle + dataSet) == false)
                {
                    var listX = new List<double>();
                    var listY = new List<double>();
                    listX.AddRange(x);
                    listY.AddRange(y);

                    scatterDatas.Add(handle + dataSet, new Tuple<List<double>, List<double>>(listX, listY));

                    var scatter = PlotPile[handle].Item1.Plot.Add.Scatter(scatterDatas[handle + dataSet].Item1, scatterDatas[handle + dataSet].Item2);
                    scatter.Label = dataSet;
                }
                else
                {
                    var datas = scatterDatas[handle + dataSet];
                    
                    datas.Item1.AddRange(x);
                    datas.Item2.AddRange(y);
                }
                PlotPile[handle].Item1.Plot.Axes.Margins(0.5, 0.5);
                PlotPile[handle].Item1.Plot.Legend.IsVisible = true;
                PlotPile[handle].Item1.Refresh();
            });
        }

        public void StreamScatter(string handle, double[] x, double[] y)
        {
            Dispatcher.Invoke(() =>
            {
                var datas = scatterDatas[handle];
                if (datas.Item1.Count == 1)
                {
                    datas.Item1.Clear();
                    datas.Item2.Clear();
                }
                datas.Item1.AddRange(x);
                datas.Item2.AddRange(y);

                PlotPile[handle].Item1.Plot.Axes.Margins(0.5, 0.5);
                PlotPile[handle].Item1.Refresh();
            });
        }

        public void StreamData(string handle, double[] data)
        {
            Dispatcher.Invoke(() =>
            {
                ((DataStreamer)PlotPile[handle].Item2).AddRange(data);
                PlotPile[handle].Item1.Plot.Axes.Margins(0.5, 0.5);
                PlotPile[handle].Item1.Refresh();

                //PlotPile[title].Item1.InvalidateVisual();
            });
        }

        public void ClearPileData(string handle)
        {
            Dispatcher.Invoke(() =>
            {
                ((DataStreamer)PlotPile[handle].Item2).Clear();
                PlotPile[handle].Item1.Refresh();
                if (scatterDatas.ContainsKey(handle))
                {
                    scatterDatas[handle].Item2.Clear();
                    scatterDatas[handle].Item1.Clear();
                }
                if (signalDatas.ContainsKey(handle))
                {
                    signalDatas[handle].Clear();
                }
            });
        }
        public void DeleteGraph(string handle)
        {
            Dispatcher.Invoke(() =>
            {
                wrap.Children.Remove(PlotPile[handle].Item1);
                PlotPile.Remove(handle);
                if (scatterDatas.ContainsKey(handle))
                    scatterDatas.Remove(handle);
                if (signalDatas.ContainsKey(handle))
                {
                    signalDatas.Remove(handle);
                }
            });
        }
        public void ClearPile()
        {
            Dispatcher.Invoke(() =>
            {
                scatterDatas.Clear();
                signalDatas.Clear();
                foreach (var plot in PlotPile.Keys)
                {
                    wrap.Children.Remove(PlotPile[plot].Item1);
                }
                PlotPile.Clear();
                scatterDatas.Clear();
            });
        }
        Dictionary<string, Tuple<WpfPlot, IPlottable>> PlotPile = new Dictionary<string, Tuple<WpfPlot, IPlottable>>();


    }
}
