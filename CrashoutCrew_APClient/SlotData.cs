using System.Collections.Generic;

namespace CrashoutCrew_APClient;

public class SlotData
{
    public readonly int ContractCountFour = 4;
    public readonly int ContractCountThree = 12;
    public readonly int ContractCountTwo = 4;
    public readonly bool DeathLink = false;
    public int ContractCount => ContractCountFour + ContractCountThree + ContractCountTwo;

    public SlotData(Dictionary<string, object> slotDict)
    {
        foreach (var x in slotDict) APConsole.Instance.DebugLog($"{x.Key} {x.Value}");
        if (slotDict.TryGetValue("FourOrderContracts", out var rawContractCount4) && rawContractCount4 is long contractCountFour)
            ContractCountFour = (int)contractCountFour;
        if (slotDict.TryGetValue("ThreeOrderContracts", out var rawContractCount3) && rawContractCount3 is long contractCountThree)
            ContractCountThree = (int)contractCountThree;
        if (slotDict.TryGetValue("TwoOrderContracts", out var rawContractCount2) && rawContractCount2 is long contractCountTwo)
            ContractCountTwo = (int)contractCountTwo;
    }
}