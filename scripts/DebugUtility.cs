using Godot;

public static class DebugUtility
{
    public static void Print(string message)
    {
        var args = OS.GetCmdlineArgs();
        var prefix = string.Empty;

        foreach (var arg in args)
        {
            if (!arg.Contains("instance")) continue;

            prefix = arg;
            break;
        }

        GD.Print($"{prefix} | {message}");
    }
}
