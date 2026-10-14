using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Startup;
using Project.Api;
using Project.Master;

namespace AbyssCGUnlock.Patches;

internal static class NovelReadBypassPatch
{
    private static IDetour _detour = null!;
    private static NovelReadNativeBridge _bridge = null!;

    internal static MethodBase TargetMethod() => AccessTools.Method(
        typeof(NovelApiDataStore),
        nameof(NovelApiDataStore.RequestAsync),
        new[] { typeof(NovelType), typeof(long), typeof(Il2CppSystem.Threading.CancellationToken) });

    internal static bool ShouldBypass(bool enabled, int novelType) => enabled &&
        (novelType == (int)NovelType.Character || novelType == (int)NovelType.CharacterSkin);

    internal static unsafe void Install(MethodBase target)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("NovelRead native bypass requires Windows x64.");

        uint alignment = 0;
        if (IL2CPP.il2cpp_class_value_size(
                Il2CppClassPointerStore<Il2CppSystem.Threading.CancellationToken>.NativeClassPtr, ref alignment) != 8 ||
            IL2CPP.il2cpp_class_value_size(
                Il2CppClassPointerStore<UniTask<NovelReadResponseEntity>>.NativeClassPtr, ref alignment) != NovelReadNativeBridge.ResultSize)
            throw new NotSupportedException("NovelRead native ABI changed: expected an 8-byte token and 24-byte UniTask.");

        var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(target)
            ?? throw new MissingFieldException("NovelRead native method metadata is unavailable.");
        var metadata = (IntPtr)(field.GetValue(null)
            ?? throw new InvalidOperationException("NovelRead native metadata field is null."));
        if (metadata == IntPtr.Zero)
            throw new InvalidOperationException("NovelRead native method metadata is null.");
        var methodPointer = UnityVersionHandler.Wrap((Il2CppMethodInfo*)metadata).MethodPointer;
        if (methodPointer == IntPtr.Zero)
            throw new InvalidOperationException("NovelRead native method pointer is null.");

        // HarmonySupport reboxes the inline token as an address; retain the native ABI instead.
        _bridge = new NovelReadNativeBridge(CreateLocalResult,
            exception => CgUnlockPlugin.LogSource.LogWarning("[CGUnlock] NovelRead local response failed; forwarding original: " + exception));
        _detour = Il2CppInteropRuntime.Instance.DetourProvider.Create(methodPointer, _bridge.Callback);
        _bridge.Original = _detour.GenerateTrampoline<NovelReadNativeBridge.RequestAsync>();
        _detour.Apply();
    }

    internal static void Dispose()
    {
        _detour?.Dispose();
        _detour = null!;
        _bridge = null!;
    }

    private static (byte[] Bytes, object Root) CreateLocalResult(int novelType, long novelId)
    {
        if (!ShouldBypass(PluginConfig.EnableNovelReadBypass.Value, novelType))
            return default;

        var fake = new NovelReadResponseEntity
        {
            read_rewards = new Il2CppReferenceArray<DropContentEntity>(0),
        };
        var task = UniTask.FromResult(fake);
        var result = new byte[NovelReadNativeBridge.ResultSize];
        try
        {
            var payload = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(task));
            Marshal.Copy(payload, result, 0, result.Length);
            CgUnlockPlugin.LogSource.LogInfo($"[CGUnlock] NovelRead API 已本地伪造成功响应 (novelType={(NovelType)novelType}, novelId={novelId})，跳过服务端校验 190103/190108");
            return (result, task);
        }
        finally
        {
            GC.KeepAlive(task);
            GC.KeepAlive(fake);
        }
    }
}
