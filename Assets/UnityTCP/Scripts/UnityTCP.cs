using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Kodai100.Tcp
{

    public enum SocketType
    {
        Server, Client
    }

    public class UnityTCP : MonoBehaviour
    {

        TCPServer server;
        TcpCommunicator client;

        public SocketType type = SocketType.Server;

        public string host = "127.0.0.1";
        public int port = 7000;

        public OnMessageEvent OnMessage;
        
        void Start()
        {

            if(type == SocketType.Server)
            {
                server = new TCPServer(new IPEndPoint(IPAddress.Any, port), OnMessage);

                var t = server.Listen();
                if (t.IsFaulted) t.Wait();
            }
            else
            {
                client = new TcpCommunicator(host, port, OnMessage);

                var t = client.Listen();
                if (t.IsFaulted) t.Wait();
            }
            
        }

        private void OnDisable()
        {

            if(type == SocketType.Server)
            {
                server.Stop();
            }
            else
            {
                client.Dispose();
            }

            
        }

    }


}