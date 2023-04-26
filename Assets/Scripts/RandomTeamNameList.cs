public static class RandomTeamNameList
{
    private static string[] _randomNames = new[]
    {
        "Space Snakes", 
        "Caras Durões", 
        "Space Knives", 
        "Tragic Gods", 
        "Tasty Trash", 
        "Epic Demons", 
        "Grip Gang", 
        "Knives Army", 
        "Ungodly Clan", 
        "Nasty News", 
        "Unified4Bad", 
        "Flying Bulls"
    };

    public static string GetRandomName()
    {
        int index = UnityEngine.Random.Range(0, _randomNames.Length);
        return _randomNames[index];
    }
}
