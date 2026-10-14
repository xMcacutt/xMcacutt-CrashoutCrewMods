using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CrashoutCrew_APClient;

public static class ContractGenerator
{
    private static readonly List<(string, string)> Warehouses = 
    [
        ("Assets/Scenes/scene-Dumbbell_1-2.unity", "Assets/Scenes/scene-Dumbbell.unity"),
        ("Assets/Scenes/scene-IPiece_1-2.unity", "Assets/Scenes/scene-IPiece.unity"),
        ("Assets/Scenes/scene-SBendMini_1-2.unity", "Assets/Scenes/scene-SBendMini.unity"),
        ("Assets/Scenes/scene-ConveyorBlock.unity", "Assets/Scenes/scene-ConveyorBlock.unity"),
        ("Assets/Scenes/scene-ConveyorHallway.unity", "Assets/Scenes/scene-ConveyorHallway.unity"),
        ("Assets/Scenes/scene-ConveyorLoop.unity", "Assets/Scenes/scene-ConveyorLoop.unity"),
        ("Assets/Scenes/scene-Figure8RaceTrackMini.unity", "Assets/Scenes/scene-Figure8RaceTrackMini.unity"),
        ("Assets/Scenes/scene-LBendRaceTrackMini.unity", "Assets/Scenes/scene-LBendRaceTrackMini.unity"),
        ("Assets/Scenes/scene-RaceTrackMini.unity", "Assets/Scenes/scene-RaceTrackMini.unity"),
        ("Assets/Scenes/scene-UBendMiniRacetrack.unity", "Assets/Scenes/scene-UBendMiniRacetrack.unity"),
    ];

    private static readonly int[] OutboundDeliveryCount = [4, 5, 6, 7, 8];

    public static ContractObject[] GenerateContracts(
        Random random, 
        ShiftOrderObject[] allOrders, 
        int twoOrderContracts,
        int threeOrderContracts, 
        int fourOrderContracts)
    {
        var contractSizes = new List<int>();
        contractSizes.AddRange(Enumerable.Repeat(2, twoOrderContracts));
        contractSizes.AddRange(Enumerable.Repeat(3, threeOrderContracts));
        contractSizes.AddRange(Enumerable.Repeat(4, fourOrderContracts));
        contractSizes = contractSizes.OrderBy(_ => random.Next()).ToList();
        
        var contracts = new List<ContractObject>();
        var orders = new List<ShiftOrderObject>();

        foreach (var size in contractSizes)
        {
            var selectedOrders = new List<ShiftOrderObject>();
            while (selectedOrders.Count < size)
            {
                if (orders.Count == 0)
                    orders.AddRange(allOrders.OrderBy(_ => random.Next()));
                var nextOrder = orders.FirstOrDefault(o => !selectedOrders.Contains(o));
                orders.Remove(nextOrder);
                selectedOrders.Add(nextOrder);
            }
            contracts.Add(GenerateContract(random, selectedOrders.ToArray()));
        }
        
        contracts = contracts.OrderBy(x => x.orders.Length).ToList();

        for (var i = 0; i < contracts.Count; i++)
            contracts[i].bellsRequired = 5 * i;
        
        return contracts.ToArray();
    }

    private static ContractObject GenerateContract(Random random, ShiftOrderObject[] orders)
    {
        var contract = ScriptableObject.CreateInstance<ContractObject>();
        contract.title = orders.GenerateName(random);
        contract.name = contract.title.Replace(' ', '_').ToUpper();
        contract.modifierMultiplier = 1;
        contract.type = ContractType.Explicit;
        contract.orders = orders;
        var warehouses = Warehouses.Random(random: random);
        contract.smallWarehouse = warehouses.Item1;
        contract.bigWarehouse = warehouses.Item2;
        contract.multiplierForOnePlayer = 0.4f;
        contract.multiplierForTwoPlayers = 0.7f;
        contract.multiplierForThreePlayers = 0.85f;
        contract.shift1 = GenerateShift(random, contract, orders, 0);
        contract.shift2 = GenerateShift(random, contract, orders, 1);
        contract.shift3 = GenerateShift(random, contract, orders, 2);
        contract.shift4 = GenerateShift(random, contract, orders, 3);
        contract.shift5 = GenerateShift(random, contract, orders, 4);
        contract.shopCards = [];
        contract.unlocks = [];
        contract.demoVisualPrefabs = [];
        return contract;
    }

    private static ContractShift GenerateShift(Random random, ContractObject contract, ShiftOrderObject[] orders, int shiftIndex)
    {
        var shift = new ContractShift();
        List<ContractShift.Outbound> outbounds = [];
        List<ContractShift.Inbound> inbounds = [];
        List<ContractShift.Order> shiftOrders = [];
        shift.owner = contract;
        shift.truckPatienceDuration = random.Next(90, 110);
        
        var outboundDeliveries = OutboundDeliveryCount[shiftIndex];
        for (var i = 0; i < outboundDeliveries; i++)
        {
            var outboundBoxCount = random.Next(4, 7); 
            outbounds.Add(new ContractShift.Outbound
            {
                bayCount = 1,
                boxCount = outboundBoxCount,
                secondsFromPrevious = random.Next(8, 14)
            });
            var inboundBayCount = random.Next(1, 3); 
            var totalInboundBoxes = outboundBoxCount + random.Next(0, 3);
            var inboundBoxCountPerBay = Mathf.CeilToInt((float)totalInboundBoxes / inboundBayCount);
            inbounds.Add(new ContractShift.Inbound
            {
                bayCount = inboundBayCount,
                boxCount = inboundBoxCountPerBay,
                normalizedTime = i * (0.75f / outboundDeliveries),
            });
        }
        shift.outbound = outbounds.ToArray();
        shift.inbound = inbounds.ToArray();
        shift.payOutAmount = outboundDeliveries * random.Next(75, 115);
        foreach (var order in orders)
        {
            var minimum = shiftIndex < 4 ? 0 : 1;
            shiftOrders.Add(new ContractShift.Order
            {
                owner = contract,
                order = order,
                randomOrderIndex = 1,
                cardCount = random.Next(minimum, 3),
            });
        }
        if (shiftOrders.All(x => x.cardCount == 0))
            shiftOrders.Random(random: random)!.cardCount = 1;
        shift.orders = shiftOrders.ToArray();
        return shift;
    }
}