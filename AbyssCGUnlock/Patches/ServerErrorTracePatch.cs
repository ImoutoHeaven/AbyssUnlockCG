using System;
using System.Reflection;
using HarmonyLib;
using Project.Api;

namespace AbyssCGUnlock.Patches;

internal static class ServerErrorTracePatch
{
    private const int ServerBusyErrorCode = 150100;

    internal static MethodBase TargetMethod() => AccessTools.Method(
        typeof(ApiErrorMessageBuilder),
        nameof(ApiErrorMessageBuilder.FormatServerErrorMessage),
        new[] { typeof(int) });

    internal static void Prefix(int errorCode)
    {
        if (errorCode != ServerBusyErrorCode)
        {
            return;
        }

        CgUnlockPlugin.LogSource.LogWarning(
            $"[CGUnlock trace] ServerError {errorCode} formatted; stack:\n{Environment.StackTrace}");
    }
}
