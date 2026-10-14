using System.Collections.Generic;
using System.Linq;
using Aggro.Core;
using HarmonyLib;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

namespace CrashoutCrew_SkysTheLimit.Patches;

[HarmonyPatch(typeof(Grabbable))]
public class Grabbable_Patches
{
    private static float SpringInterpolation(float[] samples, int x)
    {
        return x < samples.Length ? samples[x] : samples[^1];
    }
    
    private static readonly Dictionary<uint, Entity[]> PendingStacks = new();

    private static void SendStackContext(Grabbable grabbable, List<Entity> stack)
    {
        if (!NetworkServer.active) return;
        NetworkServer.SendToReadyObservers(grabbable.netIdentity,
            new StackContextMsg { netId = grabbable.netId, stack = stack.ToArray() },
            true, Channels.Reliable);
    }

    public static void OnStackContext(StackContextMsg msg)
    {
        if (NetworkServer.active) return;
        PendingStacks[msg.netId] = msg.stack;
    }

    private static void GetRpcStack(Grabbable grabbable, List<Entity> into)
    {
        if (PendingStacks.Remove(grabbable.netId, out var stack))
            into.AddRange(stack);
        else
            grabbable.GetStack(into);
    }

    [HarmonyPatch(nameof(Grabbable.OnInitializeBehaviour))]
    [HarmonyPrefix]
    public static void OnInitializeBehaviour_Prefix(Grabbable __instance)
    {
        __instance.springs = new Grabbable.SpringData[SkysTheLimitAPI.StackLimit];
        for (var i = 0; i < __instance.springs.Length; i++)
        {
            var data = new Grabbable.SpringData();
            data.angularFrequency = SpringInterpolation([12, 14, 16, 20], i);
            data.dampingRatio = SpringInterpolation([1.125f, 1.5f, 2, 3], i);
            data.distanceMultiplier = 1.4f + 1.6f / (i + 1);
            data.lerpMultiplier = 1f / (0.5f * i + 1);;
            __instance.springs[i] = data;
        }
    }

    [HarmonyPatch(nameof(Grabbable.OnInitializeBehaviour))]
    [HarmonyPostfix]
    public static void OnInitializeBehaviour_Postfix(Grabbable __instance)
    {
        if (__instance.isServer)
            __instance.Network_stackSlotsRemaining = SkysTheLimitAPI.StackLimit - 1;
    }

