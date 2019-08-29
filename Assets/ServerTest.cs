using System.Net;
using UnityEngine;


public class ServerTest : SocketServer
{
    // ポート指定（他で使用していないもの、使用されていたら手元の環境によって変更）
    [SerializeField] private int port = 7000;

    private void Start()
    {
        // 指定したポートを開く
        Listen(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
    }

    // クライアントからメッセージ受信
    protected override void OnMessage(string msg)
    {
        base.OnMessage(msg);

        // -------------------------------------------------------------
        // あとは送られてきたメッセージによって何かしたいことを書く
        // -------------------------------------------------------------

        // 今回は受信した整数値を表示用システムにセットする
        int num;
        // 整数値以外は何もしない
        if (int.TryParse(msg, out num))
        {
            // 値をセットする
            Debug.Log(num);
            // クライアントに受領メッセージを返す
            SendMessageToClient("Accept:" + num + "\n");
        }
        else
        {
            // クライアントにエラーメッセージを返す
            
        }
    }


    public void Send()
    {
        SendMessageToClient("Error\n");
    }
}
