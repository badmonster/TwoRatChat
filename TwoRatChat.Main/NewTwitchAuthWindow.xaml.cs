using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json.Linq;
using NHttp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TwoRatChat.Main.NewTwitchAuth
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    //public TwitchauthWindow main;
    public partial class NewTwitchAuthWindow : Window //, INotifyPropertyChanged
    {

        private HttpServer WebServer;
        private readonly string RedirectUrl = "http://localhost";
        private readonly string ClientId = Properties.Settings.Default.twitchAPIKey;
        private readonly string ClientSecret = Properties.Settings.Default.twitchSecret;
        private readonly List<string> Scopes = new List<string> { "chat:read", "chat:edit", "bits:read", "channel:read:subscriptions", "user:read:follows" };
        //private readonly List<string> Scopes = new List<string> { "user:edit", "chat:read", "chat:edit", "channel:edit", "whispers:read", "whisper:edit", "bits:read", "channel:read:subscriptions", "user:read:email", "user:read:subscriptions" };
        //{"chat:read","chat:edit"};



        // TwitchLib
        private TwitchClient OwnerOfChannelConnection;
        private TwitchAPI TheTwitchAPI;

        //Cached Variables
        public string CachedOwnerOfChannelAccessToken = "needaccesstoken"; //store this as may be needed as a parameter for some API  
        private string TwitchChannelName;                                    //need this for our bot to join the channel
        public string TwitchChannelId;                                      // need this for some API calls (get subscribers)

        //bot Commands
        Dictionary<string, string> CommandsStaticResponses = new Dictionary<string, string>
        {
            //{"about", "PetrovMonster Is a streamer from Russia who loves play." },
            //{"donate", "if you want to support the stream, you can donate using the following link: https://donationalerts.com/PetrovMonster" }
        };
        public NewTwitchAuthWindow()
        {

#if !NETCOREAPP
            var settings = new CefSettings()
            {

                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data

                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Oxlamon\\CefSharp\\Cache")

            };

            //Example of setting a command line argument
            //Enables WebRTC

            settings.CefCommandLineArgs.Add("enable-media-stream");

            //Perform dependency check to make sure all relevant resources are in our output directory.

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
#endif

            InitializeComponent();
            DataContext = this;

            InitializeWebServer();
            var authUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&scope={String.Join("+", Scopes)}";
            //var authUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={0}&redirect_uri=http://localhost&scope=channel_read&state=123456", TwoRatChat.Main.Properties.Settings.Default.twitchAPIKey

            //System.Diagnostics.Process.Start(authUrl);

            //second = this;
            WebBrowser(authUrl);

        }

        public string WebBrowser(string authUrl)
        {
            Browser.Address = authUrl;
            Browser = new ChromiumWebBrowser(authUrl);
            return string.Format("<HTML><BODY>Thanks for allowing TwitchAuthWPF to authenticate<br></BODY></HTML>");
        }
        /*
        public void AvtorizButton_Click(object sender, EventArgs e)
        {
            InitializeWebServer();
            var authUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&scope={String.Join("+", Scopes)}";
            //var authUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={0}&redirect_uri=http://localhost&scope=channel_read&state=123456", TwoRatChat.Main.Properties.Settings.Default.twitchAPIKey

            //System.Diagnostics.Process.Start(authUrl);
            Browser.Address = authUrl;
            Browser = new ChromiumWebBrowser(authUrl);

        }
        */
        public void InitializeWebServer()
        {
            //Create a web server locally, so we can make the request for OATH token related stuff
            WebServer = new NHttp.HttpServer();
            WebServer.EndPoint = new IPEndPoint(IPAddress.Loopback, 80);

            //Setup the callback that we want to run when we receive a request

            WebServer.RequestReceived += async (s, e) =>
            {
                using (var writer = new StreamWriter(e.Response.OutputStream))
                {

                    if (e.Request.QueryString.AllKeys.Any("code".Contains))

                    {

                        var code = e.Request.QueryString["code"];
                        var ownerOfChannelAccessAndRefresh = await getAccessAndRefreshTokens(code);
                        CachedOwnerOfChannelAccessToken = ownerOfChannelAccessAndRefresh.Item1;
                        SetNameAndIdByOauthedUser(CachedOwnerOfChannelAccessToken).Wait();
                        InitializeOwnerOfChannelConnection(TwitchChannelName, CachedOwnerOfChannelAccessToken);
                        InitializeTwitchAPI(CachedOwnerOfChannelAccessToken);
                        //Dispatcher.BeginInvoke(new ThreadStart(delegate { Browser.Dispose(); }));

                    }

                }
            };

            WebServer.Start();

            //string.Format("<HTML><BODY>Thanks for allowing TwitchAuthWPF to authenticate<br></BODY></HTML>");
            //System.Windows.MessageBox.Show($"Web server started on: {WebServer.EndPoint}");

        }

        public void InitializeTwitchAPI(string accessToken)
        {
            TheTwitchAPI = new TwitchAPI();
            TheTwitchAPI.Settings.ClientId = ClientId;
            TheTwitchAPI.Settings.AccessToken = accessToken;

        }

        void InitializeOwnerOfChannelConnection(string username, string accessToken)
        {
            OwnerOfChannelConnection = new TwitchClient();
            OwnerOfChannelConnection.Initialize(new ConnectionCredentials(username, accessToken), TwitchChannelName);

            //Events you want to subscribe to
            //1OwnerOfChannelConnection.OnConnected += Client_OnConnected;
            //1OwnerOfChannelConnection.OnDisconnected += OwnerOfChannelConnection_OnDisconnected;
            //OwnerOfChannelConnection.OnLog += OwnerOfChannelConnection_OnLog;
            //1OwnerOfChannelConnection.OnChatCommandReceived += Bot_OnChatCommandRecieved;

            //Other examples
            //OwnerOfChannelConnection.OnJoinedChannel += Client_OnJoinedChannel;
            //OwnerOfChannelConnection.OnUserJoined += BotConnection_OnUserJoind;
            //OwnerOfChannelConnection.OnUserLeft += BotConnection_OnUserLeft;
            //OwnerOfChannelConnection.OnMessageReceived += Client_OnMessageReceived;
            //OwnerOfChannelConnection.OnWhisperReceived += Client_OnWhisperReceived;
            //OwnerOfChannelConnection.OnNewSubscriber += Client_OnNewSubscriber;
            //OwnerOfChannelConnection.OnIncorrectLogin += Client_OnIncorrectLogin;
            //OwnerOfChannelConnection.OnWhisperCommandReceived += Bot_OnWhisperCommandReceived;

            OwnerOfChannelConnection.Connect();
        }

        // TwitchClient Event Hookups
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            //Log($"User {e.BotUsername} connected (bot access)!");
        }

        //------------------------------------------------------------------


        private void OwnerOfChannelConnection_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            //Log($"OnDisconnected event");
        }

        //------------------------------------------------------------------


        private void OwnerOfChannelConnection_OnLog(object sender, OnLogArgs e)
        {
            //Log($"OnLog: {e.Data}");
        }

        private void Bot_OnChatCommandRecieved(object sender, OnChatCommandReceivedArgs e)
        {
            string commandText = e.Command.CommandText.ToLower();

            if (commandText.Equals("banana", StringComparison.OrdinalIgnoreCase))
            {
                OwnerOfChannelConnection.SendMessage(TwitchChannelName, "Bananas are okey.");
            }

            //responses are added to dictionary in lowercase
            if (CommandsStaticResponses.ContainsKey(commandText))
            {
                OwnerOfChannelConnection.SendMessage(TwitchChannelName, CommandsStaticResponses[commandText]);
            }
        }

        //A way to log to the console and form log
        /*
        private void Log(string printMessage)
        {
            //   Invoke(new MethodInvoker(delegate ()
            // {
            Dispatcher.BeginInvoke(new Action(delegate
            {

                //System.Windows.Controls.RichTextBox.Document.Blocks.Clear();
                //System.Windows.Controls.RichTextBox.Document.Blocks.Add(new Paragraph(new Run("Text"))) += "\n" + printMessage;
                // }));
            }));
            Console.WriteLine(printMessage);
        }
        */
        public async Task SetNameAndIdByOauthedUser(string accessToken)
        {
            var api = new TwitchLib.Api.TwitchAPI();
            api.Settings.ClientId = ClientId;
            api.Settings.AccessToken = accessToken;
            TwoRatChat.Main.Properties.Settings.Default.Twitchaccesstoken = api.Settings.AccessToken;

            var oauthedUser = await api.Helix.Users.GetUsersAsync();
            TwitchChannelId = oauthedUser.Users[0].Id;
            Properties.Settings.Default.TwitchChannelID = oauthedUser.Users[0].Id;
            TwitchChannelName = oauthedUser.Users[0].Login;
        }
        public async Task<Tuple<string, string>> getAccessAndRefreshTokens(string code)
        {
            HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", RedirectUrl }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);
            return new Tuple<string, string>(json["access_token"].ToString(), json["refresh_token"].ToString());
        }
        private void GetSubButton_Click(object sender, EventArgs e)
        {

            if (TheTwitchAPI == null)
                System.Windows.MessageBox.Show($"Сперва авторизуйтесь! Перейдя по кнопке Log in...\n Не смогу получить нужные данные без этого.\n (Вы же вписали все API ключи в настройки TwoRatChat?!)", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
            try
            {
                GetBroadcasterSubscriptionsResponse subscribers = TheTwitchAPI.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(TwitchChannelId, 100, null, CachedOwnerOfChannelAccessToken).Result;

                if (subscribers.Data.Length == 0)

                    System.Windows.MessageBox.Show($"Колличество подписчиков: {subscribers.Data.Length},\n походу у вас нет тех, кто подписался.\n Возможно вы перепутали с followers.", "", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (subscribers.Data.Length > 0)

                    System.Windows.MessageBox.Show($"Колличество подписчиков: {subscribers.Data.Length}.", "", MessageBoxButton.OK, MessageBoxImage.Information);

            }

            catch (Exception ee)
            {
                App.Log('!', "TwitchAuthWindow: Сперва авторизуйтесь через форму на twitch!\n Не получилось вернуть count subscribers без этого.", ee);
                //System.Windows.MessageBox.Show($"Сперва нужно было авторизоваться!\n Не смогу получить нужные данные без этого. \n Приложение будет закрыто :( .");
            }

            //Log($"You've got {subscribers.Data.Length} subscribers");

        }
        private void NewAuthTwitch_FormClosing(object sender, CancelEventArgs e)
        {

            //Could call Application.Exit() most likely instead but...
            if (OwnerOfChannelConnection != null)

            {
                OwnerOfChannelConnection.Disconnect();

            }

            if (WebServer != null)

            {
                WebServer.Stop();
                WebServer.Dispose();
            }

        }
        /*
        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        */
    }

}
