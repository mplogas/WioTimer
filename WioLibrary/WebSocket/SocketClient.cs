using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WioLibrary.Logging;

namespace WioLibrary.WebSocket
{
    internal class SocketClient : IDisposable
    {
        //taken from https://gist.github.com/xamlmonkey/4737291
        //and modified
        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;
        private const int KeepAliveInterval = 20;

        private ClientWebSocket client;
        private readonly Uri uri;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken cancellationToken;

        private ILogWrapper logger;

        private Func<Task> onConnectedAction;
        private Func<string, Task> onMessageReceivedAction;
        private Func<Task> onDisconnectedAction;

        private SocketClient(string uri)
        {
            client = new ClientWebSocket();
            client.Options.KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval);
            this.uri = new Uri(uri);
            cancellationToken = cancellationTokenSource.Token;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server.</param>
        /// <param name="onConnect">The Action to call when the connection has been established.</param>
        /// <param name="onDisconnect">The Action to call when the connection has been terminated.</param>
        /// <param name="onMessage">The Action to call when a messages has been received.</param>
        /// <param name="logger">The logger</param>
        /// <returns></returns>
        internal static SocketClient Create(string uri, Func<Task> onConnect, Func<Task> onDisconnect, Func<string, Task> onMessage, ILogWrapper logger)
        {
            return new SocketClient(uri)
            {
                onConnectedAction = onConnect,
                onDisconnectedAction = onDisconnect,
                onMessageReceivedAction = onMessage,
                logger = logger
            };
        }

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <returns></returns>
        internal async Task ConnectAsync()
        {
            await ConnectAsync(cancellationToken);
        }

        internal async Task ConnectAsync(CancellationToken cancellation)
        {
            if (client.State != WebSocketState.Open)
            {
                await client.ConnectAsync(uri, cancellation);

                await CallOnConnectedAsync();
                await StartListen();
            }
        }

        /// <summary>
        /// Disconnects from the WebSocket server.
        /// </summary>
        /// <returns></returns>
        internal async Task DisconnectAsync(bool cleanUp)
        {
            await DisconnectAsync(cleanUp, cancellationToken);
        }

        internal async Task DisconnectAsync(bool cleanUp, CancellationToken cancellation)
        {
            if (client.State == WebSocketState.Open)
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellation);

                await CallOnDisconnectedAsync();
                
            }

            if (cleanUp) CleanUp();
        }

        /// <summary>
        /// Send a message to the WebSocket server.
        /// </summary>
        /// <param name="message">The message to send</param>
        internal async Task SendMessageAsync(string message)
        {
            await SendMessageAsync(message, cancellationToken);
        }

        internal async Task SendMessageAsync(string message, CancellationToken cancellation)
        {
            if (client.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > messageBuffer.Length)
                {
                    count = messageBuffer.Length - offset;
                }

                await client.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, cancellation);
            }
        }
        
        private void CleanUp()
        {
            onConnectedAction = null;
            onDisconnectedAction = null;
            onMessageReceivedAction = null;
        }

        private async Task StartListen()
        {
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (client.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();


                    WebSocketReceiveResult result;
                    do
                    {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await
                                client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            await CallOnDisconnectedAsync();
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }

                    } while (!result.EndOfMessage);

                    await CallOnMessageAsync(stringResult);

                }
            }
            catch (Exception)
            {
                await CallOnDisconnectedAsync();
            }
            finally
            {
                client.Dispose();
            }
        }

        private async Task CallOnMessageAsync(StringBuilder stringResult)
        {
            if (onMessageReceivedAction != null)
                await onMessageReceivedAction(stringResult.ToString());
        }

        private async Task CallOnDisconnectedAsync()
        {
            if (onDisconnectedAction != null)
                await onDisconnectedAction();
        }

        private async Task CallOnConnectedAsync()
        {
            if (onConnectedAction != null)
                await onConnectedAction();
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || client == null) return;

            client.Dispose();
            onConnectedAction = null;
            onDisconnectedAction = null;
            onMessageReceivedAction = null;
            client = null;
        }
        #endregion
    }
}