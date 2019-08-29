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

    public class TCPServer : MonoBehaviour
    {

        UnityTCP server;

        public int openPort = 7000;
        public OnMessageEvent OnMessage;
        
        void Start()
        {

            server = new UnityTCP(new IPEndPoint(IPAddress.Any, openPort), OnMessage);

            var t = server.Listen();
            if (t.IsFaulted) t.Wait();
        }


        private void OnDisable()
        {
            server.Stop();
        }

    }


}