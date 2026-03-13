using System.Buffers;
using AndanteTribe.Csv.Internal;

namespace CsvTranscoder.MessagePack.Tests;

// ═══════════════════════════════════════════════════════════════════════
//  ByteBufferWriter — direct unit tests for internal class
// ═══════════════════════════════════════════════════════════════════════

public class ByteBufferWriterTests
{
    [Fact]
    public void WrittenCount_ReflectsAdvancedBytes()
    {
        using var writer = new ByteBufferWriter();
        Assert.Equal(0, writer.WrittenCount);

        var span = writer.GetSpan(4);
        span[0] = 1; span[1] = 2; span[2] = 3; span[3] = 4;
        writer.Advance(4);

        Assert.Equal(4, writer.WrittenCount);
    }

    [Fact]
    public void GetSpan_ReturnsWritableSpan()
    {
        using var writer = new ByteBufferWriter();
        var span = writer.GetSpan(3);
        span[0] = 10; span[1] = 20; span[2] = 30;
        writer.Advance(3);

        Assert.Equal(new byte[] { 10, 20, 30 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void GetSpan_ZeroSizeHint_ReturnsNonEmptySpan()
    {
        using var writer = new ByteBufferWriter();
        var span = writer.GetSpan(0);
        Assert.True(span.Length > 0);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // First Dispose sets _buffer to null; second Dispose hits the early-return branch
        // at line 78: `if (buffer is null) return;`.
        var writer = new ByteBufferWriter();
        writer.Dispose();
        writer.Dispose(); // must not throw
    }
}
