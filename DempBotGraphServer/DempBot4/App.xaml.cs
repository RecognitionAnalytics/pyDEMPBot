namespace Dempbot4
{
    using CefSharp;
    using CefSharp.Wpf;
    using CommunityToolkit.Mvvm.Messaging;
    using Dempbot4.Models.ScriptEngines.Messages;
    using Flurl.Http;
    using Flurl.Http.Configuration;
    using MeasureCommons.Data.Experiments;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Windows;


    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string DataFolder;


        public static string ExperimentDataFolder = @"C:\DEMPBot_Data";


        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            // The folder for the roaming current user 
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            folder = @"C:\DempBot_Settings";
            // Combine the base folder with your specific folder....
            DataFolder = Path.Combine(folder, "DempBotSettings");

            Directory.CreateDirectory(DataFolder);

           

            var services = new ServiceCollection();
          
            services.AddSingleton<IExperiment, Experiment>();
          
            

            FlurlHttp.ConfigureClient("https://10.212.27.176:7003", cli =>
    cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());

            return services.BuildServiceProvider();
        }

        public App()
        {
            Services = ConfigureServices();
        }

        public class UntrustedCertClientFactory : DefaultHttpClientFactory { public override HttpMessageHandler CreateMessageHandler() => new HttpClientHandler { ServerCertificateCustomValidationCallback = (a, b, c, d) => true }; }


        protected override void OnStartup(StartupEventArgs e)
        {
            System.AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
          



            Debug.Print(Console.IsOutputRedirected.ToString());
            //Console.SetOut(new ControlWriter());
            var tw = new ControlWriter();
            Console.SetOut(TextWriter.Synchronized(tw));

            Console.WriteLine(Console.IsOutputRedirected.ToString());

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
            settings.CefCommandLineArgs.Add("ignore-certificate-errors");
            if (!Cef.IsInitialized)
            {
                //Perform dependency check to make sure all relevant resources are in our output directory.
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            }

            base.OnStartup(e);
        }
    }

    public class ControlWriter : TextWriter
    {
        public ControlWriter()
        {

        }

        public override void Write(char value)
        {
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = value.ToString() });
        }

        public override void Write(string value)
        {
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = value.ToString() });
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}
