using System;
using System.Collections.Concurrent;
using Aggro.Core;
using Archipelago.MultiClient.Net.Models;
using CrashoutCrew_SkysTheLimit;
using UnityEngine;

namespace CrashoutCrew_APClient;

public enum CCItem
{
    Bell = 0x1,
    Boost = 0x2,
    Charge = 0x3,
    Button = 0x4,
    Extinguisher = 0x5,
    Scrub = 0x6,
    Scrubbie = 0x7,
    Shelf = 0x8,
    Sprinkler = 0x9,
    Sturdy = 0xa,
    Tele = 0xb,
    Trash = 0xc,
    Vac = 0xd,
    Leaf = 0xe,
    XlSprinkler = 0xf,
    WaterBalloon = 0x10,
    Wild = 0x11,
    Nitro = 0x12,
    Aroma = 0x13,
    Drift = 0x14,
    Shield = 0x15,
    Chain = 0x16,
    Cup = 0x17,
    Fan = 0x18,
    Grip = 0x19,
    Spoiler = 0x1a,
    Cosmetic = 0x20,
    SpeedUpgrade = 0x30,
    StrengthUpgrade = 0x31,
    BoostUpgrade = 0x32,
    StressUpgrade = 0x33,
}

public class ItemHandler : MonoBehaviour
{
    public static ItemHandler Instance = null!;
    private readonly ConcurrentQueue<(int, ItemInfo)> _cachedItems = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void Reset() => _cachedItems.Clear();
    
    public void EnqueueItem(int itemIndex, ItemInfo item) => _cachedItems.Enqueue((itemIndex, item));

    private bool IsReady()
    {
        return ArchipelagoHandler.Instance.IsConnected && PluginMain.SaveDataHandler?.APSaveData != null;
    }
    
    void Update()
    {
        while (IsReady() && _cachedItems.TryDequeue(out var item))
            HandleItem(item.Item1, item.Item2);
    }

