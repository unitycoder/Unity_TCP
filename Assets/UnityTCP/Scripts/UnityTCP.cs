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

        public SocketType type = SocketType.Server;

        public string ip = "127.0.0.1";
        public int openPort = 7000;

        public OnMessageEvent OnMessage;
        
        void Start()
        {

            if(type == SocketType.Server)
            {
                server = new TCPServer(new IPEndPoint(IPAddress.Any, openPort), OnMessage);

                var t = server.Listen();
                if (t.IsFaulted) t.Wait();
            }
            
        }


        private void OnDisable()
        {

            if(type == SocketType.Server)
            {
                server.Stop();
            }

            
        }

    }


}