using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WioTimer
{
    public class Logic : IDisposable
    {
        private HttpClient http;
        private WebSocketWrapper wsWrapper;
        private bool isConnected;
        private bool isRunning;

       
        public Logic(HttpClient httpClient)
        {
            if(httpClient == null) throw new ArgumentNullException("Parameters MUST NOT be NULL.");

            this.http = httpClient;
            this.wsWrapper = WebSocketWrapper.Create(ConfigReader.GetValue<string>("ws-uri"));
            this.wsWrapper = wsWrapper.OnConnect(ConnectAction).OnDisconnect(DisconnectAction).OnMessage(SendAction);
        }

        public void Connect()
        {
            this.wsWrapper.Connect();
        }

        private void ConnectAction(WebSocketWrapper wrapper) 
        {
            wrapper.SendMessage(ConfigReader.GetValue<string>("ws-key"));
            isConnected = true;
            Logger.Write(Severity.Info, "WebSocket connection established.");
        }

        private void DisconnectAction(WebSocketWrapper wrapper)
        {
            Logger.Write(Severity.Info, "WebSocket connection has been closed. Trying to re-establish connection.");
            isConnected = false;

            for (var i = 1; i <= 10; i++)
            {
                if (isConnected) break;
                Logger.Write(Severity.Info, $"Attempt {i}");
                wrapper.Connect();
            }

            if(!isConnected) Logger.Write(Severity.Error, "Failed to re-establish connection. Giving up!");
        }

        private void SendAction(string msg, WebSocketWrapper wrapper)
        {
            Logger.Write(Severity.Info, $"Message {msg} has been received.");

            if (IsButtonPressed(msg))
            {
                var rainbowOn = string.Format(
                    ConfigReader.GetValue<string>("http-rainbow"),
                    ConfigReader.GetValue<int>("amount-led"), 
                    ConfigReader.GetValue<int>("brightness-rainbow"),
                    ConfigReader.GetValue<int>("speed-rainbow"), 
                    ConfigReader.GetValue<string>("ws-key"));
                var rainbowOff = string.Format(
                    ConfigReader.GetValue<string>("http-clear"),
                    ConfigReader.GetValue<int>("amount-led"),
                    ConfigReader.GetValue<string>("ws-key"));
                var duration = ConfigReader.GetValue<int>("timer-duration");

                Task.Run(async () =>
                {
                    if (!isRunning)
                    {
                        var responseOn = await this.http.PostAsync(rainbowOn, null);
                        if (!responseOn.IsSuccessStatusCode)
                        {
                            Logger.Write(Severity.Warning,
                                $"Could not start rainbow. HttpResponse is {responseOn.StatusCode}, {responseOn.ReasonPhrase}");
                            return;
                        }

                        isRunning = true;
                        Logger.Write(Severity.Info, "Turning rainbow light on");
                        await Task.Delay(duration);
                    }

                    var responseOff = await this.http.PostAsync(rainbowOff, null);
                    if (!responseOff.IsSuccessStatusCode)
                    {
                        Logger.Write(Severity.Warning, $"Could not stop rainbow. HttpResponse is {responseOff.StatusCode}, {responseOff.ReasonPhrase}");
                        return;
                    }
                    isRunning = false;
                    Logger.Write(Severity.Info, "Turning rainbow light off");
                });
            }
        }

        private bool IsButtonPressed(string msg)
        {
            try
            {
                var jObject = JObject.Parse(msg);
                var result = (string) jObject["msg"]["button_pressed"];
                if (result.Equals("14")) return true;
            }
            catch (Exception e)
            {
                Logger.Write(Severity.Warning, $"Failed to parse JSON message. {e.Message}");
            }
            
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (this.wsWrapper != null)
            {
                this.wsWrapper.Dispose();
                this.wsWrapper = null;
            }

            if (this.http != null)
            {
                this.http.Dispose();
                this.http = null;
            }
        }
    }
}
