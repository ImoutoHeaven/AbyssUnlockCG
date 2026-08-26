using System.Text.RegularExpressions;
using Xunit;

namespace AbyssCGUnlock.Tests;

/// <summary>
/// RED：把已确认的 Harmony seam、动态数据源和纯本地请求边界固化为源码契约。
/// </summary>
public class LocalCharacterUnlockContractTests
{
    private static string Read(string relativePath) => File.ReadAllText(RepoRoot.SourceFile(relativePath));

    [Fact]
    public void 角色目录捕获_只消费游戏当前动态未持有列表()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterListLocalUnlockPatch.cs");

        Assert.Contains("nameof(CharacterListSubService.CreateCharacterThumbnailModels)", src);
        Assert.Contains("_nonHasCharaDataList", src);
        Assert.Contains("_userData", src);
        Assert.Contains("LocalCharacterRegistry.Replace", src);
    }

    [Fact]
    public void 队伍角色页_从下载Master与当前账号动态重建未持有目录()
    {
        var src = Read("AbyssCGUnlock/DynamicCharacterCatalog.cs");

        Assert.Contains("Engine.Get<MasterDataStore>()", src);
        Assert.Contains("Engine.Get<IServerTimeAccessor>()", src);
        Assert.Contains("GetCache<MCharacters>()", src);
        Assert.Contains("DateTimeExtensions.IsBetween", src);
        Assert.Contains("MCharaId", src);
        Assert.Contains("CharacterDataStore.CreateFromMaster", src);
        Assert.Contains("DynamicUnownedCharacterSelector.Select", src);
        Assert.Contains("LocalCharacterRegistry.Replace", src);
    }

    [Fact]
    public void 未持有角色交流数据_从Tavern卡片与默认工作皮肤交集动态填充Tavern皮肤ID()
    {
        var src = Read("AbyssCGUnlock/DynamicCharacterCatalog.cs");

        Assert.Contains("GetCache<MCharacterSkins>()", src);
        Assert.Contains("GetCache<MTavernCharacterCards>()", src);
        Assert.Contains("DynamicTavernCharacterSkinSelector.Select", src);
        Assert.Contains("tavernSkinIds.TryGetValue", src);
        Assert.Contains("_TavernMCharacterSkinId_k__BackingField = tavernSkinId", src);
        Assert.Contains("未持有角色交流皮肤Master映射已校正", src);
        Assert.Contains("未持有角色酒馆卡片Master兼容已启用", src);
        Assert.Contains("tavern_card_backed=true", src);
        Assert.DoesNotContain("DynamicDefaultCharacterSkinSelector.Select", src);
        Assert.False(File.Exists(RepoRoot.SourceFile("AbyssCGUnlock/DynamicDefaultCharacterSkinSelector.cs")));
        Assert.DoesNotMatch(new Regex(@"\b(?:1300014|101901)\b", RegexOptions.CultureInvariant), src);
    }

    [Fact]
    public void 队伍角色页注入_锁定实际OnRefresh状态机首次MoveNext并由Finalizer移除()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterTopLocalUnlockPatch.cs");

        Assert.Contains("typeof(CharacterTopRefreshStateMachine)", src);
        Assert.Contains("nameof(CharacterTopRefreshStateMachine.MoveNext)", src);
        Assert.Contains("__instance.__1__state != -1", src);
        Assert.Contains("DynamicCharacterCatalog.Refresh", src);
        Assert.Contains(".Add(", src);
        Assert.Contains(".Remove(", src);
        Assert.Contains("Finalizer", src);
        Assert.DoesNotContain("typeof(CharacterTopSubService)", src);
    }

    [Fact]
    public void 队伍角色页后续排序刷新_锁定RefreshActiveCharacterList批量注入窗口()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterTopListRefreshLocalUnlockPatch.cs");

        Assert.Contains("typeof(CharacterListWithFavoriteViewController)", src);
        Assert.Contains("nameof(CharacterListWithFavoriteViewController.RefreshActiveCharacterList)", src);
        Assert.Contains("LocalCharacterRegistry.Snapshot", src);
        Assert.Contains(".Add(", src);
        Assert.Contains(".Remove(", src);
        Assert.Contains("Finalizer", src);
    }

    [Fact]
    public void 未持有角色模型_在CreateFromUser接缝改走Master缩略图()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterModelLocalUnlockPatch.cs");

        Assert.Contains("typeof(CharacterModel)", src);
        Assert.Contains("nameof(CharacterModel.CreateFromUser)", src);
        Assert.Contains("typeof(AppDefine.IconSize)", src);
        Assert.Contains("LocalCharacterRegistry.TryGet", src);
        Assert.Contains("__result = CharacterModel.CreateFromMaster(character, size, ct)", src);
        Assert.Contains("return false", src);
        Assert.DoesNotMatch(new Regex(@"\.\s*Request\w*\s*\(", RegexOptions.CultureInvariant), src);
    }

    [Fact]
    public void 未持有点击_锁定实际订阅闭包并只本地转发角色ID()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterListLocalUnlockPatch.cs");

        Assert.Contains("typeof(CharacterListSubService.__c)", src);
        Assert.Contains("nameof(CharacterListSubService.__c._SetupCharacterListEvent_b__33_0)", src);
        Assert.Contains("_IsHasCharacter_k__BackingField", src);
        Assert.Contains("_resultSubject.OnNext", src);
    }

    [Fact]
    public void 详情页注入_只在UpdateView同步解析窗口临时Add并由Finalizer移除()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterDetailLocalUnlockPatch.cs");

        Assert.Contains("nameof(DetailSubService.UpdateView)", src);
        Assert.Contains("_CharaDataStore_k__BackingField", src);
        Assert.Contains("_dataList", src);
        Assert.Contains(".Add(", src);
        Assert.Contains(".Remove(", src);
        Assert.Contains("Finalizer", src);
    }

    [Fact]
    public void 主角色详情查询_注册表命中时由Prefix返回账号绑定合成数据并跳过会抛错的原生查询()
    {
        var src = Read("AbyssCGUnlock/Patches/CharacterDataStoreLocalLookupPatch.cs");
        var manager = Read("AbyssCGUnlock/Patches/PatchManager.cs");

        Assert.Contains("typeof(CharacterDataStore)", src);
        Assert.Contains("nameof(CharacterDataStore.GetByTableId)", src);
        Assert.Contains("new[] { typeof(long) }", src);
        Assert.Contains("internal static bool Prefix(", src);
        Assert.Contains("ref CharacterData? __result", src);
        Assert.Contains("IL2CPP.Il2CppObjectBaseToPtr(activeStore)", src);
        Assert.Contains("IL2CPP.Il2CppObjectBaseToPtr(__instance)", src);
        Assert.Contains("LocalCharacterRegistry.TryGet", src);
        Assert.Contains("__result = character", src);
        Assert.Contains("return false;", src);
        Assert.Contains("return true;", src);
        Assert.DoesNotContain("Postfix", src);
        Assert.DoesNotContain("__result != null", src);
        Assert.Contains("prefix: new HarmonyMethod", manager);
        Assert.Contains("nameof(CharacterDataStoreLocalLookupPatch.Prefix)", manager);
        Assert.DoesNotContain("UpdateCurrentCharacterModel", src);
        Assert.DoesNotContain("Il2CppSystem.Nullable", src);
        Assert.DoesNotContain("TemporaryCharacterDataInjection", src);
    }

    [Fact]
    public void 未持有角色交流缩略图_账号目录命中时从UserData加载器改走同签名Master加载器()
    {
        var src = Read("AbyssCGUnlock/Patches/InteractionThumbnailLocalUnlockPatch.cs");
        var manager = Read("AbyssCGUnlock/Patches/PatchManager.cs");

        Assert.Contains("typeof(CharacterThumbnailLoader)", src);
        Assert.Contains("nameof(CharacterThumbnailLoader.LoadThumbnailFromUserDataAsync)", src);
        Assert.Contains("typeof(AppDefine.IconSize)", src);
        Assert.Contains("typeof(long)", src);
        Assert.Contains("typeof(SkinType)", src);
        Assert.Contains("typeof(CacheType)", src);
        Assert.Contains("CharacterThumbnailLoader __instance", src);
        Assert.Contains("LocalCharacterRegistry.TryGet", src);
        Assert.Contains("ref UniTask<Sprite> __result", src);
        Assert.Contains("__result = __instance.LoadThumbnailFromMasterDataAsync(", src);
        Assert.Contains("iconSize, mCharacterId, skinType, cacheType", src);
        Assert.Contains("return false;", src);
        Assert.Contains("未持有角色交流缩略图已切换到Master路径", src);
        Assert.DoesNotContain("._dataList", src);
        Assert.DoesNotContain(".Add(", src);
        Assert.DoesNotContain(".Remove(", src);
        Assert.DoesNotMatch(new Regex(@"\.\s*Request\w*\s*\(", RegexOptions.CultureInvariant), src);

        Assert.Contains("InteractionThumbnailLocalUnlockPatch.TargetMethod()", manager);
        Assert.Contains("nameof(InteractionThumbnailLocalUnlockPatch.Prefix)", manager);
    }

    [Fact]
    public void 主角色详情补丁注册_不得再生成复杂值类型的IL2CPP托管跳板()
    {
        var manager = Read("AbyssCGUnlock/Patches/PatchManager.cs");
        var patchesDir = RepoRoot.SourceFile("AbyssCGUnlock/Patches");
        var patches = string.Join(Environment.NewLine,
            Directory.EnumerateFiles(patchesDir, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains("CharacterDataStoreLocalLookupPatch", manager);
        Assert.DoesNotContain("MainCharacterDetailLocalUnlockPatch", manager);
        Assert.DoesNotContain("UpdateCurrentCharacterModel", manager);
        Assert.DoesNotContain("nameof(MainDetailSubService.UpdateCurrentCharacterModel)", patches);
        Assert.False(File.Exists(RepoRoot.SourceFile("AbyssCGUnlock/Patches/MainCharacterDetailLocalUnlockPatch.cs")));
    }

    [Fact]
    public void NTR页面门控_锁定真实闭包并临时覆盖直接读取的backingField()
    {
        var src = Read("AbyssCGUnlock/Patches/NtrSceneEntryPatch.cs");

        Assert.Contains("typeof(DetailSubService.__c__DisplayClass90_0)", src);
        Assert.Contains("nameof(DetailSubService.__c__DisplayClass90_0._InitializeViewAsync_b__6)", src);
        Assert.Contains("_IsTavernRegistered_k__BackingField", src);
        Assert.Contains("Prefix", src);
        Assert.Contains("Finalizer", src);
    }

    [Fact]
    public void 新增本地绕过源码_不得调用API请求或错误上报网络原语()
    {
        var src = string.Join(Environment.NewLine,
            Read("AbyssCGUnlock/DynamicCharacterCatalog.cs"),
            Read("AbyssCGUnlock/DynamicTavernCharacterSkinSelector.cs"),
            Read("AbyssCGUnlock/LocalCharacterRegistry.cs"),
            Read("AbyssCGUnlock/Patches/CharacterListLocalUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/CharacterTopLocalUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/CharacterTopListRefreshLocalUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/CharacterModelLocalUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/CharacterDetailLocalUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/CharacterDataStoreLocalLookupPatch.cs"),
            Read("AbyssCGUnlock/Patches/InteractionThumbnailLocalUnlockPatch.cs"),
            Read("AbyssCGUnlock/Patches/NtrSceneEntryPatch.cs"));

        Assert.DoesNotMatch(new Regex(@"\b(?:Character|Interaction)\w*ApiDataStore\b", RegexOptions.CultureInvariant), src);
        Assert.DoesNotMatch(new Regex(@"\.\s*Request\w*\s*\(", RegexOptions.CultureInvariant), src);
        Assert.DoesNotMatch(new Regex(@"\b(?:HttpClient|HttpWebRequest|WebRequest|UnityWebRequest|UnityUserReportingPlatform)\b", RegexOptions.CultureInvariant), src);
        Assert.DoesNotContain("System.Net", src);
        Assert.DoesNotContain("SendWebRequest", src);
    }

    [Fact]
    public void 配置与注册_默认包含未持有详情和NTR页面两个新绕过()
    {
        var config = Read("AbyssCGUnlock/PluginConfig.cs");
        var manager = Read("AbyssCGUnlock/Patches/PatchManager.cs");

        Assert.Contains("EnableUnownedCharacterDetail", config);
        Assert.Contains("EnableNtrSceneEntryBypass", config);
        Assert.Contains("CharacterCatalogCapturePatch", manager);
        Assert.Contains("UnownedCharacterSelectionPatch", manager);
        Assert.Contains("CharacterTopLocalUnlockPatch", manager);
        Assert.Contains("CharacterTopListRefreshLocalUnlockPatch", manager);
        Assert.Contains("CharacterModelLocalUnlockPatch", manager);
        Assert.DoesNotContain("CharacterTopThumbnailModelLocalUnlockPatch", manager);
        Assert.Contains("CharacterDetailLocalUnlockPatch", manager);
        Assert.Contains("CharacterDataStoreLocalLookupPatch", manager);
        Assert.Contains("InteractionThumbnailLocalUnlockPatch", manager);
        Assert.DoesNotContain("MainCharacterDetailLocalUnlockPatch", manager);
        Assert.Contains("NtrSceneEntryPatch", manager);
    }

    [Theory]
    [InlineData("Project.Interaction.CharacterList.SubService+__c")]
    [InlineData("_SetupCharacterListEvent_b__33_0")]
    [InlineData("CreateCharacterThumbnailModels")]
    [InlineData("_nonHasCharaDataList")]
    [InlineData("Project.Interaction.AdventurerDetail.SubService+__c__DisplayClass90_0")]
    [InlineData("_InitializeViewAsync_b__6")]
    [InlineData("_IsTavernRegistered_k__BackingField")]
    [InlineData("_CharaDataStore_k__BackingField")]
    [InlineData("_dataList")]
    [InlineData("Project.CharacterTop.SubService")]
    [InlineData("UpdateView() -> System.Void")]
    [InlineData("RefreshActiveCharacterList() -> System.Void")]
    [InlineData("Project.Outgame.CharacterModel")]
    [InlineData("CreateFromUser(System.Int64,Project.AppDefine+IconSize,Il2CppSystem.Threading.CancellationToken)")]
    [InlineData("CreateFromMaster(Project.User.CharacterData,Project.AppDefine+IconSize,Il2CppSystem.Threading.CancellationToken)")]
    [InlineData("Project.User.CharacterDataStore")]
    [InlineData("GetByTableId(System.Int64) -> Project.User.CharacterData")]
    [InlineData("Project.ThumbnailLoader.CharacterThumbnailLoader")]
    [InlineData("LoadThumbnailFromUserDataAsync(Project.AppDefine+IconSize,System.Int64,Project.Master.SkinType,Absf.CacheType)")]
    [InlineData("LoadThumbnailFromMasterDataAsync(Project.AppDefine+IconSize,System.Int64,Project.Master.SkinType,Absf.CacheType)")]
    [InlineData("Project.Master.NoaMessagePack.MCharacters")]
    [InlineData("Project.Master.NoaMessagePack.MCharacterSkins")]
    [InlineData("Project.Master.NoaMessagePack.MTavernCharacterCards")]
    [InlineData("_TavernMCharacterSkinId_k__BackingField")]
    [InlineData("m_character_id : System.Int64")]
    [InlineData("m_character_skin_id : System.Int64")]
    [InlineData("is_default : System.Int32")]
    [InlineData("Project.Master.MasterDataStore")]
    [InlineData("Absf.IServerTimeAccessor")]
    public void 新补丁使用的代理标识符_必须固化在反编译元数据夹具(string identifier)
    {
        Assert.Contains(identifier, ProxyTargetsFixture.Text);
    }
}
