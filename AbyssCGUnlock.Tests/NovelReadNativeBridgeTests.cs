using System.Runtime.InteropServices;
using AbyssCGUnlock.Patches;
using Xunit;

namespace AbyssCGUnlock.Tests;

public sealed class NovelReadNativeBridgeTests
{
    [Theory]
    [InlineData(true, 0, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 2, false)]
    [InlineData(true, 3, true)]
    [InlineData(true, 4, false)]
    [InlineData(true, 5, true)]
    [InlineData(true, 6, false)]
    [InlineData(true, 7, false)]
    [InlineData(true, 999, false)]
    [InlineData(false, 3, false)]
    [InlineData(false, 5, false)]
    public void Only_enabled_character_and_skin_stories_use_local_responses(bool enabled, int type, bool expected)
    {
        Assert.Equal(expected, NovelReadBypassPatch.ShouldBypass(enabled, type));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x12345678)]
    [InlineData(0x1234567887654321L)]
    public void Passthrough_preserves_inline_token_and_all_native_arguments(long token)
    {
        var calls = 0;
        var returned = new IntPtr(0x87654321);
        var errors = new List<Exception>();
        var bridge = new NovelReadNativeBridge((_, _) => default, errors.Add);
        bridge.Original = (buffer, type, id, source, info) =>
        {
            calls++;
            Assert.Equal(new IntPtr(0x11223344), buffer);
            Assert.Equal(2, type);
            Assert.Equal(10300040201L, id);
            Assert.Equal(new IntPtr(token), source);
            Assert.Equal(new IntPtr(0x55667788), info);
            return returned;
        };

        var result = bridge.Callback(new IntPtr(0x11223344), 2, 10300040201L,
            new IntPtr(token), new IntPtr(0x55667788));

        Assert.Equal(returned, result);
        Assert.Equal(1, calls);
        Assert.Empty(errors);
    }

    [Fact]
    public void Local_result_writes_only_the_24_byte_return_buffer_and_skips_original()
    {
        var payload = Enumerable.Range(1, 24).Select(value => (byte)value).ToArray();
        var errors = new List<Exception>();
        var bridge = new NovelReadNativeBridge((type, id) =>
        {
            Assert.Equal(3, type);
            Assert.Equal(42L, id);
            return (payload, new object());
        }, errors.Add);
        bridge.Original = (_, _, _, _, _) => throw new InvalidOperationException("Original must not run");
        var buffer = Marshal.AllocHGlobal(40);
        try
        {
            Marshal.Copy(Enumerable.Repeat((byte)0xCC, 40).ToArray(), 0, buffer, 40);

            Assert.Equal(buffer, bridge.Callback(buffer, 3, 42, new IntPtr(0x12345678), IntPtr.Zero));

            var actual = new byte[40];
            Marshal.Copy(buffer, actual, 0, actual.Length);
            Assert.Equal(payload, actual.Take(24));
            Assert.All(actual.Skip(24), value => Assert.Equal(0xCC, value));
            Assert.Empty(errors);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    [InlineData(25)]
    public void Invalid_payload_size_falls_back_without_writing_the_buffer(int size)
    {
        var calls = 0;
        var errors = new List<Exception>();
        var bridge = new NovelReadNativeBridge((_, _) => (new byte[size], new object()), errors.Add);
        bridge.Original = (buffer, _, _, _, _) => { calls++; return buffer; };
        var buffer = Marshal.AllocHGlobal(40);
        try
        {
            var sentinel = Enumerable.Repeat((byte)0xCC, 40).ToArray();
            Marshal.Copy(sentinel, 0, buffer, sentinel.Length);
            Assert.Equal(buffer, bridge.Callback(buffer, 3, 42, IntPtr.Zero, IntPtr.Zero));
            var actual = new byte[40];
            Marshal.Copy(buffer, actual, 0, actual.Length);
            Assert.Equal(sentinel, actual);
            Assert.Equal(1, calls);
            Assert.IsType<InvalidOperationException>(Assert.Single(errors));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Failed_local_response_falls_back_once_without_touching_return_buffer(bool loggerFails)
    {
        var errors = new List<Exception>();
        var calls = 0;
        var failure = new InvalidOperationException("Response construction failed");
        var bridge = new NovelReadNativeBridge((_, _) => throw failure, exception =>
        {
            errors.Add(exception);
            if (loggerFails)
                throw new InvalidOperationException("Logging failed");
        });
        bridge.Original = (buffer, type, id, token, info) =>
        {
            calls++;
            Assert.Equal(5, type);
            Assert.Equal(42L, id);
            Assert.Equal(new IntPtr(0x12345678), token);
            Assert.Equal(IntPtr.Zero, info);
            return buffer;
        };
        var buffer = Marshal.AllocHGlobal(24);
        try
        {
            var sentinel = Enumerable.Repeat((byte)0xCC, 24).ToArray();
            Marshal.Copy(sentinel, 0, buffer, 24);
            Assert.Equal(buffer, bridge.Callback(buffer, 5, 42, new IntPtr(0x12345678), IntPtr.Zero));
            var actual = new byte[24];
            Marshal.Copy(buffer, actual, 0, actual.Length);
            Assert.Equal(sentinel, actual);
            Assert.Equal(1, calls);
            Assert.Same(failure, Assert.Single(errors));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
