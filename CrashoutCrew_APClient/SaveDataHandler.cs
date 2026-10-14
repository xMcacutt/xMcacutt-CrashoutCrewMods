using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Aggro.Core;
using UnityEngine;

namespace CrashoutCrew_APClient;

using System.IO;
using Newtonsoft.Json;

public class CustomSaveData
{
    public int ItemIndex;
    public readonly List<string> ShopUnlocks = [];
    public int Bells;
    public int SpeedUpgrades;
    public int StrengthUpgrades;
    public int BoostUpgrades;
    public int StressUpgrades;
    public int HandlingUpgrades;
}

public class SaveDataHandler : MonoBehaviour
{
    public CustomSaveData? APSaveData;
    private string _customDataPath = "";
    public Action onInitialized = null!;

    public void GetSave()
    {
        var apHandler = ArchipelagoHandler.Instance;
        _customDataPath = $"{Application.persistentDataPath}/saves/Archipelago/{apHandler.Slot}{apHandler.Seed}.json";
        
        if (!Directory.Exists(Path.GetDirectoryName(_customDataPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(_customDataPath)!);
        
        if (File.Exists(_customDataPath))
            APSaveData = JsonConvert.DeserializeObject<CustomSaveData>(File.ReadAllText(_customDataPath)) ?? new CustomSaveData();
        else
        {
            APSaveData = new CustomSaveData();
            File.WriteAllText(_customDataPath, JsonConvert.SerializeObject(APSaveData));
        }
    }

    public void SaveGame()
    {
        File.WriteAllText(_customDataPath, JsonConvert.SerializeObject(APSaveData));
        SaveManager.SaveGameAsync();
    }
    
    public void InitializeSave()
    {
        StartCoroutine(InitializeSaveCo());
    }

    private IEnumerator InitializeSaveCo()
    {
        if (APSaveData == null)
            GetSave();
        if (SaveManager.DoesGameExist(0))
        {
            APConsole.Instance.DebugLog("Save game exists!");
            var task = SaveManager.InitializeLoadGameAsync(0);
            yield return new WaitForTask(task);
            if (task.Result != LoadResult.Loaded)
            {
                var doContinue = false;
                var doQuit = false;
                AggroInputManager.Enable();
                SaveError error;
                string info;
                switch (task.Result)
                {
                    case LoadResult.UsedBackup:
                        error = SaveError.UseBackup;
                        info = new DateTime(SaveManager.data.timeStampTicks).ToString("g", CultureInfo.CurrentCulture);
                        break;
                    case LoadResult.Failed:
                        error = SaveError.SaveFailed;
                        info = "";
                        break;
                    default:
                        throw new InvalidEnumException();
                }

                PlayerMessageManager.QueueErrorMessage(error, info, () => doContinue = true, () => doQuit = true);
                while (!doContinue && !doQuit)
                {
                    PlayerMessageManager.ProcessQueuedMessages();
                    yield return null;
                }

                AggroInputManager.Disable();
                if (doQuit)
                {
                    Application.Quit();
                    yield break;
                }

                if (doContinue)
                {
                    switch (task.Result)
                    {
                        case LoadResult.UsedBackup:
                            yield return SaveManager.DeletePrimarySaveAsync(0);
                            break;
                        case LoadResult.Failed:
                            yield return SaveManager.DeleteGameAsync(0);
                            SaveManager.InitializeNewGame(0);
                            break;
                        default:
                            throw new InvalidEnumException();
                    }

                    yield return SaveManager.SaveGameAsync();
                }
            }

            if (SaveManager.data.GetSaveVersion() != 1)
            {
                SaveManager.Uninitialize();
                SaveManager.InitializeNewGame(0);
                yield return new WaitForTask(SaveManager.SaveGameAsync());
            }
        }
        else
        {
            APConsole.Instance.DebugLog("Save game does not exist!");
            SaveManager.InitializeNewGame(0);
            yield return new WaitForTask(SaveManager.SaveGameAsync());
        }

        foreach (CostumeObject costume in GlobalScriptableObject<CosmeticGlobalData>.instance.costumes)
        {
            if (costume != null && costume.startsUnlocked)
                SaveManager.data.UnlockCostume(costume);
        }

        onInitialized.Invoke();
    }

    public void Reset()
    {
        SaveGame();
        APSaveData = null;
    }
}