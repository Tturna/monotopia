using Godot;

#nullable enable
public static class DebugUtility
{
    private static string? debugInstancePrefix;

    private static void FindDebugInstancePrefix()
    {
        var args = OS.GetCmdlineArgs();

        foreach (var arg in args)
        {
            if (!arg.Contains("instance")) continue;

            debugInstancePrefix = arg;
            break;
        }
    }

    public static void Print(string message)
    {
        if (debugInstancePrefix is null) FindDebugInstancePrefix();

        GD.Print($"{debugInstancePrefix} | {message}");
    }

    public static string GetBriefBuildInfoString()
    {
        if (debugInstancePrefix is null) FindDebugInstancePrefix();

        return $"Debug instance: {debugInstancePrefix}";
    }
}
