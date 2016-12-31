using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using WioLibrary;
using WioLibrary.Logging;

namespace Wiotimer.Core
{
    public class Logic
    {
        private readonly IConfigurationRoot config;
        private readonly SocketManager sockets;
        private readonly ILogWrapper logger;
        private readonly HttpClient http;

        private Func<Task> onConnect = async () => { };
        private Func<Task> onDisconnect = async () => { };
        private Func<string, Task> onMessage = async (message) => { };

        private bool isRunning;

        public Logic(ILogWrapper logger, HttpClient http)
        {
            this.config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .AddEnvironmentVariables()
                .Build();

            this.logger = logger;
            this.http = http;
            this.sockets = SocketManager.Instance;
            this.sockets.Initialize(this.logger);
        }

        public async Task Connect()
        {
            onMessage = async (msg) => { RainbowTask(msg); };

            await
                this.sockets.AddAsync(this.config["WebSocket:Id"], $"{this.config["WebSocket:Protocol"]}{this.config["BaseUri"]}{this.config["WebSocket:Endpoint"]}", onConnect, onDisconnect,
                    onMessage, true);
        }

        public async Task Disconnect()
        {
            await this.sockets.Remove(this.config[""]);
        }

        private bool IsButtonPressed(string msg)
        {
            try
            {
                var jObject = JObject.Parse(msg);
                var result = (string)jObject["msg"]["button_pressed"];
                if (result.Equals("14")) return true;
            }
            catch (Exception e)
            {
                this.logger.Write(LogSeverity.Warn, e);
            }

            return false;
        }

        private void RainbowTask(string msg)
        {
            logger.Write(LogSeverity.Debug, $"Message {msg} has been received.");

            if (!IsButtonPressed(msg)) return;

            var rainbowOn = GetRainbowOnUri();
            var rainbowOff = GetRainbowOffUri();
            var duration = GetRainbowDuration();
                
            Task.Run(async () =>
            {
                try
                {
                    if (!isRunning)
                    {
                        var responseOn = await this.http.PostAsync(rainbowOn, null);
                        if (!responseOn.IsSuccessStatusCode)
                        {
                            this.logger.Write(LogSeverity.Warn,
                                $"Could not start rainbow. HttpResponse is {responseOn.StatusCode}, {responseOn.ReasonPhrase}");
                            return;
                        }

                        isRunning = true;
                        this.logger.Write(LogSeverity.Debug, "Turning rainbow light on");

                        //TODO: I don't like it. I think a timer would be better. Idk vOv
                        await Task.Delay(duration);
                    }

                    //avoid multiple posts to turn off the rainbow
                    if (isRunning)
                    {
                        var responseOff = await this.http.PostAsync(rainbowOff, null);
                        if (!responseOff.IsSuccessStatusCode)
                        {
                            this.logger.Write(LogSeverity.Warn,
                                $"Could not stop rainbow. HttpResponse is {responseOff.StatusCode}, {responseOff.ReasonPhrase}");
                            return;
                        }
                        isRunning = false;
                        this.logger.Write(LogSeverity.Debug, "Turning rainbow light off");
                    }
                    
                }
                catch (Exception e)
                {
                    this.logger.Write(LogSeverity.Error, e);
                }
            });
        }

        private string GetRainbowOnUri()
        {
            return $"{this.config["Rainbow:Protocol"]}{this.config["BaseUri"]}{this.config["LED:Endpoint"]}/{string.Format(config["Rainbow:HTTP-Start"], config["LED:Amount"], config["Rainbow:Brightness"], config["Rainbow:Speed"], config["WebSocket:Id"])}";
        }

        private string GetRainbowOffUri()
        {
            return $"{this.config["Rainbow:Protocol"]}{this.config["BaseUri"]}{this.config["LED:Endpoint"]}/{string.Format(config["Rainbow:HTTP-Stop"], config["LED:Amount"], config["WebSocket:Id"])}";
        }

        private int GetRainbowDuration()
        {
            int duration;
            if (int.TryParse(config["Rainbow:Duration"], out duration))
            {
                duration = duration * 1000; //get milliseconds
            }
            else
            {
                duration = 6000;
            }

            return duration;
        }
    }
}