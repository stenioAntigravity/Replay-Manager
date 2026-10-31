using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayManager.Infrastructure.YouTube;

/// <summary>
/// Stream wrapper que limita a taxa de transferência (bandwidth throttling) em bytes por segundo.
/// </summary>
public class ThrottledStream : Stream
{
    private readonly Stream _baseStream;
    private long _maxBytesPerSecond;
    private long _bytesTransferredInWindow;
    private long _windowStartTicks;

    public ThrottledStream(Stream baseStream, long maxBytesPerSecond = 0)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        _maxBytesPerSecond = maxBytesPerSecond;
        _windowStartTicks = Environment.TickCount64;
    }

    /// <summary>
    /// Limite máximo em bytes por segundo. Se menor ou igual a 0, sem limite (ilimitado).
    /// </summary>
    public long MaxBytesPerSecond
    {
        get => _maxBytesPerSecond;
        set => _maxBytesPerSecond = value;
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override void Flush() => _baseStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
    public override void SetLength(long value) => _baseStream.SetLength(value);

    public override int Read(byte[] buffer, int offset, int count)
    {
        Throttle(count);
        var bytesRead = _baseStream.Read(buffer, offset, count);
        RecordBytes(bytesRead);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await ThrottleAsync(count, cancellationToken);
        var bytesRead = await _baseStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        RecordBytes(bytesRead);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await ThrottleAsync(buffer.Length, cancellationToken);
        var bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken);
        RecordBytes(bytesRead);
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Throttle(count);
        _baseStream.Write(buffer, offset, count);
        RecordBytes(count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await ThrottleAsync(count, cancellationToken);
        await _baseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        RecordBytes(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await ThrottleAsync(buffer.Length, cancellationToken);
        await _baseStream.WriteAsync(buffer, cancellationToken);
        RecordBytes(buffer.Length);
    }

    private void Throttle(int count)
    {
        if (_maxBytesPerSecond <= 0) return;

        var now = Environment.TickCount64;
        var elapsedMs = now - _windowStartTicks;

        if (elapsedMs >= 1000)
        {
            _windowStartTicks = now;
            _bytesTransferredInWindow = 0;
            return;
        }

        if (_bytesTransferredInWindow + count > _maxBytesPerSecond)
        {
            var sleepMs = (int)(1000 - elapsedMs);
            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
                _windowStartTicks = Environment.TickCount64;
                _bytesTransferredInWindow = 0;
            }
        }
    }

    private async Task ThrottleAsync(int count, CancellationToken cancellationToken)
    {
        if (_maxBytesPerSecond <= 0) return;

        var now = Environment.TickCount64;
        var elapsedMs = now - _windowStartTicks;

        if (elapsedMs >= 1000)
        {
            _windowStartTicks = now;
            _bytesTransferredInWindow = 0;
            return;
        }

        if (_bytesTransferredInWindow + count > _maxBytesPerSecond)
        {
            var sleepMs = (int)(1000 - elapsedMs);
            if (sleepMs > 0)
            {
                await Task.Delay(sleepMs, cancellationToken);
                _windowStartTicks = Environment.TickCount64;
                _bytesTransferredInWindow = 0;
            }
        }
    }

    private void RecordBytes(int count)
    {
        if (count > 0)
        {
            Interlocked.Add(ref _bytesTransferredInWindow, count);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
