using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketServer : MonoBehaviour
{
    private TcpListener listener;
    private readonly List<TcpClient> clients = new List<TcpClient>();

    // ソケット接続準備、待機
    protected void Listen(IPEndPoint endPoint)
    {
        listener = new TcpListener(endPoint);
        listener.Start();

        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);
    }

    // クライアントからの接続処理
    private void DoAcceptTcpClientCallback(IAsyncResult ar)
    {
        var listener = (TcpListener)ar.AsyncState;
        var client = listener.EndAcceptTcpClient(ar);
        clients.Add(client);

        Debug.Log("Connect: " + client.Client.RemoteEndPoint);












        // 接続が確立したら次の人を受け付ける
        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

        // 今接続した人とのネットワークストリームを取得
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);

        // 接続が切れるまで送受信を繰り返す
        while (client.Connected)
        {
            while (!reader.EndOfStream)
            {
                // 一行分の文字列を受け取る
                var str = reader.ReadLine();
                OnMessage(str);
            }

            // クライアントの接続が切れたら
            if (client.Client.Poll(1000, SelectMode.SelectRead) && (client.Client.Available == 0))
            {
                Debug.Log("Disconnect: " + client.Client.RemoteEndPoint);
                client.Close();
                clients.Remove(client);
                break;
            }
        }
    }


    // メッセージ受信
    protected virtual void OnMessage(string msg)
    {
        Debug.Log(msg);
    }

    // クライアントにメッセージ送信
    protected void SendMessageToClient(string msg)
    {
        if (clients.Count == 0)
        {
            return;
        }

        var body = Encoding.UTF8.GetBytes(msg);

        // 全員に同じメッセージを送る
        foreach (var client in clients)
        {
            try
            {
                var stream = client.GetStream();
                stream.Write(body, 0, body.Length);
            }
            catch
            {
                clients.Remove(client);
            }
        }
    }

    // 終了処理
    protected virtual void OnApplicationQuit()
    {
        if (listener == null)
        {
            return;
        }

        if (clients.Count != 0)
        {
            foreach (var client in clients)
            {
                client.Close();
            }
        }
        listener.Stop();
    }
}
