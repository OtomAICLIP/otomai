using System.Net;
using System.Net.Sockets;
using System.Text;
using Bubble.Core.Network.Proxy;
using Serilog;

namespace Bubble.Core.Network;

/// <summary>
/// TCP client is used to read/write data from/into the connected TCP server
/// </summary>
/// <remarks>Thread-safe</remarks>
public class TcpClient : IDisposable
{
    /// <summary>
    /// Initialize TCP client with a given server IP address and port number
    /// </summary>
    /// <param name="address">IP address</param>
    /// <param name="port">Port number</param>
    public TcpClient(IPAddress address, int port) : this(new IPEndPoint(address, port), null) { }

    /// <summary>
    /// Initialize TCP client with a given server IP address and port number
    /// </summary>
    /// <param name="address">IP address</param>
    /// <param name="port">Port number</param>
    public TcpClient(string address, int port, Socks5Options? proxy) : this(
        new IPEndPoint(IPAddress.Parse(address), port),
        proxy) { }

    /// <summary>
    /// Initialize TCP client with a given DNS endpoint
    /// </summary>
    /// <param name="endpoint">DNS endpoint</param>
    public TcpClient(DnsEndPoint endpoint) : this(endpoint as EndPoint, endpoint.Host, endpoint.Port, null) { }

    /// <summary>
    /// Initialize TCP client with a given IP endpoint
    /// </summary>
    /// <param name="endpoint">IP endpoint</param>
    public TcpClient(IPEndPoint endpoint, Socks5Options? proxy) : this(endpoint as EndPoint,
                                                                       endpoint.Address.ToString(),
                                                                       endpoint.Port,
                                                                       proxy) { }

    public Socks5Options? Proxy { get; set; }

