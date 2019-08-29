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


[System.Serializable]
public class OnMessageEvent : UnityEvent<string>
{
}


public class Test : MonoBehaviour
{

    TCPServer server;
    HashSet<TcpClient> clients = new HashSet<TcpClient>();

    SynchronizationContext mainContext;

    public OnMessageEvent OnMessage;

    // Start is called before the first frame update
    void Start()
    {
        

        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7000);

        server = new TCPServer(endpoint);
        server.ClientHandler = ClientHandler;
        

        var t = server.Start();

        if (t.IsFaulted) t.Wait();
    }

    // クライアント接続時のハンドラ
    async void ClientHandler(TcpClient client)
    {
        Debug.Log($"Connected from {client.Client.RemoteEndPoint}");
        clients.Add(client);


        // 接続時メッセージ送信
        byte[] msg = Encoding.GetEncoding("Shift_JIS").GetBytes("Hello");
        client.GetStream().Write(msg, 0, msg.Length);
        client.GetStream().Flush();


        using (var s = client.GetStream())
        {
            await handleNetworkStream(client).ConfigureAwait(false);  // awaitable
        }

        
    }

    async Task handleNetworkStream(TcpClient client)
    {
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);

        // 接続が切れるまで送受信を繰り返す
        while (client.Connected)
        {
            while (!reader.EndOfStream)
            {
                // 一行分の文字列を受け取る
                var str = reader.ReadLine();
                Debug.Log(str);
                mainContext.Post(_ => OnMessage.Invoke(str), null);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {

    }

    private void OnDisable()
    {
        server.Stop();

        lock (clients)
        {
            foreach (var c in clients)
            {
                c.Close();
            }
        }
    }

}
