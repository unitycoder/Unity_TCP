using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

namespace Kodai100.Tcp
{
    
    class TCPServer : IDisposable
    {
        
        IPEndPoint endpoint;

        TcpListener listener;
        HashSet<TcpClient> clients = new HashSet<TcpClient>();

        SynchronizationContext mainContext;
        volatile bool acceptLoop = true;
        
        OnMessageEvent OnMessage;


        public TCPServer(IPEndPoint endpoint, OnMessageEvent onMessage)
        {
            this.endpoint = endpoint ?? throw new ArgumentNullException("endpoint should not be null");

            // Set Unity main thread
            mainContext = SynchronizationContext.Current;

            OnMessage = onMessage;
        }
        
        public async Task Listen()
        {
            lock (this)
            {
                if (listener != null)
                    throw new InvalidOperationException("Already started");

                acceptLoop = true;
                listener = new TcpListener(endpoint);
            }

            listener.Start();

            while (acceptLoop)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    
                    var _ = Task.Run(() => OnConnectClient(client));

                }
                catch (ObjectDisposedException e)
                {
                    // thrown if the listener socket is closed
                }
                catch (SocketException e)
                {
                    // Some socket error
                }
            }
        }



        public void Stop()
        {

            lock (this)
            {

                if (listener == null)
                    throw new InvalidOperationException("Not started");

                acceptLoop = false;

                listener.Stop();
                listener = null;

            }


            lock (clients)
            {
                foreach (var c in clients)
                {
                    c.Close();
                }
            }

        }

        async Task OnConnectClient(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint;

            Debug.Log($"<color=yellow>Established</color> : {clientEndpoint}");
            clients.Add(client);

            ReturnMessageToClient(client);

            await NetworkStreamHandler(client).ConfigureAwait(false);

            Debug.Log($"<color=red>Disconnected</color> : {clientEndpoint}");
            clients.Remove(client);
        }


        void ReturnMessageToClient(TcpClient client)
        {
            var terminator = "\r\n";

            byte[] msg = Encoding.GetEncoding("UTF-8").GetBytes($"Established{terminator}");
            client.GetStream().Write(msg, 0, msg.Length);
            client.GetStream().Flush();
        }



        async Task NetworkStreamHandler(TcpClient client)
        {

            while (client.Connected)
            {
                using (var stream = client.GetStream())
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);

                    while (!reader.EndOfStream)
                    {
                        // TODO : currently, supported string decode only
                        var str = await reader.ReadLineAsync();
                        mainContext.Post(_ => OnMessage.Invoke(str), null);
                    }

                }

            }

            // Disconnected
        }

        public void Dispose()
        {
            Stop();
        }
    }

}