    /// <summary>
    /// Initialize TCP client with a given endpoint, address and port
    /// </summary>
    /// <param name="endpoint">Endpoint</param>
    /// <param name="address">Server address</param>
    /// <param name="port">Server port</param>
    private TcpClient(EndPoint endpoint, string address, int port, Socks5Options? proxy)
    {
        Proxy = proxy;
        Id = Guid.NewGuid();
        Address = address;
        Port = port;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Client Id
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// TCP server address
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// TCP server port
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Endpoint
    /// </summary>
    public EndPoint Endpoint { get; private set; }

    /// <summary>
    /// Socket
    /// </summary>
    public Socket Socket { get; private set; }

    /// <summary>
    /// Number of bytes pending sent by the client
    /// </summary>
    public long BytesPending { get; private set; }

    /// <summary>
    /// Number of bytes sending by the client
    /// </summary>
    public long BytesSending { get; private set; }

    /// <summary>
    /// Number of bytes sent by the client
    /// </summary>
    public long BytesSent { get; private set; }

    /// <summary>
    /// Number of bytes received by the client
    /// </summary>
    public long BytesReceived { get; private set; }

    /// <summary>
    /// Option: dual mode socket
    /// </summary>
    /// <remarks>
    /// Specifies whether the Socket is a dual-mode socket used for both IPv4 and IPv6.
    /// Will work only if socket is bound on IPv6 address.
    /// </remarks>
    public bool OptionDualMode { get; set; }

    /// <summary>
    /// Option: keep alive
    /// </summary>
    /// <remarks>
    /// This option will setup SO_KEEPALIVE if the OS support this feature
    /// </remarks>
    public bool OptionKeepAlive { get; set; }

    /// <summary>
    /// Option: TCP keep alive time
    /// </summary>
    /// <remarks>
    /// The number of seconds a TCP connection will remain alive/idle before keepalive probes are sent to the remote
    /// </remarks>
    public int OptionTcpKeepAliveTime { get; set; } = -1;

    /// <summary>
    /// Option: TCP keep alive interval
    /// </summary>
    /// <remarks>
    /// The number of seconds a TCP connection will wait for a keepalive response before sending another keepalive probe
    /// </remarks>
    public int OptionTcpKeepAliveInterval { get; set; } = -1;

    /// <summary>
    /// Option: TCP keep alive retry count
    /// </summary>
    /// <remarks>
    /// The number of TCP keep alive probes that will be sent before the connection is terminated
    /// </remarks>
    public int OptionTcpKeepAliveRetryCount { get; set; } = -1;

    /// <summary>
    /// Option: no delay
    /// </summary>
    /// <remarks>
    /// This option will enable/disable Nagle's algorithm for TCP protocol
    /// </remarks>
    public bool OptionNoDelay { get; set; }

    /// <summary>
    /// Option: receive buffer limit
    /// </summary>
    public int OptionReceiveBufferLimit { get; set; } = 0;

    /// <summary>
    /// Option: receive buffer size
    /// </summary>
    public int OptionReceiveBufferSize { get; set; } = 8192;

    /// <summary>
    /// Option: send buffer limit
    /// </summary>
    public int OptionSendBufferLimit { get; set; } = 0;

    /// <summary>
    /// Option: send buffer size
    /// </summary>
    public int OptionSendBufferSize { get; set; } = 8192;

    #region Connect/Disconnect client

    private SocketAsyncEventArgs _connectEventArg;

    /// <summary>
    /// Is the client connecting?
    /// </summary>
    public bool IsConnecting { get; private set; }

    /// <summary>
    /// Is the client connected?
    /// </summary>
    public bool IsConnected { get; private set; }

    public bool ProxyConnected { get; private set; }

    /// <summary>
    /// Create a new socket object
    /// </summary>
    /// <remarks>
    /// Method may be override if you need to prepare some specific socket object in your implementation.
    /// </remarks>
    /// <returns>Socket object</returns>
    protected virtual Socket CreateSocket()
    {
        return new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }

    /// <summary>
    /// Connect the client (synchronous)
    /// </summary>
    /// <remarks>
    /// Please note that synchronous connect will not receive data automatically!
    /// You should use Receive() or ReceiveAsync() method manually after successful connection.
    /// </remarks>
    /// <returns>'true' if the client was successfully connected, 'false' if the client failed to connect</returns>
    public virtual bool Connect()
    {
        if (IsConnected || IsConnecting)
            return false;


        // Setup buffers
        _receiveBuffer = new Buffer();
        _sendBufferMain = new Buffer();
        _sendBufferFlush = new Buffer();

        // Setup event args
        _connectEventArg = new SocketAsyncEventArgs();
        _connectEventArg.RemoteEndPoint = Endpoint;
        _connectEventArg.Completed += OnAsyncCompleted;
        _receiveEventArg = new SocketAsyncEventArgs();
        _receiveEventArg.Completed += OnAsyncCompleted;
        _sendEventArg = new SocketAsyncEventArgs();
        _sendEventArg.Completed += OnAsyncCompleted;

        Socket = CreateSocket();

        // Update the client socket disposed flag
        IsSocketDisposed = false;

        // Apply the option: dual mode (this option must be applied before connecting)
        if (Socket.AddressFamily == AddressFamily.InterNetworkV6)
            Socket.DualMode = OptionDualMode;

        // Call the client connecting handler
        OnConnecting();

        try
        {
            // Connect to the server
            Socket.Connect(Endpoint);
        }
        catch (SocketException ex)
        {
            // Call the client error handler
            SendError(ex.SocketErrorCode);

            // Reset event args
            _connectEventArg.Completed -= OnAsyncCompleted;
            _receiveEventArg.Completed -= OnAsyncCompleted;
            _sendEventArg.Completed -= OnAsyncCompleted;

            // Call the client disconnecting handler
            OnDisconnecting();
            ProxyConnected = false;
            
            // Close the client socket
            Socket.Close();

            // Dispose the client socket
            Socket.Dispose();

            // Dispose event arguments
            _connectEventArg.Dispose();
            _receiveEventArg.Dispose();
            _sendEventArg.Dispose();

            // Call the client disconnected handler
            OnDisconnected();

            return false;
        }

        // Apply the option: keep alive
        if (OptionKeepAlive)
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        if (OptionTcpKeepAliveTime >= 0)
            Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, OptionTcpKeepAliveTime);
        if (OptionTcpKeepAliveInterval >= 0)
            Socket.SetSocketOption(SocketOptionLevel.Tcp,
                                   SocketOptionName.TcpKeepAliveInterval,
                                   OptionTcpKeepAliveInterval);
        if (OptionTcpKeepAliveRetryCount >= 0)
            Socket.SetSocketOption(SocketOptionLevel.Tcp,
                                   SocketOptionName.TcpKeepAliveRetryCount,
                                   OptionTcpKeepAliveRetryCount);
        // Apply the option: no delay
        if (OptionNoDelay)
            Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

        // Prepare receive & send buffers
        _receiveBuffer.Reserve(OptionReceiveBufferSize);
        _sendBufferMain.Reserve(OptionSendBufferSize);
        _sendBufferFlush.Reserve(OptionSendBufferSize);

        // Reset statistic
        BytesPending = 0;
        BytesSending = 0;
        BytesSent = 0;
        BytesReceived = 0;

        // Update the connected flag
        IsConnected = true;

        // Call the client connected handler
        OnConnected();

        // Call the empty send buffer handler
        if (_sendBufferMain.IsEmpty)
            OnEmpty();

        return true;
    }

