using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

class AwaitServer : ServerBase
{
    public AwaitServer(IPEndPoint endpoint) : base(endpoint)
    {
    }

    public override void Run()
    {
        run(Listen).Wait();
    }

    async Task run(IPEndPoint end)
    {
        var listener = new TcpListener(end);
        listener.Start(backlog);
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            Interlocked.Increment(ref AcceptCount);
            handleTcpClient(client);
        }
    }

    async void handleTcpClient(TcpClient client)
    {
        setSocketOption(client.Client);
        try
        {
            using (var s = client.GetStream())
            {
                await handleNetworkStream(s).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Interlocked.Increment(ref CloseCount);
            client.Close();
        }
    }

    async Task handleNetworkStream(NetworkStream stream)
    {
        var buffer = new byte[bufferSize];
        while (true)
        {
            if (!await fill(stream, buffer, headerSize).ConfigureAwait(false))
            {
                Interlocked.Increment(ref CloseByInvalidStream);
                return;
            }
            var length = BitConverter.ToInt32(buffer, 0);
            if (length == 0)
            {
                Interlocked.Increment(ref CloseByPeerCount);
                return;
            }
            if (!await fill(stream, buffer, length).ConfigureAwait(false))
            {
                Interlocked.Increment(ref CloseByInvalidStream);
                return;
            }
            await stream.WriteAsync(buffer, 0, length).ConfigureAwait(false);
            Interlocked.Increment(ref WriteCount);
        }
    }

    async Task<bool> fill(NetworkStream stream, byte[] buffer, int rest)
    {
        if (rest > buffer.Length)
        {
            return false;
        }

        int offset = 0;
        while (rest > 0)
        {
            var length = await stream.ReadAsync(buffer, offset, rest).ConfigureAwait(false);
            Interlocked.Increment(ref ReadCount);
            if (length == 0)
            {
                return false;
            }
            rest -= length;
            offset += length;
        }
        return true;
    }
}
