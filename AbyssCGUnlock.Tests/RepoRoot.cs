namespace AbyssCGUnlock.Tests;

internal static class RepoRoot
{
    /// <summary>从测试程序集所在目录向上查找仓库根（含 AbyssCGUnlock.sln）。</summary>
    internal static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AbyssCGUnlock.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("未找到仓库根（AbyssCGUnlock.sln）");
    }

    internal static string SourceFile(string relativePath)
        => Path.Combine(Find(), relativePath);
}