    /// <summary>
    /// Disconnect the client (synchronous)
    /// </summary>
    /// <returns>'true' if the client was successfully disconnected, 'false' if the client is already disconnected</returns>
    public virtual bool Disconnect()
    {
        if (!IsConnected && !IsConnecting)
            return false;

        // Cancel connecting operation
        if (IsConnecting)
            Socket.CancelConnectAsync(_connectEventArg);

        // Reset event args
        _connectEventArg.Completed -= OnAsyncCompleted;
        _receiveEventArg.Completed -= OnAsyncCompleted;
        _sendEventArg.Completed -= OnAsyncCompleted;

        // Call the client disconnecting handler
        OnDisconnecting();
        ProxyConnected = false;
        
        try
        {
            try
            {
                // Shutdown the socket associated with the client
                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException) { }

            // Close the client socket
            Socket.Close();

            // Dispose the client socket
            Socket.Dispose();

            // Dispose event arguments
            _connectEventArg.Dispose();
            _receiveEventArg.Dispose();
            _sendEventArg.Dispose();

            // Update the client socket disposed flag
            IsSocketDisposed = true;
        }
        catch (ObjectDisposedException) { }

        // Update the connected flag
        IsConnected = false;

        // Update sending/receiving flags
        _receiving = false;
        _sending = false;

        // Clear send/receive buffers
        ClearBuffers();

        // Call the client disconnected handler
        OnDisconnected();

        return true;
    }

    /// <summary>
    /// Reconnect the client (synchronous)
    /// </summary>
    /// <returns>'true' if the client was successfully reconnected, 'false' if the client is already reconnected</returns>
    public virtual bool Reconnect()
    {
        if (!Disconnect())
            return false;

        return Connect();
    }

    /// <summary>
    /// Connect the client (asynchronous)
    /// </summary>
    /// <returns>'true' if the client was successfully connected, 'false' if the client failed to connect</returns>
    public virtual bool ConnectAsync()
    {
        if (IsConnected || IsConnecting)
            return false;

        // Setup buffers
        _receiveBuffer = new Buffer();
        _sendBufferMain = new Buffer();
        _sendBufferFlush = new Buffer();

        // Setup event args
        _connectEventArg = new SocketAsyncEventArgs();
        _connectEventArg.RemoteEndPoint = Proxy != null
            ? new IPEndPoint(IPAddress.Parse(Proxy.ProxyHost), Proxy.ProxyPort)
            : Endpoint;
        _connectEventArg.Completed += OnAsyncCompleted;
        _receiveEventArg = new SocketAsyncEventArgs();
        _receiveEventArg.Completed += OnAsyncCompleted;
        _sendEventArg = new SocketAsyncEventArgs();
        _sendEventArg.Completed += OnAsyncCompleted;

        Socket = CreateSocket();

        // Update the client socket disposed flag
        IsSocketDisposed = false;

        // Apply the option: dual mode (this option must be applied before connecting)
        if (Socket.AddressFamily == AddressFamily.InterNetworkV6)
            Socket.DualMode = OptionDualMode;

        // Update the connecting flag
        IsConnecting = true;

        // Call the client connecting handler
        OnConnecting();

        // Create a new client socket
        if (!Socket.ConnectAsync(_connectEventArg))
            ProcessConnect(_connectEventArg);

        return true;
    }

    /// <summary>
    /// Disconnect the client (asynchronous)
    /// </summary>
    /// <returns>'true' if the client was successfully disconnected, 'false' if the client is already disconnected</returns>
    public virtual bool DisconnectAsync() => Disconnect();

    /// <summary>
    /// Reconnect the client (asynchronous)
    /// </summary>
    /// <returns>'true' if the client was successfully reconnected, 'false' if the client is already reconnected</returns>
    public virtual bool ReconnectAsync()
    {
        if (!DisconnectAsync())
            return false;

        while (IsConnected)
            Thread.Yield();

        return ConnectAsync();
    }

    #endregion

    #region Send/Receive data

    // Receive buffer
    private bool _receiving;
    private Buffer _receiveBuffer;

    private SocketAsyncEventArgs _receiveEventArg;

    // Send buffer
    private readonly object _sendLock = new object();
    private bool _sending;
    private Buffer _sendBufferMain;
    private Buffer _sendBufferFlush;
    private SocketAsyncEventArgs _sendEventArg;
    private long _sendBufferFlushOffset;

    /// <summary>
    /// Send data to the server (synchronous)
    /// </summary>
    /// <param name="buffer">Buffer to send</param>
    /// <returns>Size of sent data</returns>
    public virtual long Send(byte[] buffer) => Send(buffer.AsSpan());

    /// <summary>
    /// Send data to the server (synchronous)
    /// </summary>
    /// <param name="buffer">Buffer to send</param>
    /// <param name="offset">Buffer offset</param>
    /// <param name="size">Buffer size</param>
    /// <returns>Size of sent data</returns>
    public virtual long Send(byte[] buffer, long offset, long size) => Send(buffer.AsSpan((int)offset, (int)size));

