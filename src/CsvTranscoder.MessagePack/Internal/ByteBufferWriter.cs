using System.Buffers;

namespace AndanteTribe.Csv.Internal;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> backed by a buffer rented from <see cref="ArrayPool{T}.Shared"/>.
/// Dispose to return the rented buffer to the pool.
/// </summary>
internal sealed class ByteBufferWriter : IBufferWriter<byte>, IDisposable
{
    private const int MinimumBufferSize = 65536;

    private byte[]? _buffer;
    private int _index;

    public int WrittenCount => _index;

    public ReadOnlySpan<byte> WrittenSpan => WrittenMemory.Span;

    public ReadOnlyMemory<byte> WrittenMemory => _buffer?.AsMemory(0, _index) ?? throw new ObjectDisposedException(nameof(ByteBufferWriter));

    public ByteBufferWriter()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(MinimumBufferSize);
        _index = 0;
    }

    public void Advance(int count)
    {
        if (_buffer is null)
        {
            throw new ObjectDisposedException(nameof(ByteBufferWriter));
        }
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_buffer is null)
        {
            throw new ObjectDisposedException(nameof(ByteBufferWriter));
        }
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_index);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_buffer is null)
        {
            throw new ObjectDisposedException(nameof(ByteBufferWriter));
        }
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_index);
    }

    private void EnsureCapacity(int sizeHint)
    {
        var needed = _index + Math.Max(sizeHint, 1);
        if (needed <= _buffer!.Length)
        {
            return;
        }

        var newSize = Math.Max(needed, _buffer.Length * 2);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _index).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer is null)
        {
            return;
        }

        _buffer = null;
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
