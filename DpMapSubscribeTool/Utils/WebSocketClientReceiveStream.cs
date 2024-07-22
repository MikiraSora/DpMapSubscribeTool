using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Utils;

public class WebSocketClientReceiveStream : Stream
{
    private readonly ClientWebSocket webSocket;

    public WebSocketClientReceiveStream(ClientWebSocket webSocket)
    {
        this.webSocket = webSocket;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var slice = buffer.AsMemory().Slice(offset, count);
        var result = await webSocket.ReceiveAsync(slice, cancellationToken);
        return result.Count;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, default).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}