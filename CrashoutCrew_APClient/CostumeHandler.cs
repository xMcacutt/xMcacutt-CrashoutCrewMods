namespace CrashoutCrew_APClient;

public static class CostumeHandler
{
    private static readonly System.Random Random = new();
    
    public static void SelectRandom()
    {
        if (!GameUtil.TryGetLocalPlayer(out var player))
            return;
        var playerCostumeManager = player.GetObject<PlayerCostumeManager>();
        var costumeManagerNetwork = player.GetObject<PlayerCostumeManagerNetwork>();
        playerCostumeManager.SetUpUnlockedIndicies();
        var selected = Random.Next(playerCostumeManager.unlockedCostumeIndicies.Length);
        playerCostumeManager.currentUnlockedCostumeIndex = selected;
        costumeManagerNetwork.SetCostumeIndex(playerCostumeManager.unlockedCostumeIndicies[playerCostumeManager.currentUnlockedCostumeIndex]);
        playerCostumeManager.ResetAllCostumes();
        SaveManager.data.SetCurrentCostume(playerCostumeManager.costumes[costumeManagerNetwork.currentCostumeID].costumeObject);
    }
}