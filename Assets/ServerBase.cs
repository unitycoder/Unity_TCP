using System.Net;
using System.Net.Sockets;

public abstract class ServerBase
{
    public int AcceptCount;
    public int ReadCount;
    public int WriteCount;
    public int CloseCount;
    public int CloseByPeerCount;
    public int CloseByInvalidStream;
    public readonly IPEndPoint Listen;
    protected const int headerSize = 4;
    protected const int backlog = 1000;
    protected const int bufferSize = 1000;
    protected const char terminate = '\n';

    public ServerBase(IPEndPoint endpoint)
    {
        Listen = endpoint;
    }

    abstract public void Run();

    protected void setSocketOption(Socket sock)
    {
        sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
    }

    public override string ToString()
    {
        return $"accept({AcceptCount}) close({CloseCount}) peer({CloseByPeerCount}) + invalid({CloseByInvalidStream}) read({ReadCount}) write({WriteCount}) : {GetType().Name}";
    }
}