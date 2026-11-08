namespace ShutTheBox_GameEngine;

internal static class CodeGenerator
{
    private const string Characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string NewCode(Func<string, bool> isTaken)
    {
        string code;
        do
        {
            code = new string(Enumerable.Range(0, 6)
                .Select(_ => Characters[Random.Shared.Next(Characters.Length)])
                .ToArray());
        }
        while (isTaken(code));

        return code;
    }
}
