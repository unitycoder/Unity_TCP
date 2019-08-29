using UnityEngine.Events;

namespace Kodai100.Tcp
{

    [System.Serializable]
    public class OnMessageEvent : UnityEvent<string>
    {
    }

    public interface ITcpReceivable
    {
        void OnMessage(string message);
    }
}