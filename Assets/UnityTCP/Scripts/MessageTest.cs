using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kodai100.Tcp;

public class MessageTest : MonoBehaviour, ITcpReceivable
{

    List<string> list = new List<string>();

    
    public void OnMessage(string message)
    {
        list.Add(message);

        Debug.Log($"<color=cyan>Received</color> : {message}");
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
