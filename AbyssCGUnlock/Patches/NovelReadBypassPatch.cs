using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Project.Api;
using Project.Master;

namespace AbyssCGUnlock.Patches;

/// <summary>
/// 服务端校验绕过（v0.2.0）。
///
/// 证据链（新鲜反编译，docs/rca-error-190103-190108.md）：
/// - 播放确认后客户端调用 Project.Api.NovelApiDataStore.RequestAsync(novelType, novelId, ct)
///   （public static，代理元数据 NativeMethodInfoPtr_RequestAsync_Public_Static_...），
///   服务端 NovelRead API 校验「皮肤持有/好感度」并返回错误码：
///     190108 = 未购皮肤（逆夜袭玩法套餐），190103 = 好感度不足（和老师一起的培训）。
/// - 客户端只负责展示：ApiErrorMessageBuilder.FormatServerErrorMessage(errorCode)
///   查 _serverErrorCodeMapInProduct，否则 String.Format("エラーが発生しました。\nError Code: {0}", errorCode)。
/// - 结论：这两个码是服务端返回；客户端 UI 补丁无法绕过，必须本地伪造成功响应。
///
/// 实现：Harmony prefix 跳过原方法，返回 UniTask.FromResult(空的 NovelReadResponseEntity)，
/// 仅对 NovelType.Character（好感度场景）与 NovelType.CharacterSkin（皮肤场景）生效，
/// 其它类型（Main/Event/Home/Prologue/Other）保持原样走服务端。
/// </summary>
internal static class NovelReadBypassPatch
{
    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(NovelApiDataStore),
            nameof(NovelApiDataStore.RequestAsync),
            new[] { typeof(NovelType), typeof(long), typeof(Il2CppSystem.Threading.CancellationToken) });
    }

    internal static bool Prefix(NovelType novelType, long mNovelId, ref Cysharp.Threading.Tasks.UniTask<NovelReadResponseEntity> __result)
    {
        if (!PluginConfig.EnableNovelReadBypass.Value)
        {
            return true;
        }

        if (novelType != NovelType.Character && novelType != NovelType.CharacterSkin)
        {
            return true;
        }

        // 空成功响应（read_rewards 置空数组，避免下游读字段时踩空）
        var fake = new NovelReadResponseEntity();
        fake.read_rewards = new Il2CppReferenceArray<DropContentEntity>(0);

        __result = Cysharp.Threading.Tasks.UniTask.FromResult(fake);
        CgUnlockPlugin.LogSource.LogInfo($"[CGUnlock] NovelRead API 已本地伪造成功响应 (novelType={novelType}, novelId={mNovelId})，跳过服务端校验 190103/190108");
        return false;
    }
}
