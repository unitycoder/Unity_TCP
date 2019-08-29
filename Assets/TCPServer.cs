using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


/// Rev4 fix async void 
class TCPServer
{
    /// <summary>
    /// 接続待ち受け endpoint
    /// </summary>
    protected IPEndPoint endpoint;

    protected TcpListener listener;
        
    /// <summary>
    /// 接続確立時の callback handler nullable
    /// </summary>
    protected Action<TcpClient> clientHandler;

    /// <summary>
    /// 接続待ち受けループの有効フラグ
    /// </summary>
    protected volatile bool acceptLoop = true;
        
/// <summary>
    /// 接続待ち受けループの終了フラグ
    /// </summary>
    protected ManualResetEvent acceptLoopLatch;

    /// <summary>
    /// 指定された endpoint で接続を待ち受ける TCPServer を構築する
    /// </summary>
    /// <param name="endpoint">待ち受けアドレス・ポート番号 notnull</param>
    public TCPServer(IPEndPoint endpoint)
    {
        if (endpoint == null)
            throw new ArgumentNullException("endpoint should not be null");

        this.endpoint = endpoint;
    }

    /// <summary>
    /// 接続受け付けハンドラ nullable
    /// </summary>
    public Action<TcpClient> ClientHandler {
        set {
            Volatile.Write(ref clientHandler, value);
        }
        get {
            return Volatile.Read(ref clientHandler);
        }
    }

    /// <summary>
    /// 接続待ち受けを開始する
    /// 受け付けた接続は TcpClient オブジェクトとして ClientHandler に callback される
    /// </summary>
    /// <returns>Taskオブジェクト</returns>
    /// <exception cref="InvalidOperationException">既に待ち受けている場合</exception>
    /// <exception cref="SocketException"></exception>
    /// <example>
    /// var t = server.Start();
    /// if (t.IsFaulted) t.Wait();    // Start 時に例外があった場合は、その例外を生じさせる
    /// </example>
    public async Task Start()
    {
        lock (this)
        {
            if (listener != null)
                throw new InvalidOperationException("Already started");

            acceptLoop = true;
            listener = new TcpListener(endpoint);
            acceptLoopLatch = new ManualResetEvent(false);
        }

        listener.Start();

        while (acceptLoop)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var handler = Volatile.Read(ref clientHandler);
                if (handler != null)
                {
                    handler(client);    // callback
                }
                else
                {
                    client.Close();     // close if no handler
                }
            }
            catch (ObjectDisposedException e)
            {
                // thrown if the listener socket is closed
            }
            catch (SocketException e)
            {
                // Some socket error
            }
        }
        acceptLoopLatch.Set();  // set exit flag
    }

    /// <summary>
    /// 接続待ち受けを停止する
    /// </summary>
    /// <remarks>
    /// 接続待ち受けを停止し、接続待ちスレッドの終了を待機する
    /// </remarks>
    /// <exception cref="System.InvalidCastException">Startが未実行の場合</exception>
    public void Stop()
    {
        if (listener == null)
            throw new InvalidOperationException("Not started");

        acceptLoop = false;
        listener.Stop();
        listener = null;
        acceptLoopLatch.WaitOne();  // wait exit flag
        acceptLoopLatch.Dispose();
        acceptLoopLatch = null;
    }
}
