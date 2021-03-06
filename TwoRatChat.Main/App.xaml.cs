using CefSharp;
using CefSharp.Wpf;
//using NewTwitchAuth;
//using NewTwitchAuth.WebServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

using TwoRatChat.Main;
using TwoRatChat.Main.NewTwitchAuth;

namespace TwoRatChat.Main
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        /*
        private void OnStartup(object sender, StartupEventArgs e)
        {
            (MainWindow = new Window()).Show();
            new NewTwitchAuthWindow().Show();
            
        }*/
        /*
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
#if !NETCOREAPP
            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Example of setting a command line argument
            //Enables WebRTC
            settings.CefCommandLineArgs.Add("enable-media-stream");

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
#endif
            //Start my MainWindow here so that I have a reference to it.
            Window myNewTwitchAuth = new NewTwitchAuthWindow();

            //Show the MainWindow
            Current.MainWindow = myNewTwitchAuth;
            Current.MainWindow.Show();

            //Start the actual work, create the object and update the main property
            //so we can update the TextBox with data

            NewTwitchAuthWindow myProgram = new NewTwitchAuthWindow();
            //myProgram.window = (NewTwitchAuthWindow)myNewTwitchAuth;

            //Start the actual work using a Task
            Task.Run(() => myProgram.InitializeComponent());
        

        }
        */
        public static string DataFolder { get; private set; }
        public static string DataLocalFolder { get; private set; }

        public static string UserFolder { get; private set; }
        public static string TempFolder { get; private set; }

        public static void Log(char level, string Format, params object[] Params)
        {
            DateTime dt = DateTime.Now;
            string text = string.Format(Format, Params);
            try
            {
                File.AppendAllText(TempFolder + "\\trace.log",
                    string.Format("[{0}] {1:dd.HH:mm:ss} {2}\r\n", level, dt, text));
            }
            catch
            {

            }
#if DEBUG
            Console.WriteLine(string.Format("[{0}] {1}", level, text));
#endif
        }

        public static Version GetRunningVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public static string MapFileName(string relativeFileName)
        {
            string file = DataLocalFolder + relativeFileName;
            if (File.Exists(file))
                return file;
            file = DataFolder + relativeFileName;
            if (File.Exists(file))
                return file;
            return null;
        }

        #region .ctor
        public App()
            : base()
        {

            Oxlamon.Common.SmartArgs args = new Oxlamon.Common.SmartArgs();
#if !ANTIKRIZIS
#if !DEBUG
            if (!args.ContainsKey( "run" )) {
                System.Diagnostics.Process.Start( "TwoRatChat.Launcher.exe" );
                if( App.Current != null )
                    App.Current.Shutdown();
                return;
            }
#endif
#endif
            if (args.ContainsKey("reset"))
            {
                System.Windows.MessageBox.Show("Будут сброшены настройки, после чего программа будет закрыта. Перезапустите ее самостоятельно.");
                TwoRatChat.Main.Properties.Settings.Default.Reset();
                TwoRatChat.Main.Properties.Settings.Default.Save();
                if (App.Current != null)
                    App.Current.Shutdown();
                return;
            }

            //Oxlamon.Common.SmartArgs args = new Oxlamon.Common.SmartArgs();
            //if ( !args.ContainsKey( "czt" ) ) {
            //    if ( App.Current != null )
            //        App.Current.Shutdown();
            //    return;
            //}
            ServicePointManager.ServerCertificateValidationCallback +=
                   (sender, cert, chain, sslPolicyErrors) =>
                   {
                       return true;
                   };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
               | SecurityProtocolType.Tls11
               | SecurityProtocolType.Tls12
               | SecurityProtocolType.Ssl3;

            UserFolder = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(Path.Combine(UserFolder, "local"));
            TempFolder = UserFolder + "\\local\\temp";
            Directory.CreateDirectory(TempFolder);
            DataLocalFolder = UserFolder + "\\local\\data";
            Directory.CreateDirectory(DataLocalFolder);

            DataFolder = UserFolder + "\\data";

            Log(' ', "TwoRatChat started. Version: {0}", GetRunningVersion());

            //CultureInfo.DefaultThreadCurrentCulture = new CultureInfo( "ru-RU" );

            // Код для апгрейда настроек
            if (!TwoRatChat.Main.Properties.Settings.Default.Upgraded)
            {
                TwoRatChat.Main.Properties.Settings.Default.Upgrade();
                TwoRatChat.Main.Properties.Settings.Default.Upgraded = true;
                TwoRatChat.Main.Properties.Settings.Default.Save();
            }

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
      

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log('!', "CurrentDomain_UnhandledException: {0}", e.ExceptionObject);
            System.Windows.MessageBox.Show(TwoRatChat.Main.Properties.Resources.MES_FatalError);

            System.Windows.Application.Current.Shutdown(-1);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log('!', "App_DispatcherUnhandledException: {0}", e.Exception);
            System.Windows.MessageBox.Show(TwoRatChat.Main.Properties.Resources.MES_FatalError);

            System.Windows.Application.Current.Shutdown(-2);
        }
        #endregion
    }
    /*
    namespace TwitchAuth
    {
        /// <summary>
        /// Interaction logic for App.xaml
        /// </summary>
        public partial class App2 : System.Windows.Application
        {

            protected override void OnStartup(StartupEventArgs e)
            {
                base.OnStartup(e);

                //Start my MainWindow here so that I have a reference to it.
                Window myMain = new TwitchauthWindow();

                //Show the MainWindow
                System.Windows.Application.Current.MainWindow = myMain;
                System.Windows.Application.Current.MainWindow.Show();

                //Start the actual work, create the object and update the main property
                //so we can update the TextBox with data

                Twitchauth.Twitchauth myProgram = new Twitchauth.Twitchauth();
                myProgram.main = (Twitchauth.TwitchauthWindow)myMain;

                //Start the actual work using a Task
                Task.Run(() => myProgram.DoWork());




            }
        }
    }
    */
    /*    namespace TwoRatChat.Main.NewTwitchAuth.NewTwitchAuthWindow
      {
            public partial class App2 : System.Windows.Application
            {
                protected override void OnStartup(StartupEventArgs e)
                {
                    base.OnStartup(e);
  #if !NETCOREAPP
                  var settings = new CefSettings()
                  {
                      //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                      CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                  };

                  //Example of setting a command line argument
                  //Enables WebRTC
                  settings.CefCommandLineArgs.Add("enable-media-stream");

                  //Perform dependency check to make sure all relevant resources are in our output directory.
                  Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
  #endif
                  //Start my MainWindow here so that I have a reference to it.
                  Window myNewTwitchAuth = new NewTwitchAuthWindow();

                    //Show the MainWindow
                    System.Windows.Application.Current.MainWindow = myNewTwitchAuth;
                    System.Windows.Application.Current.MainWindow.Show();

                    //Start the actual work, create the object and update the main property
                    //so we can update the TextBox with data

                    NewTwitchAuthWindow myProgram = new NewTwitchAuthWindow();
                    //myProgram.NewTwitchAuthWindow= (NewTwitchAuthWindow)myNewTwitchAuth;

                    //Start the actual work using a Task
                    Task.Run(() => myProgram.InitializeComponent());




                }

            }

        } */
}
