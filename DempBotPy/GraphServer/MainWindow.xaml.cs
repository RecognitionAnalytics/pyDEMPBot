using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
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
using System.Diagnostics;
using System.Threading;
using ScottPlot.Statistics;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace GraphServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            StartServer();



        }
        public bool StayOpen = true;
        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StayOpen = false;
            killToken.Cancel();
            try
            {
                server.Close();

            }
            catch { }
            Thread.Sleep(500);

            try
            {

                serverThread.Abort();
            }
            catch { }
        }

        private async Task ReturnString(string str, BinaryWriter bw)
        {

            var buf = Encoding.ASCII.GetBytes(str);     // Get ASCII byte array     
            bw.Write((UInt32)buf.Length);                // Write string length
            //var blen = BitConverter.GetBytes();
            //await server.WriteAsync(blen, 0, 4);
            //await server.WriteAsync(buf, 0, buf.Length);
            bw.Write(buf);                              // Write string
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Byte_to_UInt
        {
            [FieldOffset(0)]
            public Byte[] Bytes;

            [FieldOffset(0)]
            public UInt32[] UInt;

            public static byte[] ToByte(UInt32 Uint)
            {
                return (new Byte_to_UInt() { UInt = new UInt32[] { Uint } }).Bytes;
            }

            public static int ToInt(byte[] bytes)
            {
                return (int)((new Byte_to_UInt() { Bytes = bytes }).UInt[0]);
            }
        }

        private async Task<string> ReadString(BinaryReader br)
        {
            byte[] buff = new byte[4];
            await server.ReadAsync(buff, 0, 4, killToken.Token);

            var len = Byte_to_UInt.ToInt(buff);
            //var len = (int)br.ReadUInt32();            // Read string length
            var command = new string(br.ReadChars(len));    // Read string
            Debug.Print(command);
            return command;
        }

        private double[] ReadDoubles(BinaryReader br)
        {
            var len = (int)br.ReadUInt32();            // Read string length
            var data = new double[len];

            for (int i = 0; i < len; i++)
                data[i] = br.ReadDouble();

            return data;
        }

        CancellationTokenSource killToken = new CancellationTokenSource();

        public async Task GetConnections()
        {
            await server.WaitForConnectionAsync(killToken.Token);
        }


        public Thread serverThread;
        NamedPipeServerStream server;

        public async Task ProcessCommands(NamedPipeServerStream server)
        {
            var br = new BinaryReader(server);
            var bw = new BinaryWriter(server);

            while (StayOpen)
            {
                try
                {

                    var command = await ReadString(br);

                    Console.WriteLine("Read: \"{0}\"", command);

                    switch (command)
                    {
                        case "AddLongGraph":
                            {
                                Dispatcher.Invoke(() => grid.RowDefinitions[0].Height = new GridLength(250));

                                var title = await ReadString(br);
                                var Xlabel = await ReadString(br);
                                var Ylabel = await ReadString(br);
                                var graphType = await ReadString(br);

                                var handle = longS.AddGraph(title, Xlabel, Ylabel, graphType);
                                await ReturnString(handle, bw);
                            }
                            break;
                        case "AddGraph":
                            {
                                var title = await ReadString(br);
                                var Xlabel = await ReadString(br);
                                var Ylabel = await ReadString(br);
                                var graphType = await ReadString(br);

                                var handle = graphs.AddGraph(title, Xlabel, Ylabel, graphType);
                                await ReturnString(handle, bw);
                            }
                            break;
                        case "StreamData":
                            {
                                var title = await ReadString(br);
                                var data = ReadDoubles(br);
                                graphs.StreamData(title, data);
                            }
                            break;
                        case "SignalData":
                            {
                                var title = await ReadString(br);
                                var data = ReadDoubles(br);
                                graphs.StreamSignal(title, data);
                            }
                            break;
                        case "ScatterData":
                            {
                                var title = await ReadString(br);
                                var x = ReadDoubles(br);
                                var y = ReadDoubles(br);
                                graphs.StreamScatter(title, x, y);
                            }
                            break;
                        case "MultiData":
                            {
                                var title = await ReadString(br);
                                var dataSet = await ReadString(br);
                                var x = ReadDoubles(br);
                                var y = ReadDoubles(br);
                                graphs.StreamLong( title, dataSet, x, y);
                            }
                            break;
                        case "LongData":
                            {
                                var title = await ReadString(br);
                                var dataSet = await ReadString(br);
                                var x = ReadDoubles(br);
                                var y = ReadDoubles(br);
                                longS.StreamLong(title, dataSet, x, y);
                            }
                            break;
                        case "ClearData":
                            {
                                var title = await ReadString(br);
                                graphs.ClearPileData(title);
                            }
                            break;
                        case "DeleteAll":
                            {
                                graphs.ClearPile();
                            }
                            break;
                        case "DeleteAllLong":
                            {
                                longS.ClearPile();
                            }
                            break;
                        case "DeleteGraph":
                            {
                                var title = await ReadString(br);
                                graphs.DeleteGraph(title);
                            }
                            break;
                    }





                }
                catch (EndOfStreamException)
                {
                    break;                    // When client disconnects
                }
            }
        }
        private void StartServer()
        {
            serverThread = new Thread(_ =>
            {
                server = null;
                while (StayOpen)
                {
                    try
                    {
                        // Open the named pipe.
                        server = new NamedPipeServerStream("NPtest");

                        Console.WriteLine("Waiting for connection...");

                        GetConnections().Wait();

                        Console.WriteLine("Connected.");

                        ProcessCommands(server).Wait();

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    finally
                    {
                        try
                        {
                            Console.WriteLine("Client disconnected.");
                            server.Close();
                            server.Dispose();
                        }
                        catch { }

                    }
                }
            });
            serverThread.Start();
        }


    }
}
