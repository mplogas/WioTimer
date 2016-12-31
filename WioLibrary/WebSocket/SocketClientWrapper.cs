using System;
using System.Threading.Tasks;
using WioLibrary.Logging;

namespace WioLibrary.WebSocket
{
    internal class SocketClientWrapper
    {
        private readonly ILogWrapper logger;
        internal string Id { get; private set; }
        internal SocketClient Socket { get; private set; }
        internal Func<Task> ConnectAction { get; private set; }
        internal Func<Task> DisconnectAction { get; private set; }
        internal Func<string, Task> SendMessageAction { get; private set; }
        internal string Uri { get; private set; }

        internal SocketClientWrapper(string id, string uri, Func<Task> connectActionAsync, Func<Task> disconnectActionAsync, Func<string, Task> messageActionAsync, ILogWrapper logger)
        {
            this.Id = id;
            this.Uri = uri;
            this.logger = logger; 

            //this allows me to hook into the actions
            this.ConnectAction = async () =>
            {
                this.logger.Write(LogSeverity.Debug, "onConnect Action invoked");
                await connectActionAsync.Invoke();

                await Socket.SendMessageAsync(id);
            };

            this.DisconnectAction = async () =>
            {
                this.logger.Write(LogSeverity.Debug, "onDisconnect Action invoked");
                await disconnectActionAsync.Invoke();
            };

            this.SendMessageAction = async (message) =>
            {
                this.logger.Write(LogSeverity.Debug, "messageAction invoked");
                await messageActionAsync.Invoke(message);
            };

            this.Socket = SocketClient.Create(this.Uri, this.ConnectAction, this.DisconnectAction,
                this.SendMessageAction, this.logger);
        }
        
    }
}