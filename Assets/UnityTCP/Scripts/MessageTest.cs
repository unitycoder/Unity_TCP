using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Kodai100.Tcp;

public class MessageTest : MonoBehaviour
{

    public UnityTCP tcpServer;

    List<string> list = new List<string>();

    public string text;
    
    public int clientId;
    public int ClientID {
        get {
            if (clientId >= tcpServer.Clients.Count) return tcpServer.Clients.Count - 1;
            if (clientId < 0) return 0;
            else return clientId;
        }
    }
    
    public void OnMessage(string message)
    {
        list.Add(message);

        Debug.Log($"<color=cyan>Received</color> : {message}");
    }

    public void OnEstablished(TcpClient client)
    {
        var endpoint = client.Client.RemoteEndPoint;
        Debug.Log($"<color=yellow>Established</color> : {endpoint}");

        tcpServer.SendMessageToClient(client, "Established");

    }

    public void OnDisconnected(EndPoint endpoint)
    {
        Debug.Log($"<color=red>Disconnected</color> : {endpoint}");
    }


    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.B))
        {
            tcpServer.BroadcastToClients(text);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            tcpServer.SendMessageToClient(tcpServer.Clients[ClientID], text);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            tcpServer.SendMessageToServer(text);
        }

    }


    private void OnGUI()
    {

        using(new GUILayout.VerticalScope())
        {
            list.ForEach(elem =>
            {
                GUILayout.Label(elem);
            });

        }
        
    }
}
