using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WioLibrary.Logging;
using WioLibrary.WebSocket;

namespace WioLibrary
{
    public class SocketManager
    {
        private List<SocketClientWrapper> sockets;
        private static readonly SocketManager instance = new SocketManager();
        private ILogWrapper logger;

        static SocketManager() { }

        private SocketManager()
        {
            this.sockets = new List<SocketClientWrapper>();
        }

        public static SocketManager Instance => instance;

        public void Initialize(ILogWrapper logger)
        {
            if (logger == null) throw new ArgumentNullException("Logger must not be NULL!");
            this.logger = logger;
        }

        public async Task AddAsync(string id, string uri, Func<Task> onConnect, Func<Task> onDisconnect, Func<string, Task> onMessage, bool connect)
        {
            if (HasItem(id)) return;

            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("Id must not be NULL!");
            if (string.IsNullOrEmpty(uri)) throw new ArgumentNullException("Uri must not be NULL!");
            if (onConnect == null || onDisconnect == null || onMessage == null) throw new ArgumentNullException("Func must not be NULL!");
            if (this.logger == null) throw new InvalidOperationException("Not initialized yet! Run Initialize() first!");

            var socket = new SocketClientWrapper(id, uri, onConnect, onDisconnect, onMessage, this.logger);
            if (connect) await socket.Socket.ConnectAsync();
        }

        public async Task ConnectAsync(string id)
        {
            if(!HasItem(id)) return;

            await sockets.First(s => s.Id.Equals(id)).Socket.ConnectAsync();
        }

        public async Task DisconnectAsync(string id)
        {
            if (!HasItem(id)) return;

            await sockets.First(s => s.Id.Equals(id)).Socket.DisconnectAsync(false);

        }

        public async Task Remove(string id)
        {
            if (!HasItem(id)) return;

            await sockets.First(s => s.Id.Equals(id)).Socket.DisconnectAsync(false);
        }

        public async Task Send(string id, string message)
        {
            if (!HasItem(id)) return;

            await sockets.First(s => s.Id.Equals(id)).Socket.SendMessageAsync(message);
        }

        private bool HasItem(string id)
        {
            return sockets.Any(s => s.Id.Equals(id.Trim()));
        }
   }
}