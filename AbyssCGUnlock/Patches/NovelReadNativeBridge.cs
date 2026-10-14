using System;
using System.Runtime.InteropServices;

namespace AbyssCGUnlock.Patches;

internal sealed class NovelReadNativeBridge
{
    internal const int ResultSize = 24;

    // Windows x64: hidden UniTask result buffer, followed by the inline 8-byte token.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr RequestAsync(
        IntPtr resultBuffer, int novelType, long novelId, IntPtr tokenSource, IntPtr methodInfo);

    private readonly Func<int, long, (byte[] Bytes, object Root)> _createLocalResult;
    private readonly Action<Exception> _reportError;
    internal RequestAsync Original = null!;
    internal RequestAsync Callback { get; }

    internal NovelReadNativeBridge(Func<int, long, (byte[] Bytes, object Root)> createLocalResult, Action<Exception> reportError)
    {
        _createLocalResult = createLocalResult;
        _reportError = reportError;
        Callback = Invoke;
    }

    private IntPtr Invoke(IntPtr resultBuffer, int novelType, long novelId, IntPtr tokenSource, IntPtr methodInfo)
    {
        (byte[] Bytes, object Root) result = default;
        try
        {
            result = _createLocalResult(novelType, novelId);
            if (result.Bytes != null && result.Bytes.Length != ResultSize)
                throw new InvalidOperationException("Unexpected native NovelRead UniTask size.");
        }
        catch (Exception exception)
        {
            result = default;
            try
            {
                _reportError(exception);
            }
            catch
            {
                // Logging must not throw across the native callback boundary.
            }
        }

        if (result.Bytes != null)
        {
            try
            {
                Marshal.Copy(result.Bytes, 0, resultBuffer, result.Bytes.Length);
                return resultBuffer;
            }
            finally
            {
                GC.KeepAlive(result.Root);
            }
        }

        return Original(resultBuffer, novelType, novelId, tokenSource, methodInfo);
    }
}