    /// <summary>
    /// Send data to the server (synchronous)
    /// </summary>
    /// <param name="buffer">Buffer to send as a span of bytes</param>
    /// <returns>Size of sent data</returns>
    public virtual long Send(ReadOnlySpan<byte> buffer)
    {
        try
        {
            if (!IsConnected)
                return 0;

            if (buffer.IsEmpty)
                return 0;

            // Sent data to the server
            long sent = Socket.Send(buffer, SocketFlags.None, out SocketError ec);
            if (sent > 0)
            {
                // Update statistic
                BytesSent += sent;

                // Call the buffer sent handler
                OnSent(sent, BytesPending + BytesSending);
            }

            // Check for socket error
            if (ec != SocketError.Success)
            {
                SendError(ec);
                Disconnect();
            }

            return sent;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending data to the server");
            return 0;
        }
    }

    /// <summary>
    /// Send text to the server (synchronous)
    /// </summary>
    /// <param name="text">Text string to send</param>
    /// <returns>Size of sent text</returns>
    public virtual long Send(string text) => Send(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// Send text to the server (synchronous)
    /// </summary>
    /// <param name="text">Text to send as a span of characters</param>
    /// <returns>Size of sent text</returns>
    public virtual long Send(ReadOnlySpan<char> text) => Send(Encoding.UTF8.GetBytes(text.ToArray()));

    /// <summary>
    /// Send data to the server (asynchronous)
    /// </summary>
    /// <param name="buffer">Buffer to send</param>
    /// <returns>'true' if the data was successfully sent, 'false' if the client is not connected</returns>
    public virtual bool SendAsync(byte[] buffer) => SendAsync(buffer.AsSpan());

    /// <summary>
    /// Send data to the server (asynchronous)
    /// </summary>
    /// <param name="buffer">Buffer to send</param>
    /// <param name="offset">Buffer offset</param>
    /// <param name="size">Buffer size</param>
    /// <returns>'true' if the data was successfully sent, 'false' if the client is not connected</returns>
    public virtual bool SendAsync(byte[] buffer, long offset, long size) =>
        SendAsync(buffer.AsSpan((int)offset, (int)size));

    /// <summary>
    /// Send data to the server (asynchronous)
    /// </summary>
    /// <param name="buffer">Buffer to send as a span of bytes</param>
    /// <returns>'true' if the data was successfully sent, 'false' if the client is not connected</returns>
    public virtual bool SendAsync(ReadOnlySpan<byte> buffer)
    {
        if (!IsConnected)
            return false;

        if (buffer.IsEmpty)
            return true;

        lock (_sendLock)
        {
            // Check the send buffer limit
            if (((_sendBufferMain.Size + buffer.Length) > OptionSendBufferLimit) && (OptionSendBufferLimit > 0))
            {
                SendError(SocketError.NoBufferSpaceAvailable);
                return false;
            }

            // Fill the main send buffer
            _sendBufferMain.Append(buffer);

            // Update statistic
            BytesPending = _sendBufferMain.Size;

            // Avoid multiple send handlers
            if (_sending)
                return true;
            else
                _sending = true;

            // Try to send the main buffer
            TrySend();
        }

        return true;
    }

    /// <summary>
    /// Send text to the server (asynchronous)
    /// </summary>
    /// <param name="text">Text string to send</param>
    /// <returns>'true' if the text was successfully sent, 'false' if the client is not connected</returns>
    public virtual bool SendAsync(string text) => SendAsync(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// Send text to the server (asynchronous)
    /// </summary>
    /// <param name="text">Text to send as a span of characters</param>
    /// <returns>'true' if the text was successfully sent, 'false' if the client is not connected</returns>
    public virtual bool SendAsync(ReadOnlySpan<char> text) => SendAsync(Encoding.UTF8.GetBytes(text.ToArray()));

    /// <summary>
    /// Receive data from the server (synchronous)
    /// </summary>
    /// <param name="buffer">Buffer to receive</param>
    /// <returns>Size of received data</returns>
    public virtual long Receive(byte[] buffer) { return Receive(buffer, 0, buffer.Length); }

    /// <summary>
    /// Receive data from the server (synchronous)
    /// </summary>
    /// <param name="buffer">Buffer to receive</param>
    /// <param name="offset">Buffer offset</param>
    /// <param name="size">Buffer size</param>
    /// <returns>Size of received data</returns>
    public virtual long Receive(byte[] buffer, long offset, long size)
    {
        if (!IsConnected)
            return 0;

        if (size == 0)
            return 0;

        // Receive data from the server
        long received = Socket.Receive(buffer, (int)offset, (int)size, SocketFlags.None, out SocketError ec);
        if (received > 0)
        {
            // Update statistic
            BytesReceived += received;

            // Call the buffer received handler
            OnInternalReceived(buffer, 0, received);

            if (ProxyConnected || Proxy == null)
                OnReceived(buffer, 0, received);
        }

        // Check for socket error
        if (ec != SocketError.Success)
        {
            SendError(ec);
            Disconnect();
        }

        return received;
    }

    private void OnInternalReceived(byte[] buffer, int offset, long received)
    {
        if(Proxy == null)
            return;
        
        if(ProxyConnected)
            return;
        
        var data = buffer[offset..(offset + (int)received)];
        
        if(ProxyState == ProxyStateAuth.Header)
        {
            ReceiveProxyAuth(data);
        }
        else if (ProxyState == ProxyStateAuth.AuthType)
        {
            ReceiveProxyAuthResult(data);
        }
        else if (ProxyState == ProxyStateAuth.Auth)
        {
            ReceiveProxyServer(data);
        }

    }

    private void ReceiveProxyServer(byte[] data)
    {
        /*
         +---- +-----+-------+------+----------+----------+
        | VER | REP | RSV   | ATYP | BND.ADDR | BND.PORT |
        +-----+-----+-------+------+----------+----------+
        | 1   | 1   | X'00' | 1    | Variable | 2        |
        +-----+-----+-------+------+----------+----------+
        */
        if (data[1] != Socks5Constants.CmdReplySucceeded)
        {
            Socket.Close();
            throw new Exception("Failed to connect to the proxy destination.");
        }
        
        ProxyConnected = true;
        ProxyState = ProxyStateAuth.Server;
        
        OnConnected();
    }

    /// <summary>
    /// Receive text from the server (synchronous)
    /// </summary>
    /// <param name="size">Text size to receive</param>
    /// <returns>Received text</returns>
    public virtual string Receive(long size)
    {
        var buffer = new byte[size];
        var length = Receive(buffer);
        return Encoding.UTF8.GetString(buffer, 0, (int)length);
    }

    /// <summary>
    /// Receive data from the server (asynchronous)
    /// </summary>
    public virtual void ReceiveAsync()
    {
        // Try to receive data from the server
        TryReceive();
    }

    /// <summary>
    /// Try to receive new data
    /// </summary>
    private void TryReceive()
    {
        if (_receiving)
            return;

        if (!IsConnected)
            return;

        bool process = true;

        while (process)
        {
            process = false;

            try
            {
                // Async receive with the receive handler
                _receiving = true;
                _receiveEventArg.SetBuffer(_receiveBuffer.Data, 0, (int)_receiveBuffer.Capacity);
                if (!Socket.ReceiveAsync(_receiveEventArg))
                    process = ProcessReceive(_receiveEventArg);
            }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// Try to send pending data
    /// </summary>
    private void TrySend()
    {
        if (!IsConnected)
            return;

        bool empty = false;
        bool process = true;

        while (process)
        {
            process = false;

            lock (_sendLock)
            {
                // Is previous socket send in progress?
                if (_sendBufferFlush.IsEmpty)
                {
                    // Swap flush and main buffers
                    _sendBufferFlush = Interlocked.Exchange(ref _sendBufferMain, _sendBufferFlush);
                    _sendBufferFlushOffset = 0;

                    // Update statistic
                    BytesPending = 0;
                    BytesSending += _sendBufferFlush.Size;

                    // Check if the flush buffer is empty
                    if (_sendBufferFlush.IsEmpty)
                    {
                        // Need to call empty send buffer handler
                        empty = true;

                        // End sending process
                        _sending = false;
                    }
                }
                else
                    return;
            }

            // Call the empty send buffer handler
            if (empty)
            {
                OnEmpty();
                return;
            }

            try
            {
                // Async write with the write handler
                _sendEventArg.SetBuffer(_sendBufferFlush.Data,
                                        (int)_sendBufferFlushOffset,
                                        (int)(_sendBufferFlush.Size - _sendBufferFlushOffset));
                if (!Socket.SendAsync(_sendEventArg))
                    process = ProcessSend(_sendEventArg);
            }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// Clear send/receive buffers
    /// </summary>
    private void ClearBuffers()
    {
        lock (_sendLock)
        {
            // Clear send buffers
            _sendBufferMain.Clear();
            _sendBufferFlush.Clear();
            _sendBufferFlushOffset = 0;

            // Update statistic
            BytesPending = 0;
            BytesSending = 0;
        }
    }

    #endregion

    #region IO processing

    /// <summary>
    /// This method is called whenever a receive or send operation is completed on a socket
    /// </summary>
    private void OnAsyncCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (IsSocketDisposed)
            return;

        // Determine which type of operation just completed and call the associated handler
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Connect:
                ProcessConnect(e);
                break;
            case SocketAsyncOperation.Receive:
                if (ProcessReceive(e))
                    TryReceive();
                break;
            case SocketAsyncOperation.Send:
                if (ProcessSend(e))
                    TrySend();
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a receive or send");
        }
    }

    /// <summary>
    /// This method is invoked when an asynchronous connect operation completes
    /// </summary>
    private void ProcessConnect(SocketAsyncEventArgs e)
    {
        IsConnecting = false;

        if (e.SocketError == SocketError.Success)
        {
            // Apply the option: keep alive
            if (OptionKeepAlive)
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            if (OptionTcpKeepAliveTime >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp,
                                       SocketOptionName.TcpKeepAliveTime,
                                       OptionTcpKeepAliveTime);
            if (OptionTcpKeepAliveInterval >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp,
                                       SocketOptionName.TcpKeepAliveInterval,
                                       OptionTcpKeepAliveInterval);
            if (OptionTcpKeepAliveRetryCount >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp,
                                       SocketOptionName.TcpKeepAliveRetryCount,
                                       OptionTcpKeepAliveRetryCount);
            // Apply the option: no delay
            if (OptionNoDelay)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            // Prepare receive & send buffers
            _receiveBuffer.Reserve(OptionReceiveBufferSize);
            _sendBufferMain.Reserve(OptionSendBufferSize);
            _sendBufferFlush.Reserve(OptionSendBufferSize);

            // Reset statistic
            BytesPending = 0;
            BytesSending = 0;
            BytesSent = 0;
            BytesReceived = 0;

            // Update the connected flag
            IsConnected = true;
            
            OnInternalConnected();

            // Try to receive something from the server
            TryReceive();

            // Check the socket disposed state: in some rare cases it might be disconnected while receiving!
            if (IsSocketDisposed)
                return;

            // Call the client connected handler

            if (ProxyConnected || Proxy == null)
            {
                OnConnected();
            }

            // Call the empty send buffer handler
            if (_sendBufferMain.IsEmpty)
                OnEmpty();
        }
        else
        {
            // Call the client disconnected handler
            SendError(e.SocketError);
            OnDisconnected();
        }
    }

    public enum ProxyStateAuth
    {
        Header,
        AuthType,
        Auth,
        Server
    }
    
    public ProxyStateAuth ProxyState { get; set; }
    

    private void OnInternalConnected()
    {
        if(Proxy == null)
            return;
        
        SendProxyHeader();
        
        /*
        +-----+-----+-------+------+----------+----------+
        | VER | CMD | RSV   | ATYP | DST.ADDR | DST.PORT |
        +--- -+-----+-------+------+----------+----------+
        | 1   | 1   | X'00' | 1    | Variable | 2        |
        +-----+-----+-------+------+----------+----------+
        */

       /* var addressType = GetDestAddressType(Proxy.DestinationHost);
        var destAddr = GetDestAddressBytes(addressType, Proxy.DestinationHost);
        var destPort = GetDestPortBytes(Proxy.DestinationPort);

        var buffer = new byte[6 + Proxy.DestinationHost.Length];
        buffer[0] = 5;
        buffer[1] = Socks5Constants.CmdConnect;
        buffer[2] = Socks5Constants.Reserved;
        buffer[3] = addressType;
        destAddr.CopyTo(buffer, 4);
        destPort.CopyTo(buffer, 4 + destAddr.Length);

        Socket.SendAsync(buffer, SocketFlags.None);

        /*
        +---- +-----+-------+------+----------+----------+
        | VER | REP | RSV   | ATYP | BND.ADDR | BND.PORT |
        +-----+-----+-------+------+----------+----------+
        | 1   | 1   | X'00' | 1    | Variable | 2        |
        +-----+-----+-------+------+----------+----------+
        */

       /* var response = new byte[255];
        Socket.Receive(response, SocketFlags.None);

        if (response[1] != Socks5Constants.CmdReplySucceeded)
        {
            Socket.Close();
            throw new Exception("Failed to connect to the proxy destination.");
        }*/

    }

    private void ReceiveProxyAuthResult(byte[] response)
    {
        /*
        +----+--------+
        |VER | STATUS |
        +----+--------+
        | 1  |   1    |
        +----+--------+
        */
        if (response[1] != 0)
        {
            Socket.Close();
            throw new Exception("Proxy authentication failed.");
        }
        
        ProxyState = ProxyStateAuth.Auth;
        
        /*
         +-----+-----+-------+------+----------+----------+
        | VER | CMD | RSV   | ATYP | DST.ADDR | DST.PORT |
        +--- -+-----+-------+------+----------+----------+
        | 1   | 1   | X'00' | 1    | Variable | 2        |
        +-----+-----+-------+------+----------+----------+
        */
        
        var addressType = GetDestAddressType(Proxy.DestinationHost);
        var destAddr = GetDestAddressBytes(addressType, Proxy.DestinationHost);
        var destPort = GetDestPortBytes(Proxy.DestinationPort);

        var buffer = new byte[4 + destAddr.Length + destPort.Length];
        buffer[0] = 5;
        buffer[1] = Socks5Constants.CmdConnect;
        buffer[2] = Socks5Constants.Reserved;
        buffer[3] = addressType;
        destAddr.CopyTo(buffer, 4);
        destPort.CopyTo(buffer, 4 + destAddr.Length);

        SendAsync(buffer);
    }

    private void SendProxyHeader()
    {
        /*
        +----+----------+----------+
        | VER | NMETHODS | METHODS |
        +----+----------+----------+
        | 1  | 1        | 1 to 255 |
        +----+----------+----------+
         */

        var buffer = new byte[]
        {
            5,
            2,
            Socks5Constants.AuthMethodNoAuthenticationRequired, Socks5Constants.AuthMethodUsernamePassword
        };
        
        ProxyState = ProxyStateAuth.Header;
        SendAsync(buffer);
    }

    private bool ReceiveProxyAuth(byte[] response)
    {
        /*
        +-----+--------+
        | VER | METHOD |
        +-----+--------+
        | 1   | 1      |
        +-----+--------+
        */
        
        if(Proxy == null)
            return false;
        
        if (response.Length != 2)
            throw new Exception($"Failed to select an authentication method, the server sent {response.Length} bytes.");

        if (response[1] == Socks5Constants.AuthMethodReplyNoAcceptableMethods)
        {
            Socket.Close();
            throw new Exception(
                "The proxy destination does not accept the supported proxy client authentication methods.");
        }

        if (response[1] == Socks5Constants.AuthMethodUsernamePassword && string.IsNullOrEmpty(Proxy.Credentials.Username))
        {
            Socket.Close();
            throw new Exception("The proxy destination requires a username and password for authentication.");
        }

        if (response[1] == Socks5Constants.AuthMethodNoAuthenticationRequired)
            return true;

        /*
        +-----+------+----------+------+----------+
        | VER | ULEN | UNAME    | PLEN | PASSWD   |
        +----+-------+----------+------+----------+
        | 1  | 1     | 1 to 255 | 1    | 1 to 255 |
        +----+-------+----------+------+----------+
        */
        var buffer = ConstructAuthBuffer(Proxy.Credentials.Username, Proxy.Credentials.Password);
        SendAsync(buffer);

        ProxyState = ProxyStateAuth.AuthType;
        return false;
    }

    private static byte GetDestAddressType(string host)
    {
        if (!IPAddress.TryParse(host, out var ipAddr))
            return Socks5Constants.AddrtypeDomainName;

        switch (ipAddr.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                return Socks5Constants.AddrtypeIpv4;
            case AddressFamily.InterNetworkV6:
                return Socks5Constants.AddrtypeIpv6;
            default:
                throw new Exception(
                    string.Format("The host addess {0} of type '{1}' is not a supported address type.\n" +
                                  "The supported types are InterNetwork and InterNetworkV6.",
                                  host,
                                  Enum.GetName(typeof(AddressFamily), ipAddr.AddressFamily)));
        }
    }

    private static byte[] GetDestAddressBytes(byte addressType, string host)
    {
        switch (addressType)
        {
            case Socks5Constants.AddrtypeIpv4:
            case Socks5Constants.AddrtypeIpv6:
                return IPAddress.Parse(host).GetAddressBytes();
            case Socks5Constants.AddrtypeDomainName:
                byte[] bytes = new byte[host.Length + 1];
                bytes[0] = Convert.ToByte(host.Length);
                Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);
                return bytes;
            default:
                return [];
        }
    }

    private static byte[] GetDestPortBytes(int value)
    {
        return
        [
            Convert.ToByte(value / 256),
            Convert.ToByte(value % 256)
        ];
    }

    
    private static byte[] ConstructAuthBuffer(string username, string password)
    {
        var credentials = new byte[3 + username.Length + password.Length];

        credentials[0] = 0x01;
        credentials[1] = (byte)Encoding.ASCII.GetByteCount(username);
        Array.Copy(Encoding.ASCII.GetBytes(username), 0, credentials, 2, credentials[1]);
        credentials[credentials[1] + 2] = (byte)Encoding.ASCII.GetByteCount(password);
        Array.Copy(Encoding.ASCII.GetBytes(password), 0, credentials, 3 + credentials[1], credentials[credentials[1] + 2]);

        return credentials;
    }


    /// <summary>
    /// This method is invoked when an asynchronous receive operation completes
    /// </summary>
    private bool ProcessReceive(SocketAsyncEventArgs e)
    {
        if (!IsConnected)
            return false;

        try
        {
            long size = e.BytesTransferred;

            // Received some data from the server
            if (size > 0)
            {
                // Update statistic
                BytesReceived += size;

                // Call the buffer received handler
                var proxyConnected = ProxyConnected;
                
                OnInternalReceived(_receiveBuffer.Data, 0, size);

                if (proxyConnected || Proxy == null)
                    OnReceived(_receiveBuffer.Data, 0, size);

                // If the receive buffer is full increase its size
                if (_receiveBuffer.Capacity == size)
                {
                    // Check the receive buffer limit
                    if (((2 * size) > OptionReceiveBufferLimit) && (OptionReceiveBufferLimit > 0))
                    {
                        SendError(SocketError.NoBufferSpaceAvailable);
                        DisconnectAsync();
                        return false;
                    }

                    _receiveBuffer.Reserve(2 * size);
                }
            }

            _receiving = false;

            // Try to receive again if the client is valid
            if (e.SocketError == SocketError.Success)
            {
                // If zero is returned from a read operation, the remote end has closed the connection
                if (size > 0)
                    return true;
                else
                    DisconnectAsync();
            }
            else
            {
                SendError(e.SocketError);
                DisconnectAsync();
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing receive operation");
            return false;
        }
    }

    /// <summary>
    /// This method is invoked when an asynchronous send operation completes
    /// </summary>
    private bool ProcessSend(SocketAsyncEventArgs e)
    {
        if (!IsConnected)
            return false;

        long size = e.BytesTransferred;

        // Send some data to the server
        if (size > 0)
        {
            // Update statistic
            BytesSending -= size;
            BytesSent += size;

            // Increase the flush buffer offset
            _sendBufferFlushOffset += size;

            // Successfully send the whole flush buffer
            if (_sendBufferFlushOffset == _sendBufferFlush.Size)
            {
                // Clear the flush buffer
                _sendBufferFlush.Clear();
                _sendBufferFlushOffset = 0;
            }

            // Call the buffer sent handler
            OnSent(size, BytesPending + BytesSending);
        }

        // Try to send again if the client is valid
        if (e.SocketError == SocketError.Success)
            return true;
        else
        {
            SendError(e.SocketError);
            DisconnectAsync();
            return false;
        }
    }

    #endregion

    #region Session handlers

    /// <summary>
    /// Handle client connecting notification
    /// </summary>
    protected virtual void OnConnecting() { }

    /// <summary>
    /// Handle client connected notification
    /// </summary>
    protected virtual void OnConnected() { }

    /// <summary>
    /// Handle client disconnecting notification
    /// </summary>
    protected virtual void OnDisconnecting() { }

    /// <summary>
    /// Handle client disconnected notification
    /// </summary>
    protected virtual void OnDisconnected() { }

    /// <summary>
    /// Handle buffer received notification
    /// </summary>
    /// <param name="buffer">Received buffer</param>
    /// <param name="offset">Received buffer offset</param>
    /// <param name="size">Received buffer size</param>
    /// <remarks>
    /// Notification is called when another part of buffer was received from the server
    /// </remarks>
    protected virtual void OnReceived(byte[] buffer, long offset, long size) { }

    /// <summary>
    /// Handle buffer sent notification
    /// </summary>
    /// <param name="sent">Size of sent buffer</param>
    /// <param name="pending">Size of pending buffer</param>
    /// <remarks>
    /// Notification is called when another part of buffer was sent to the server.
    /// This handler could be used to send another buffer to the server for instance when the pending size is zero.
    /// </remarks>
    protected virtual void OnSent(long sent, long pending) { }

    /// <summary>
    /// Handle empty send buffer notification
    /// </summary>
    /// <remarks>
    /// Notification is called when the send buffer is empty and ready for a new data to send.
    /// This handler could be used to send another buffer to the server.
    /// </remarks>
    protected virtual void OnEmpty() { }

    /// <summary>
    /// Handle error notification
    /// </summary>
    /// <param name="error">Socket error code</param>
    protected virtual void OnError(SocketError error) { }

    #endregion

    #region Error handling

    /// <summary>
    /// Send error notification
    /// </summary>
    /// <param name="error">Socket error code</param>
    private void SendError(SocketError error)
    {
        // Skip disconnect errors
        if ((error == SocketError.ConnectionAborted) ||
            (error == SocketError.ConnectionRefused) ||
            (error == SocketError.ConnectionReset) ||
            (error == SocketError.OperationAborted) ||
            (error == SocketError.Shutdown))
            return;

        OnError(error);
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Disposed flag
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Client socket disposed flag
    /// </summary>
    public bool IsSocketDisposed { get; private set; } = true;

    // Implement IDisposable.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposingManagedResources)
    {
        // The idea here is that Dispose(Boolean) knows whether it is
        // being called to do explicit cleanup (the Boolean is true)
        // versus being called due to a garbage collection (the Boolean
        // is false). This distinction is useful because, when being
        // disposed explicitly, the Dispose(Boolean) method can safely
        // execute code using reference type fields that refer to other
        // objects knowing for sure that these other objects have not been
        // finalized or disposed of yet. When the Boolean is false,
        // the Dispose(Boolean) method should not execute code that
        // refer to reference type fields because those objects may
        // have already been finalized."

        if (!IsDisposed)
        {
            if (disposingManagedResources)
            {
                // Dispose managed resources here...
                DisconnectAsync();
            }

            // Dispose unmanaged resources here...

            // Set large fields to null here...

            // Mark as disposed.
            IsDisposed = true;
        }
    }

    #endregion
}