    [HarmonyPatch(nameof(Grabbable.OnUpdateSimulation))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode != System.Reflection.Emit.OpCodes.Ldc_I4_4)
                continue;
            if (codes[i + 1].opcode != System.Reflection.Emit.OpCodes.Stloc_S)
                continue;
            codes[i] = new CodeInstruction(
                System.Reflection.Emit.OpCodes.Ldsfld,
                AccessTools.Field(typeof(SkysTheLimitAPI), nameof(SkysTheLimitAPI.StackLimit))
            );
            break;
        }
        return codes;
    }

    /*
     * GETSTACK SERVER FUNCTION ASSUMES A SET NUMBER OF OBJECTS
     * I REPLACE SERVERGETSTACK() AND THEREFORE ALL CALLS INTO IT TO AVOID BREAKING WITH HIGHER MAX STACK COUNT
     */

    [HarmonyPatch(nameof(Grabbable.ServerBackFromOutbound))]
    [HarmonyPrefix]
    public static bool ServerBackFromOutbound(Grabbable __instance)
    {
        if (!NetworkServer.active)
            Debug.LogWarning(
                "[Server] function 'System.Void Grabbable::ServerBackFromOutbound()' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerBackFromOutbound();
        else
        {
            __instance._isInteractable = true;
            __instance._serverIsOutbounding = false;
            for (var i = 0; i < __instance._stack.Count; ++i)
                __instance._stack[i].GetObject<Grabbable>()._serverIsOutbounding = false;
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            var currentRoomType = GameUtil.GetCurrentRoomType();
            foreach (var stackEntity in stack.Skip(1))
                __instance.SetPhysics(stackEntity, currentRoomType);
            var position = ((IEntityBehaviourBase)__instance).entity.transform.position;
            var rotation = ((IEntityBehaviourBase)__instance).entity.transform.rotation;
            __instance.ServerFixStack(position, rotation);
            SendStackContext(__instance, stack);
            __instance.RpcReadyFromInbound(Entity.invalid, Entity.invalid, Entity.invalid, currentRoomType);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcReadyFromInbound__Entity__Entity__Entity__RoomType))]
    [HarmonyPrefix]
    public static bool UserCode_RpcReadyFromInbound__Entity__Entity__Entity__RoomType(
        Grabbable __instance,
        Entity e2,
        Entity e3,
        Entity e4,
        RoomType room)
    {
        if (__instance.isServer)
            return false;
        __instance._isInteractable = true;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        foreach (var stackEntity in stack.Skip(1))
            __instance.SetPhysics(stackEntity, room);
        return false;
    }

    [HarmonyPatch(nameof(Grabbable.ServerBreakEntireStack))]
    [HarmonyPrefix]
    public static bool ServerBreakEntireStack(Grabbable __instance)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerBreakEntireStack()' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerBreakEntireStack();
        else
        {
            if (__instance.serverHolderEntity != Entity.invalid)
            {
                Grabbable._holders.Clear();
                __instance.serverHolderEntity.GetObjects<GrabbableHolder>(Grabbable._holders);
                foreach (var holder in Grabbable._holders.Where(holder => holder.id == __instance.serverHolderId))
                {
                    holder.ServerRemoveItem();
                    break;
                }
                __instance.serverHolderEntity = Entity.invalid;
            }

            if (__instance.serverPlayerEntity != Entity.invalid)
            {
                __instance.serverPlayerEntity.GetObject<PlayerGrabber>().ServerDropBoxesSimple();
                __instance.serverPlayerEntity = Entity.invalid;
            }
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            var currentRoomType = GameUtil.GetCurrentRoomType();
            foreach (var stackEntity in stack)
                __instance.SetPhysics(stackEntity, currentRoomType);
            SendStackContext(__instance, stack);
            __instance.RpcBreakStack(Entity.invalid, Entity.invalid, Entity.invalid, Entity.invalid, currentRoomType);
            ((IEntityBehaviourBase)__instance).entity.predictedRigidbodyGroup.ServerResetGroup();
            foreach (var entity2 in __instance._stack)
            {
                var grabbable = entity2.GetObject<Grabbable>();
                grabbable.RpcQueueEvBrokeEntireStack();
                var onUnitSphere = UnityEngine.Random.onUnitSphere;
                onUnitSphere.y = math.abs(onUnitSphere.y);
                var vector3 = entity2.transform.TransformDirection(onUnitSphere);
                entity2.rigidbody.AddForce(vector3 * grabbable.breakStackForceAmount, ForceMode.Impulse);
                grabbable.Network_baseGrabbable = null;
                entity2.predictedRigidbodyGroup.ServerResetGroup();
            }
            __instance._stack.Clear();
            __instance.tutorialStackBroken = true;
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcBreakStack__Entity__Entity__Entity__Entity__RoomType))]
    [HarmonyPrefix]
    public static bool UserCode_RpcBreakStack__Entity__Entity__Entity__Entity__RoomType(
        Grabbable __instance,
        Entity e1,
        Entity e2,
        Entity e3,
        Entity e4,
        RoomType room)
    {
        if (__instance.isServer)
            return false;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        foreach (var stackEntity in stack)
            __instance.SetPhysics(stackEntity, room);
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.ServerPlaceInHolder))]
    [HarmonyPrefix]
    public static bool ServerPlaceInHolder(Grabbable __instance, GrabbableHolder holder)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerPlaceInHolder()' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerPlaceInHolder(holder);
        else
        {
            var container = holder.container;
            __instance.serverPlayerEntity = Entity.invalid;
            __instance.serverHolderEntity = ((IEntityBehaviourBase)holder).entity;
            __instance.serverHolderId = holder.id;
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            var currentRoomType = GameUtil.GetCurrentRoomType();
            for (var i = 0; i < stack.Count; i++)
                __instance.SetKinematic(stack[i], container, i, true, true); 
            __instance.ServerFixStack(container.position, container.rotation);
            SendStackContext(__instance, stack);
            __instance.RpcPlaceInHolder(((IEntityBehaviourBase)holder).entity, holder.id, Entity.invalid, Entity.invalid, Entity.invalid, currentRoomType);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcPlaceInHolder__Entity__Int32__Entity__Entity__Entity__RoomType))]
    [HarmonyPrefix]
    public static bool UserCode_RpcPlaceInHolder__Entity__Int32__Entity__Entity__Entity__RoomType(
        Grabbable __instance,
        Entity holderEntity,
        int holderId,
        Entity e2,
        Entity e3,
        Entity e4,
        RoomType room)
    {
        if (__instance.isServer)
            return false;
        if (!__instance.TryGetHolder(holderEntity, holderId, out var holder))
            Debug.LogError("Could not find holder!");
        else
        {
            var container = holder.container;
            var stack = new List<Entity>();
            GetRpcStack(__instance, stack);
            for (var i = 0; i < stack.Count; i++)
                __instance.SetKinematic(stack[i], container, i, true, true);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.ServerPlayerDropped))]
    [HarmonyPrefix]
    public static bool ServerPlayerDropped(Grabbable __instance, Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerPlayerDropped(UnityEngine.Vector3,UnityEngine.Vector3,UnityEngine.Quaternion)' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerPlayerDropped(position, velocity, rotation);
        else
        {
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            var currentRoomType = GameUtil.GetCurrentRoomType();
            __instance.serverPlayerEntity = Entity.invalid;
            foreach (var stackEntity in stack)
                __instance.SetPhysics(stackEntity, currentRoomType);
            SendStackContext(__instance, stack);
            __instance.RpcPlayerDropped(Entity.invalid, Entity.invalid, Entity.invalid, currentRoomType);
            Grabbable._entities.Clear();
            __instance.GetStack(Grabbable._entities);
            foreach (var constraints in Grabbable._entities.Select(e => e.rigidbody.constraints).Where(PhysicsUtil.IsConstrainingRotation))
            {
                rotation = PhysicsUtil.Constrain(rotation, constraints);
                break;
            }
            __instance.ServerFixStack(position, velocity, rotation);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcPlayerDropped__Entity__Entity__Entity__RoomType))]
    [HarmonyPrefix]
    public static bool UserCode_RpcPlayerDropped__Entity__Entity__Entity__RoomType(
        Grabbable __instance,
        Entity e2,
        Entity e3,
        Entity e4,
        RoomType room)
    {
        if (__instance.isServer)
            return false;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        foreach (var stackEntity in stack)
            __instance.SetPhysics(stackEntity, room);
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.ServerPlayerGrabbed))]
    [HarmonyPrefix]
    public static bool ServerPlayerGrabbed(Grabbable __instance, PlayerGrabber grabber)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerPlayerGrabbed(PlayerGrabber)' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerPlayerGrabbed(grabber);
        else
        {
            var grabbedContainer = grabber.grabbedContainer;
            __instance.serverPlayerEntity = ((IEntityBehaviourBase)grabber).entity;
            __instance.serverHolderEntity = Entity.invalid;
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            for (var i = 0; i < stack.Count; i++)
                __instance.SetKinematic(stack[i], grabbedContainer, i, false, false); 
            SendStackContext(__instance, stack);
            __instance.RpcSetPlaceInPlayerGrabber(((IEntityBehaviourBase)grabber).entity, Entity.invalid, Entity.invalid, Entity.invalid);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcSetPlaceInPlayerGrabber__Entity__Entity__Entity__Entity))]
    [HarmonyPrefix]
    public static bool UserCode_RpcSetPlaceInPlayerGrabber__Entity__Entity__Entity__Entity(
        Grabbable __instance,
        Entity player,
        Entity e2,
        Entity e3,
        Entity e4)
    {
        if (__instance.isServer || !player.TryGetObject<PlayerGrabber>(out var playerGrabber))
            return false;
        var grabbedContainer = playerGrabber.grabbedContainer;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        for (var i = 0; i < stack.Count; i++)
            __instance.SetKinematic(stack[i], grabbedContainer, i, false, false);
        AudioManager.PlaySfx(__instance.pickUpSfx, ((IEntityBehaviourBase)__instance).entity.transform.position);
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.ServerPlayerPrepareForStacked))]
    [HarmonyPrefix]
    public static bool ServerPlayerPrepareForStacked(Grabbable __instance)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerPlayerPrepareForStacked()' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerPlayerPrepareForStacked();
        else
        {
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            var currentRoomType = GameUtil.GetCurrentRoomType();
            __instance.serverPlayerEntity = Entity.invalid;
            foreach (var stackEntity in stack)
                __instance.SetPhysics(stackEntity, currentRoomType);
            SendStackContext(__instance, stack);
            __instance.RpcPlayerPrepareForStacked(Entity.invalid, Entity.invalid, Entity.invalid, currentRoomType);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcPlayerPrepareForStacked__Entity__Entity__Entity__RoomType))]
    [HarmonyPrefix]
    public static bool UserCode_RpcPlayerPrepareForStacked__Entity__Entity__Entity__RoomType(
        Grabbable __instance,
        Entity e2,
        Entity e3,
        Entity e4,
        RoomType room)
    {
        if (__instance.isServer)
            return false;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        foreach (var stackEntity in stack)
            __instance.SetPhysics(stackEntity, room);
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.ServerReadyForOutboundTransition))]
    [HarmonyPrefix]
    public static bool ServerReadyForOutboundTransition(Grabbable __instance, GrabbableHolder holder)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerReadyForOutboundTransition(GrabbableHolder)' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerReadyForOutboundTransition(holder);
        else
        {
            __instance._isInteractable = false;
            __instance._serverIsOutbounding = true;
            for (var i = 0; i < __instance._stack.Count; ++i)
                __instance._stack[i].GetObject<Grabbable>()._serverIsOutbounding = true;
            var container = holder.container;
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            for (var i = 1; i < stack.Count; i++)
                __instance.SetKinematic(stack[i], container, i, false, true);
            SendStackContext(__instance, stack);
            __instance.RpcReadyForOutboundTransition(((IEntityBehaviourBase)holder).entity, holder.id, Entity.invalid, Entity.invalid, Entity.invalid);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcReadyForOutboundTransition__Entity__Int32__Entity__Entity__Entity))]
    [HarmonyPrefix]
    public static bool UserCode_RpcReadyForOutboundTransition__Entity__Int32__Entity__Entity__Entity(
        Grabbable __instance,
        Entity holderEntity,
        int holderId,
        Entity e2,
        Entity e3,
        Entity e4)
    {
        if (__instance.isServer)
            return false;
        __instance._isInteractable = false;
        if (!__instance.TryGetHolder(holderEntity, holderId, out var holder))
        {
            Debug.LogError("Could not find holder!");
            return false;
        }
        var container = holder.container;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        for (var i = 1; i < stack.Count; i++)
            __instance.SetKinematic(stack[i], container, i, false, true);
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.ServerRemoveFromHolder))]
    [HarmonyPrefix]
    public static bool ServerRemoveFromHolder(Grabbable __instance)
    {
        if (!NetworkServer.active)
            Debug.LogWarning("[Server] function 'System.Void Grabbable::ServerRemoveFromHolder()' called when server was not active");
        else if (__instance.Network_baseGrabbable != null)
            __instance.Network_baseGrabbable.GetEntity().GetObject<Grabbable>().ServerRemoveFromHolder();
        else
        {
            __instance.serverHolderEntity = Entity.invalid;
            var stack = new List<Entity>();
            __instance.GetStack(stack);
            var currentRoomType = GameUtil.GetCurrentRoomType();
            foreach (var stackEntity in stack)
                __instance.SetPhysics(stackEntity, currentRoomType);
            SendStackContext(__instance, stack);
            __instance.RpcRemoveFromHolder(Entity.invalid, Entity.invalid, Entity.invalid, currentRoomType);
        }
        return false;
    }
    
    [HarmonyPatch(nameof(Grabbable.UserCode_RpcRemoveFromHolder__Entity__Entity__Entity__RoomType))]
    [HarmonyPrefix]
    public static bool UserCode_RpcRemoveFromHolder__Entity__Entity__Entity__RoomType(
        Grabbable __instance,
        Entity e2,
        Entity e3,
        Entity e4,
        RoomType room)
    {
        if (__instance.isServer)
            return false;
        var stack = new List<Entity>();
        GetRpcStack(__instance, stack);
        foreach (var stackEntity in stack)
            __instance.SetPhysics(stackEntity, room);
        return false;
    }
}