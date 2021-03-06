using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows.Threading;
using System.Xml.Linq;
using TwoRatChat.Controls;

namespace TwoRatChat.Main.Sources
{
    public class YoutubeGaming_ChatSource : TwoRatChat.Model.ChatSource
    {
        /// <summary>
        /// Тут я его должен убрать из исходников, но в общем кто знает
        /// </summary>
        //string youtubetoken = Properties.Settings.Default.YoutubeAccesstoken;

        private readonly string youTubeAPIKey = Properties.Settings.Default.youTubeAPIKey;
        //private readonly string YOUTUBE_APIKEY = "Здесь ваш YOUTUBE API Key";

        const string API_getLiveStreamingDetails = "https://www.googleapis.com/youtube/v3/videos?part=liveStreamingDetails&id={0}&key={1}";
        const string API_getMessages = "https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={0}&part=snippet%2CauthorDetails&key={1}";

        class YoutubeMessage
        {
            public DateTime publishedAt;
            public string displayMessage;
        }

        class YoutubeAuthor
        {
            public string displayName;
            public bool isVerified;
            public bool isChatOwner;
            public bool isChatSponsor;
            public bool isChatModerator;
            public string profileImageUrl;
        }

        class YoutubeMessageContainer
        {
            public string kind;
            public string etag;
            public string id;
            public YoutubeMessage snippet;
            public YoutubeAuthor authorDetails;

            [JsonIgnore]
            public int NeedToDelete;
        }

        class YoutubeLiveMessages
        {
            public string nextPageToken;
            public int pollingIntervalMillis;
            public YoutubeMessageContainer[] items;
        }

        string _liveChatId;
        // Timer _retriveTimer;
        //List<YoutubeMessageContainer> _cache;
        string _id;
        DateTime _last;
        bool _showComments = false;

        public override string Id { get { return "youtube"; } }

        long _nextTimeUpdateVideoID = 0;
        long _nextTimeUpdateStreamingDetails = 0;
        long _nextTimeUpdateMessage = 0;

        public YoutubeGaming_ChatSource(Dispatcher dispatcher)
            : base(dispatcher)
        {
        }

