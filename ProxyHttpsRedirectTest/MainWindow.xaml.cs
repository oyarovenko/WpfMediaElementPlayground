using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private const string RemoteUrl = @"https://video-dev.github.io/streams/x36xhzz/url_8/url_596/";
        //private const string ResourceName = @"193039199_mp4_h264_aac_fhd_7.ts";

        private const string RemoteUrl = @"https://commondatastorage.googleapis.com/gtv-videos-bucket/sample";
        private const string ResourceName = "ForBiggerBlazes.mp4"; // 00:15
        //private const string ResourceName = "TearsOfSteel.mp4"; //super long - 12:44
        private const string ServerPath = @"http://localhost:8573/";

        private HttpListener _server;
        private HttpClient _client;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            _server = new HttpListener();
            _server.Prefixes.Add(ServerPath);
            _server.Start();

            _client = new HttpClient();
            _cts = new CancellationTokenSource();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            Closing += OnWinClosing;
        }

        private void ButtonLoadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                mediaPanel.Source = new Uri($"{ServerPath}/{ResourceName}");
                _server.BeginGetContext(OnMessageArrived, _server);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void OnWinClosing(object sender, CancelEventArgs e)
        {
            _cts?.Cancel();
            _client?.Dispose();
            _server?.Stop();
        }

        #region HTTP Client

        private async Task<HttpResponseMessage> HeadVideoRequest(string resourceName)
        {
            var result = await _client.SendAsync(
                new HttpRequestMessage
                {
                    Method = HttpMethod.Head,
                    RequestUri = new Uri($"{RemoteUrl}{resourceName}"),
                    Version = new Version(1, 1),
                }, _cts.Token);

            return result;
        }

        private async Task<HttpResponseMessage> GetVideoRequest(string resourceName)
        {
            var result = await _client.SendAsync(
                new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{RemoteUrl}{resourceName}"),
                    Version = new Version(1, 1),
                }, HttpCompletionOption.ResponseHeadersRead, _cts.Token);

            return result;
        }

        #endregion

        #region HTTP Server

        private async void OnMessageArrived(IAsyncResult ar)
        {
            var listiner = (HttpListener)ar.AsyncState;
            HttpListenerContext context = listiner.EndGetContext(ar);

            var request = context.Request;
            string resourceName = request.RawUrl;

            var getVideoResponce = await GetVideoRequest(resourceName);
            getVideoResponce.EnsureSuccessStatusCode();

            using (var output = context.Response.OutputStream)
            {
                using (var networkStream = await getVideoResponce.Content.ReadAsStreamAsync())
                {
                    _cts.Token.Register(() =>
                    {
                        output.Close();
                        networkStream.Close();
                    });

                    //cancellation still not working as expected, consider other options for copying or handle cancelleation exceptions
                    await networkStream.CopyToAsync(output, 4096, _cts.Token);
                }
            }
        }

        #endregion
    }
}