    private void HandleItem(int index, ItemInfo item)
    {
        if (index <= PluginMain.SaveDataHandler!.APSaveData!.ItemIndex)
            return;

        switch ((CCItem)item.ItemId)
        {
            case CCItem.Bell:
                PluginMain.SaveDataHandler.APSaveData.Bells++;
                var lobbyManager = FindObjectOfType<LobbyManager>();
                if (lobbyManager != null && lobbyManager.isServer)
                {
                    // Set level to be unlocked on enter
                    lobbyManager.Network_syncHostTotalBells = PluginMain.SaveDataHandler.APSaveData.Bells;
                    lobbyManager._syncUnlockList.Clear();
                    foreach (var allContract in lobbyManager._allContracts)
                    {
                        if (allContract.isDemoLocked)
                            lobbyManager._syncUnlockList.Add(false);
                        else if (lobbyManager._syncHostTotalBells >= allContract.bellsRequired 
                                 || allContract.type != ContractType.Random 
                                 && SaveManager.data.TryGetContractBellCount(allContract, out var bells) && bells > 0)
                            lobbyManager._syncUnlockList.Add(true);
                        else
                            lobbyManager._syncUnlockList.Add(false);
                    }
                    // Set conveyor objects to be visible / invisible
                    var lobbyContractVisualManager = FindObjectOfType<LobbyContractVisualManager>();
                    if (lobbyContractVisualManager != null)
                    {
                        for (var contractIndex = 0; contractIndex < lobbyContractVisualManager._contracts.Count; ++contractIndex)
                        {
                            var contract = lobbyContractVisualManager._contracts[contractIndex];
                            var num = contract.type == ContractType.Random ? contract.randomBoxCount : contract.orders.Length;
                            var visualStack = lobbyContractVisualManager.contractVisualGroup.GetComponentsInChildren<LobbyOrderVisualStack>()[contractIndex];
                            for (var componentIndex = 0; componentIndex < num; ++componentIndex)
                                foreach (var componentsInChild in visualStack.GetComponentsInChildren<Renderer>())
                                    if (lobbyManager.IsContractUnlocked(contractIndex))
                                        componentsInChild.SetPropertyBlockFloat(LobbyContractVisualManager.Locked, 0f);
                        }
                    }
                    // Set UI to be locked / unlocked
                    var selectionUI = FindObjectOfType<ContractSelectionUI>();
                    if (selectionUI != null)
                    {
                        for (var contractIndex = 0; contractIndex < selectionUI._contracts.Count; ++contractIndex)
                        {
                            var contract = selectionUI._contracts[contractIndex];
                            if (!SaveManager.data.TryGetContractBellCount(contract, out var bells))
                                bells = 0;
                            if (!SaveManager.data.TryGetContractScore(contract, out var score))
                                score = ContractScore.D;
                            if (!SaveManager.data.TryGetContractTime(contract, out TimeSpan timeSpan))
                                timeSpan = TimeSpan.Zero;
                            var contractUI = selectionUI.contractGroup.GetComponentsInChildren<ContractUI>()[contractIndex];
                            if (contract.bellsRequired > PluginMain.SaveDataHandler.APSaveData.Bells)
                                continue;
                            contractUI.lockedContainer.SetActive(false);
                            contractUI.unlockedContainer.SetActive(true);
                            contractUI.bellsRequiredContainer.SetActive(true);
                            contractUI.bellsRequiredText.text = "";
                            contractUI.localizedText.SetIndex(contract.title);
                            contractUI.localizedText.gameObject.SetActive(true);
                            contractUI.lockedText.SetActive(false);
                            var checkedLocations = ArchipelagoHandler.Instance.CountLocationsCheckedInRange(0x30 * contractIndex + 1, 0x30 * (contractIndex + 1) + 1);
                            contractUI.contractNumText.text = $"{checkedLocations}/{35} locations - {contractIndex + 1}";
                            contractUI.contractNumText.enabled = true;
                            for (var bellIndex = 0; bellIndex < contractUI.bells.Length; ++bellIndex)
                                contractUI.bells[bellIndex].SetActive(bellIndex < (int)score);
                            for (var bubbleIndex = 0; bubbleIndex < contractUI.scoreBubbles.Length; ++bubbleIndex)
                                contractUI.scoreBubbles[bubbleIndex].scoreBubble.SetActive(false);
                            contractUI.finalGradeContainer.gameObject.SetActive(bells > 0);
                            contractUI.finalGradeImage.sprite = contractUI.gradeSprites[bells == 5 ? (int)score + 1 : 0];
                            contractUI.finalGradeDillyImage.sprite = contractUI.gradeDillySprites[bells == 5 ? (int)score + 1 : 0];
                            contractUI.finalGradeImage.color = GlobalScriptableObject<AggroSettingsObject>.instance.gradeColors[bells == 5 ? (int)score + 1 : 0];
                            contractUI.finalGradeDillyImage.color = GlobalScriptableObject<AggroSettingsObject>.instance.gradeColors[bells == 5 ? (int)score + 1 : 0];
                            if (bells >= 5 && timeSpan != TimeSpan.Zero)
                                contractUI.bestTimeText.text = timeSpan.ToString("mm\\:ss\\:ff");
                            else
                                contractUI.bestTimeText.text = "--:--:--";
                        }
                    }
                }
                break;
            case CCItem.Boost:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPBOOST");
                break;
            case CCItem.Charge:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPCHARGE");
                break;
            case CCItem.Button:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPBT");
                break;
            case CCItem.Extinguisher:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPFE");
                break;
            case CCItem.Scrub:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPSCRUB");
                break;
            case CCItem.Scrubbie:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPROBO");
                break;
            case CCItem.Shelf:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPSHELF");
                break;
            case CCItem.Sprinkler:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPSMSPRINK");
                break;
            case CCItem.Sturdy:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPSTURDY");
                break;
            case CCItem.Tele:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPTELE");
                break;
            case CCItem.Trash:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPTRASH");
                break;
            case CCItem.Vac:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPVAC");
                break;
            case CCItem.Leaf:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPLEAF");
                break;
            case CCItem.XlSprinkler:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPXLSPRINK");
                break;
            case CCItem.WaterBalloon:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPWBP");
                break;
            case CCItem.Wild:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPCARD");
                break;
            case CCItem.Nitro:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPBOOSTP");
                break;
            case CCItem.Aroma:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPDECAYP");
                break;
            case CCItem.Drift:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPDRIFTP");
                break;
            case CCItem.Shield:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPEXPP");
                break;
            case CCItem.Chain:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPTRACP");
                break;
            case CCItem.Cup:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPSTRESSP");
                break;
            case CCItem.Fan:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPFIREP");
                break;
            case CCItem.Grip:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPGRIPP");
                break;
            case CCItem.Spoiler:
                PluginMain.SaveDataHandler.APSaveData.ShopUnlocks.Add("SHOPSPEEDP");
                break;
            case CCItem.Cosmetic:
                CostumeHandler.SelectRandom();
                break;
            case CCItem.SpeedUpgrade:
                PluginMain.SaveDataHandler.APSaveData.SpeedUpgrades = Math.Min(PluginMain.SaveDataHandler.APSaveData.SpeedUpgrades + 1, 3);
                break;
            case CCItem.StrengthUpgrade:
                PluginMain.SaveDataHandler.APSaveData.StrengthUpgrades = Math.Min(PluginMain.SaveDataHandler.APSaveData.StrengthUpgrades + 1, 3);
                SkysTheLimitAPI.SetStackLimit(PluginMain.SaveDataHandler.APSaveData.StrengthUpgrades + 3);
                break;
            case CCItem.BoostUpgrade:
                PluginMain.SaveDataHandler.APSaveData.BoostUpgrades = Math.Min(PluginMain.SaveDataHandler.APSaveData.BoostUpgrades + 1, 3);
                break;
            case CCItem.StressUpgrade:
                PluginMain.SaveDataHandler.APSaveData.StressUpgrades = Math.Min(PluginMain.SaveDataHandler.APSaveData.StressUpgrades + 1, 3);
                break;
        }
        
        PluginMain.SaveDataHandler.APSaveData.ItemIndex = index;
        PluginMain.SaveDataHandler.SaveGame();
    }
}