        public override void UpdateViewerCount()
        {
        }

        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 1000;
                return w;
            }
        }

        //YoutubeMessageContainer getById( string id ) {
        //    for (int j = 0; j < _cache.Count; ++j)
        //        if (_cache[j].id == id)
        //            return _cache[j];
        //    return null;
        //}

        Uri[] getBages(YoutubeMessageContainer b)
        {
            if (Properties.Settings.Default.youtube_ShowIcons)
            {
                return new System.Uri[] { new Uri(b.authorDetails.profileImageUrl, UriKind.Absolute) };
            }
            return new System.Uri[0];
        }

        private void readMessages(string activeLiveChatId)
        {

        }

        //eWRei_9cEO8
        public override void Create(string streamerUri, string id)
        {
            this.Label = this._id = id;
            this.Uri = this.SetKeywords(streamerUri, false);
            this.Tooltip = "youtube";

            _showComments = Properties.Settings.Default.Chat_LoadHistory;

            //_cache = new List<YoutubeMessageContainer>();
            _youtubeChannelId = "";
            _last = DateTime.Now.AddDays(-10);
            _chatThread = new System.Threading.Thread(chatThreadFunc);
            _chatThread.Start();
        }

        bool _abortRequested = false;
        System.Threading.Thread _chatThread;

        dynamic api(string url)
        {
            MyWebClient mwc = new MyWebClient();
            try
            {
                byte[] data = mwc.DownloadData(url);
                return Newtonsoft.Json.JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data));
            }
            catch (Exception er)
            {
                return null;
            }
        }

        string _youtubeChannelId;
        string _youtubeLiveVideoId;
        MyWebClient _chatLoader = new MyWebClient();

        void SetTooltip(string message = "")
        {
            string channel = _youtubeChannelId;
            if (string.IsNullOrEmpty(channel))
                channel = "Unknown";

            Tooltip = $"youtube: {channel}";

            if (!string.IsNullOrEmpty(message))
                Tooltip = $"{Tooltip} - {message}";
        }

        bool PopulateChannelID()
        {
            _youtubeChannelId = null;

            dynamic channelInfo = api(string.Format("https://www.googleapis.com/youtube/v3/channels?part=id&forUsername={0}&key={1}",
                HttpUtility.UrlEncode(this.Uri),
                youTubeAPIKey));

            if (channelInfo != null)
            {

                if (channelInfo.items == null) { Header = "ERROR!"; }
                else
                {
                    Header = "Loading 30%";
                    _youtubeChannelId = (string)channelInfo.items[0].id;
                }
            }

            if (string.IsNullOrEmpty(_youtubeChannelId))
                _youtubeChannelId = this.Uri;

            if (string.IsNullOrEmpty(_youtubeChannelId))
            {
                SetTooltip("Error PopulateChannelID: Still empty channel ID.");
                return false;
            }

            return true;
        }

        bool PopulateLiveVideoID()
        {
            _youtubeLiveVideoId = null;

            dynamic liveVideo = api(string.Format("https://www.googleapis.com/youtube/v3/search?part=id&channelId={0}&eventType=live&type=video&key={1}&rnd={2}",
                _youtubeChannelId,
                youTubeAPIKey, DateTime.Now.ToBinary()));

            if (liveVideo == null)
            {
                SetTooltip("Error PopulateLiveVideoID: Null");
                return false;
            }

            if (liveVideo.items.Count == 0)
            {
                SetTooltip("Error PopulateLiveVideoID: No items");
                return false;
            }

            _youtubeLiveVideoId = (string)liveVideo.items[0].id.videoId;

            if (string.IsNullOrEmpty(_youtubeLiveVideoId))
            {
                SetTooltip("Error PopulateLiveVideoID: Still empty video ID.");
                return false;
            }

            return true;
        }

        bool PopulateLiveStreamingDetails()
        {
            _liveChatId = null;

            dynamic details = api(string.Format(API_getLiveStreamingDetails,
                        _youtubeLiveVideoId,
                        youTubeAPIKey));

            if (details == null)
                return false;

            if (details.items.Count == 0)
                return false;

            if (details.items[0].liveStreamingDetails.concurrentViewers == null)
                return false;

            ViewersCount = (int)details.items[0].liveStreamingDetails.concurrentViewers;
            _liveChatId = (string)details.items[0].liveStreamingDetails.activeLiveChatId;

            return true;
        }

        bool GetMessages()
        {
            if (string.IsNullOrEmpty(_liveChatId))
                return false;

            try
            {
                byte[] data = _chatLoader.DownloadData(string.Format(API_getMessages, _liveChatId, youTubeAPIKey));

                string x = Encoding.UTF8.GetString(data);

                YoutubeLiveMessages d = Newtonsoft.Json.JsonConvert.DeserializeObject<YoutubeLiveMessages>(x);
                //sleepMs = d.pollingIntervalMillis;

                List<YoutubeMessageContainer> NewMessage = new List<YoutubeMessageContainer>();

                foreach (var m in from b in d.items
                                  orderby b.snippet.publishedAt
                                  select b)
                {
                    if (m.snippet.publishedAt > _last)
                    {
                        NewMessage.Add(m);
                        _last = m.snippet.publishedAt;
                    }
                }

                if (_showComments)
                {
                    if (NewMessage.Count > 0)
                    {
                        newMessagesArrived(from b in NewMessage
                                           orderby b.snippet.publishedAt
                                           select new TwoRatChat.Model.ChatMessage(getBages(b))
                                           {
                                               Date = DateTime.Now,
                                               Name = b.authorDetails.displayName,
                                               Text = HttpUtility.HtmlDecode(b.snippet.displayMessage),
                                               Source = this,
                                               Id = _id,
                                               ToMe = this.ContainKeywords(b.authorDetails.displayName),

                                               //Form = 0
                                           });
                    }

                }

                _showComments = true;
            }
            catch
            {
                return false;
            }

            return true;
        }

        long GetCurrentTimeInMS()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        }

        // Change these for timeouts.
        int _videoIDUpdateTime = 15000;
        int _streamingDetailsUpdateTime = 5000;
        int _messageUpdateTime = 2000;

        void chatThreadFunc()
        {
            Header = "Loading 0%";
            int sleepMs = 1000;
            int sleepErrorMs = 10000;

            while (!_abortRequested)
            {
                Status = false;

                // Try to load the channel ID if we don't have it yet.
                if (string.IsNullOrEmpty(_youtubeChannelId))
                {
                    if (!PopulateChannelID())
                    {
                        ViewersCount = 0;
                        Thread.Sleep(sleepErrorMs);
                        continue;
                    }
                }

                var curTime = GetCurrentTimeInMS();

                // Try to get the current live video ID if it's empty or if enough time has passed to re-update.
                if (string.IsNullOrEmpty(_youtubeLiveVideoId) || curTime >= _nextTimeUpdateVideoID)
                {
                    _nextTimeUpdateVideoID = curTime + _videoIDUpdateTime;

                    if (!PopulateLiveVideoID())
                    {
                        ViewersCount = 0;
                        Thread.Sleep(sleepErrorMs);
                        continue;
                    }
                }

                // There is a live video. Set status to true.
                Status = true;

                // Get the streaming details such as viewer count and live chat ID.
                if (curTime >= _nextTimeUpdateStreamingDetails)
                {
                    _nextTimeUpdateStreamingDetails = curTime + _streamingDetailsUpdateTime;

                    if (PopulateLiveStreamingDetails())
                    {
                        Header = $"{ViewersCount}";
                    }
                    else
                    {
                        Header = null;
                        //"Video live but could not get details";
                    }
                }

                // Get messages.
                if (!string.IsNullOrEmpty(_liveChatId))
                {
                    if (curTime >= _nextTimeUpdateMessage)
                    {
                        _nextTimeUpdateMessage = curTime + _messageUpdateTime;
                        GetMessages();
                    }
                }

                SetTooltip();

                Thread.Sleep(sleepMs);
            }
        }

        public override void Destroy()
        {
            _abortRequested = true;
            _chatThread.Abort();
        }
    }
}