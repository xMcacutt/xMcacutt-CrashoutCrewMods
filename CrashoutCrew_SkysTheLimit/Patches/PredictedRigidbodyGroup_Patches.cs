using System;
using System.Collections.Generic;
using System.Linq;
using Aggro.Core;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CrashoutCrew_SkysTheLimit.Patches;

[HarmonyPatch(typeof(PredictedRigidbodyGroup))]
public static class PredictedRigidbodyGroup_Patches
{
    [HarmonyPatch(nameof(PredictedRigidbodyGroup.OnEntityCreated))]
    [HarmonyPrefix]
    public static bool OnEntityCreated(PredictedRigidbodyGroup __instance)
    {
        var ext = GetExtendedData(__instance);
        __instance.motionSmoothingVelocityThresholdSqr = __instance.motionSmoothingVelocityThreshold *
                                                         __instance.motionSmoothingVelocityThreshold;
        __instance.motionSmoothingAngularVelocityThresholdSqr = __instance.motionSmoothingAngularVelocityThreshold *
                                                                __instance.motionSmoothingAngularVelocityThreshold;
        __instance.positionCorrectionThresholdSqr =
            __instance.positionCorrectionThreshold * __instance.positionCorrectionThreshold;
        __instance.predictedRigidbody = ((IEntityBehaviourBase)__instance).entity.rigidbody;
        __instance.predictedRigidbodyTransform = ((IEntityBehaviourBase)__instance).entity.transform;
        __instance.tf = __instance.predictedRigidbodyTransform;
        if (!__instance.isServer)
            return false;
        ext.serverGroup.Clear();
        ext.serverGroup.Add(((IEntityBehaviourBase)__instance).entity);
        return false;
    }

    private static ExtendedPredictedRigidbodyGroup GetExtendedData(PredictedRigidbodyGroup basePrg)
    {
        if (!basePrg.TryGetComponent(out ExtendedPredictedRigidbodyGroup ext))
            ext = basePrg.gameObject.AddComponent<ExtendedPredictedRigidbodyGroup>();
        return ext;
    }

    private static List<Entity> GetExtendedGroup(PredictedRigidbodyGroup group)
    {
        var ext = GetExtendedData(group);
        return ext.serverGroup;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerAddToGroup))]
    [HarmonyPrefix]
    public static bool OnServerAddToGroup(PredictedRigidbodyGroup __instance, Entity e)
    {
        if (!NetworkServer.active) return true;
        if (e == ((IEntityBehaviourBase)__instance).entity) return false;
        var list = GetExtendedGroup(__instance);
        if (!list.Contains(((IEntityBehaviourBase)__instance).entity))
            list.Insert(0, ((IEntityBehaviourBase)__instance).entity);
        if (!list.Contains(e))
            list.Add(e);
        __instance.SetDirty();
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerRemoveFromGroup))]
    [HarmonyPrefix]
    public static bool OnServerRemoveFromGroup(PredictedRigidbodyGroup __instance, Entity e)
    {
        if (!NetworkServer.active) return true;
        var list = GetExtendedGroup(__instance);
        list.Remove(e);
        __instance.SetDirty();
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerResetGroup))]
    [HarmonyPrefix]
    public static bool OnServerResetGroup(PredictedRigidbodyGroup __instance)
    {
        if (!NetworkServer.active) return true;

        var list = GetExtendedGroup(__instance);
        list.Clear();
        list.Add(((IEntityBehaviourBase)__instance).entity);
        __instance.SetDirty();
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerClearGroup))]
    [HarmonyPrefix]
    public static bool OnServerClearGroup(PredictedRigidbodyGroup __instance)
    {
        if (!NetworkServer.active) return true;
        var list = GetExtendedGroup(__instance);
        list.Clear();
        __instance.SetDirty();
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerTeleport), [typeof(List<Vector3>), typeof(List<Quaternion>)])]
    [HarmonyPrefix]
    public static bool OnServerTeleport1(PredictedRigidbodyGroup __instance, List<Vector3> positions,
        List<Quaternion> rotations)
    {
        if (!NetworkServer.active) return true;

        var entities = GetExtendedGroup(__instance).ToArray();
        if (entities.Length == 0) return true;

        var posArray = positions.ToArray();
        var rotArray = rotations.ToArray();
        var zeroVelocities = new Vector3[entities.Length];

        StackNetworkHandler.TeleportInternal(in entities, in posArray, in rotArray, in zeroVelocities);

        NetworkServer.SendToAll(new ServerTeleportMsg1
        {
            entities = entities,
            positions = posArray,
            rotations = rotArray
        });

        ClientClearState(entities);
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerTeleport), typeof(List<Vector3>), typeof(Quaternion))]
    [HarmonyPrefix]
    public static bool OnServerTeleport2(PredictedRigidbodyGroup __instance, List<Vector3> positions,
        Quaternion rotation)
    {
        if (!NetworkServer.active) return true;

        var entities = GetExtendedGroup(__instance).ToArray();
        if (entities.Length == 0) return true;

        var posArray = positions.ToArray();
        var rotArray = Enumerable.Repeat(rotation, entities.Length).ToArray();
        var zeroVelocities = new Vector3[entities.Length];

        StackNetworkHandler.TeleportInternal(in entities, in posArray, in rotArray, in zeroVelocities);

        NetworkServer.SendToAll(new ServerTeleportMsg2
        {
            entities = entities,
            positions = posArray,
            rotation = rotation
        });

        ClientClearState(entities);
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ServerTeleport),
        [typeof(List<Vector3>), typeof(Vector3), typeof(Quaternion)])]
    [HarmonyPrefix]
    public static bool OnServerTeleport3(PredictedRigidbodyGroup __instance, List<Vector3> positions, Vector3 velocity,
        Quaternion rotation)
    {
        if (!NetworkServer.active) return true;

        var entities = GetExtendedGroup(__instance).ToArray();
        if (entities.Length == 0) return true;

        var posArray = positions.ToArray();
        var rotArray = Enumerable.Repeat(rotation, entities.Length).ToArray();
        var velArray = Enumerable.Repeat(velocity, entities.Length).ToArray();

        StackNetworkHandler.TeleportInternal(in entities, in posArray, in rotArray, in velArray);

        NetworkServer.SendToAll(new ServerTeleportMsg3
        {
            entities = entities,
            positions = posArray,
            velocity = velocity,
            rotation = rotation
        });

        ClientClearState(entities);
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.OnUpdateSimulationLate))]
    [HarmonyPrefix]
    public static bool OnUpdateSimulationLate(PredictedRigidbodyGroup __instance)
    {
        var ext = GetExtendedData(__instance);
        if (__instance.isServer)
        {
            if (__instance.IsMoving())
            {
                if (!__instance._wasMovingLastFrame)
                {
                    __instance._serverSendRpc = true;
                    __instance.ClearAllDirtyBits();
                }

                __instance._wasMovingLastFrame = true;
            }
            else
                __instance._wasMovingLastFrame = false;
        }

        if (!__instance.isClientOnly || ext.lastRecorded.group.Count == 0)
            return false;
        if (__instance.onlyRecordChanges)
        {
            var flag = false;
            foreach (var rigidbodyGroupEntryState in ext.lastRecorded.group)
            {
                if (!rigidbodyGroupEntryState.entity.Exists())
                    continue;
                rigidbodyGroupEntryState.entity.predictedRigidbodyGroup.tf.GetPositionAndRotation(out var position,
                    out var rotation);
                if (!((rigidbodyGroupEntryState.position - position).sqrMagnitude >=
                      __instance.positionCorrectionThresholdSqr) &&
                    !(Quaternion.Angle(rigidbodyGroupEntryState.rotation, rotation) >=
                      __instance.rotationCorrectionThreshold))
                    continue;
                flag = true;
                break;
            }

            if (!flag)
                return false;
        }

        __instance.RecordState();
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.RecordState))]
    [HarmonyPrefix]
    public static bool RecordState(PredictedRigidbodyGroup __instance)
    {
        var ext = GetExtendedData(__instance);
        var time = NetworkTime.time;
        if (time < __instance.lastRecordTime + (double)__instance.recordInterval)
            return false;
        __instance.lastRecordTime = time;
        var predictedTime = NetworkTime.predictedTime;
        if (predictedTime == ext.lastRecorded.timestamp)
            return false;
        if (ext.stateHistory.Count >= __instance.stateHistoryLimit)
            ext.stateHistory.RemoveAt(0);
        var list = new List<Entity>();
        for (var index = 0; index < ext.lastRecorded.group.Count; ++index)
            list.Add(ext.lastRecorded.group[index].entity);
        var count = ext.stateHistory.Count;
        var rigidbodyGroupState1 = new ExtendedRigidbodyGroupState();
        if (count > 0)
            rigidbodyGroupState1 = ext.stateHistory.Values[count - 1];
        var rigidbodyGroupState2 = new ExtendedRigidbodyGroupState();
        rigidbodyGroupState2.timestamp = predictedTime;
        for (var index = 0; index < list.Count; ++index)
        {
            var entity = list[index];
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            var vector3_1 = Vector3.zero;
            var vector3_2 = Vector3.zero;
            if (entity.Exists())
            {
                var predictedRigidbodyGroup = entity.predictedRigidbodyGroup;
                predictedRigidbodyGroup.tf.GetPositionAndRotation(out position, out rotation);
                vector3_1 = predictedRigidbodyGroup.predictedRigidbody.velocity;
                vector3_2 = predictedRigidbodyGroup.predictedRigidbody.angularVelocity;
            }

            var vector3_3 = Vector3.zero;
            var vector3_4 = Vector3.zero;
            var vector3_5 = Vector3.zero;
            var quaternion = Quaternion.identity;
            if (count > 0)
            {
                var rigidbodyGroupEntryState = rigidbodyGroupState1.group[index];
                vector3_3 = position - rigidbodyGroupEntryState.position;
                vector3_4 = vector3_1 - rigidbodyGroupEntryState.velocity;
                quaternion = (rotation * Quaternion.Inverse(rigidbodyGroupEntryState.rotation)).normalized;
                vector3_5 = vector3_2 - rigidbodyGroupEntryState.angularVelocity;
            }

            rigidbodyGroupState2.group.Add(new RigidbodyGroupEntryState()
            {
                entity = entity,
                positionDelta = vector3_3,
                position = position,
                rotationDelta = quaternion,
                rotation = rotation,
                velocityDelta = vector3_4,
                velocity = vector3_1,
                angularVelocityDelta = vector3_5,
                angularVelocity = vector3_2
            });
        }

        ext.stateHistory.Add(predictedTime, rigidbodyGroupState2);
        ext.lastRecorded = rigidbodyGroupState2;
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.UpdateServer))]
    [HarmonyPrefix]
    public static bool UpdateServer(PredictedRigidbodyGroup __instance)
    {
        var ext = GetExtendedData(__instance);
        if (__instance.reduceSendsWhileIdle)
        {
            var flag = ext.serverGroup.Any(t => t.Exists() && t.predictedRigidbodyGroup.IsMoving());
            __instance.syncInterval = !flag || ext.serverGroup.Count <= 0 ? 1f : __instance.dynamicMinSyncInterval;
        }

        if (ext.serverGroup.Count <= 0)
            return false;
        __instance.SetDirty();
        return false;
    }

    private static ExtendedRigidbodyGroupState GetRigidbodyGroupState(PredictedRigidbodyGroup __instance,
        ExtendedRigidbodyGroupPayload payload, double timestamp)
    {
        var rigidbodyGroupState = new ExtendedRigidbodyGroupState();
        rigidbodyGroupState.timestamp = timestamp;
        for (var index = 0; index < payload.count; ++index)
            rigidbodyGroupState.group.Add(new RigidbodyGroupEntryState
            {
                entity = index != 0 ? payload.entities[index - 1] : ((IEntityBehaviourBase)__instance).entity,
                position = payload.positions[index],
                rotation = payload.rotations[index],
                velocity = payload.velocities[index],
                angularVelocity = payload.angularVelocities[index]
            });
        return rigidbodyGroupState;
    }

    private static void OnReceivedState(PredictedRigidbodyGroup __instance, in ExtendedRigidbodyGroupState groupState)
    {
        var positions = new List<Vector3>();
        var magnitudes = new List<float>();
        var rotations = new List<Quaternion>();
        var index1 = 0;
        List<RigidbodyGroupEntryState> group;
        while (true)
        {
            var num = index1;
            group = groupState.group;
            var count = group.Count;
            if (num < count)
            {
                group = groupState.group;
                var rigidbodyGroupEntryState = group[index1];
                var position = Vector3.zero;
                var rotation = Quaternion.identity;
                if (rigidbodyGroupEntryState.entity.Exists())
                    rigidbodyGroupEntryState.entity.predictedRigidbodyGroup.predictedRigidbodyTransform
                        .GetPositionAndRotation(out position, out rotation);
                positions.Add(position);
                rotations.Add(rotation);
                magnitudes.Add(Vector3.SqrMagnitude(rigidbodyGroupEntryState.position - position));
                ++index1;
            }
            else
                break;
        }

        if (__instance.compareLastFirst)
        {
            var flag = true;
            var index2 = 0;
            while (true)
            {
                var num = index2;
                group = groupState.group;
                var count = group.Count;
                if (num < count)
                {
                    if (magnitudes[index2] < __instance.positionCorrectionThresholdSqr)
                    {
                        group = groupState.group;
                        if (Quaternion.Angle(group[index2].rotation, rotations[index2]) <
                            __instance.rotationCorrectionThreshold)
                            ++index2;
                        else
                        {
                            flag = false;
                            break;
                        }
                    }
                    else
                    {
                        flag = false;
                        break;
                    }
                }
                else
                    break;
            }

            if (flag)
                return;
        }

        __instance.RecordState();
        var ext = GetExtendedData(__instance);
        if (ext.stateHistory.Count < 2)
            return;
        var rigidbodyGroupState1 = ext.stateHistory.Values[0];
        var rigidbodyGroupState2 = ext.stateHistory.Values[ext.stateHistory.Count - 1];
        if (groupState.timestamp < rigidbodyGroupState1.timestamp)
        {
            if (ext.stateHistory.Count >= __instance.stateHistoryLimit)
                Debug.LogWarning(
                    $"Hard correcting client object {__instance.name} because the client is too far behind the server. History of size={ext.stateHistory.Count} @ t={groupState.timestamp:F3} oldest={rigidbodyGroupState1.timestamp:F3} newest={rigidbodyGroupState2.timestamp:F3}. This would cause the client to be out of sync as long as it's behind.");
            ApplyState(__instance, in groupState);
        }
        else if (rigidbodyGroupState2.timestamp < groupState.timestamp)
        {
            if (!magnitudes.Any(t => t >= __instance.positionCorrectionThresholdSqr))
                return;
            ApplyState(__instance, in groupState);
        }
        else
        {
            if (!Prediction.Sample(ext.stateHistory, groupState.timestamp, out var a, out var b, out var afterIndex,
                    out var t))
            {
                Debug.LogError(
                    $"Failed to sample history of size={ext.stateHistory.Count} @ t={groupState.timestamp:F3} oldest={rigidbodyGroupState1.timestamp:F3} newest={rigidbodyGroupState2.timestamp:F3}. This should never happen because the timestamp is within history.");
                ApplyState(__instance, in groupState);
            }
            else
            {
                var rigidbodyGroupState3 = ExtendedRigidbodyGroupState.Interpolate(in a, in b, (float)t);
                var flag = false;
                var index4 = 0;
                while (true)
                {
                    var num = index4;
                    group = groupState.group;
                    var count = group.Count;
                    if (num < count)
                    {
                        group = groupState.group;
                        var rigidbodyGroupEntryState1 = group[index4];
                        var rigidbodyGroupEntryState2 = rigidbodyGroupState3.group[index4];
                        if (Vector3.SqrMagnitude(
                                rigidbodyGroupEntryState1.position - rigidbodyGroupEntryState2.position) <
                            __instance.positionCorrectionThresholdSqr
                            && Quaternion.Angle(rigidbodyGroupEntryState1.rotation,
                                rigidbodyGroupEntryState2.rotation) < __instance.rotationCorrectionThreshold)
                            ++index4;
                        else
                        {
                            flag = true;
                            break;
                        }
                    }
                    else
                        break;
                }

                if (!flag)
                    return;
                ApplyState(__instance,
                    CorrectHistory(ext.stateHistory, __instance.stateHistoryLimit, groupState, a, b, afterIndex));
            }
        }
    }

    private static void ApplyState(PredictedRigidbodyGroup __instance, in ExtendedRigidbodyGroupState groupState)
    {
        var flag = false;
        var num = __instance.snapThreshold * __instance.snapThreshold;
        for (var index = 0; index < groupState.group.Count; ++index)
        {
            var entity = groupState.group[index].entity;
            if (!entity.Exists())
                continue;
            var predictedRigidbody = entity.predictedRigidbodyGroup.predictedRigidbody;
            var vector3 = predictedRigidbody.velocity;
            if (!(vector3.sqrMagnitude <= (double)num))
                continue;
            vector3 = predictedRigidbody.angularVelocity;
            if (!(vector3.sqrMagnitude <= (double)num))
                continue;
            flag = true;
            break;
        }

        if (flag)
        {
            foreach (var rigidbodyGroupEntryState in groupState.group)
            {
                if (!rigidbodyGroupEntryState.entity.Exists())
                    continue;
                var predictedRigidbody = rigidbodyGroupEntryState.entity.predictedRigidbodyGroup.predictedRigidbody;
                predictedRigidbody.position = rigidbodyGroupEntryState.position;
                predictedRigidbody.rotation = rigidbodyGroupEntryState.rotation;
                if (predictedRigidbody.isKinematic)
                    continue;
                predictedRigidbody.velocity = rigidbodyGroupEntryState.velocity;
                predictedRigidbody.angularVelocity = rigidbodyGroupEntryState.angularVelocity;
            }

            var ext = GetExtendedData(__instance);
            ext.stateHistory.Clear();
            ext.stateHistory.Add(groupState.timestamp, groupState);
        }
        else
        {
            foreach (var rigidbodyGroupEntryState in groupState.group)
            {
                if (!rigidbodyGroupEntryState.entity.Exists())
                    continue;
                var predictedRigidbody = rigidbodyGroupEntryState.entity.predictedRigidbodyGroup.predictedRigidbody;
                predictedRigidbody.MovePosition(rigidbodyGroupEntryState.position);
                predictedRigidbody.MoveRotation(rigidbodyGroupEntryState.rotation);
                if (predictedRigidbody.isKinematic)
                    continue;
                predictedRigidbody.velocity = rigidbodyGroupEntryState.velocity;
                predictedRigidbody.angularVelocity = rigidbodyGroupEntryState.angularVelocity;
            }
        }
    }

    private static ExtendedRigidbodyGroupState CorrectHistory(
        SortedList<double, ExtendedRigidbodyGroupState> history,
        int stateHistoryLimit,
        ExtendedRigidbodyGroupState corrected,
        ExtendedRigidbodyGroupState before,
        ExtendedRigidbodyGroupState after,
        int afterIndex)
    {
        if (history.Count >= stateHistoryLimit)
        {
            history.RemoveAt(0);
            --afterIndex;
        }

        var num1 = after.timestamp - before.timestamp;
        var num2 = after.timestamp - corrected.timestamp;
        var t = num1 != 0.0 ? num2 / num1 : 0.0;
        for (var index = 0; index < after.group.Count; ++index)
        {
            var rigidbodyGroupEntryState = after.group[index];
            rigidbodyGroupEntryState.positionDelta =
                Vector3.Lerp(Vector3.zero, rigidbodyGroupEntryState.positionDelta, (float)t);
            rigidbodyGroupEntryState.velocityDelta =
                Vector3.Lerp(Vector3.zero, rigidbodyGroupEntryState.velocityDelta, (float)t);
            rigidbodyGroupEntryState.angularVelocityDelta = Vector3.Lerp(Vector3.zero,
                rigidbodyGroupEntryState.angularVelocityDelta, (float)t);
            rigidbodyGroupEntryState.rotationDelta = Quaternion
                .Slerp(Quaternion.identity, rigidbodyGroupEntryState.rotationDelta, (float)t).normalized;
            after.group[index] = rigidbodyGroupEntryState;
        }

        history[after.timestamp] = after;
        var rigidbodyGroupState1 = corrected;
        for (var index1 = afterIndex; index1 < history.Count; ++index1)
        {
            var key = history.Keys[index1];
            var rigidbodyGroupState2 = history.Values[index1];
            for (var index2 = 0; index2 < rigidbodyGroupState2.group.Count; ++index2)
            {
                var rigidbodyGroupEntryState1 = rigidbodyGroupState2.group[index2];
                var rigidbodyGroupEntryState2 = rigidbodyGroupState1.group[index2];
                rigidbodyGroupEntryState1.position =
                    rigidbodyGroupEntryState2.position + rigidbodyGroupEntryState1.positionDelta;
                rigidbodyGroupEntryState1.velocity =
                    rigidbodyGroupEntryState2.velocity + rigidbodyGroupEntryState1.velocityDelta;
                rigidbodyGroupEntryState1.angularVelocity = rigidbodyGroupEntryState2.angularVelocity +
                                                            rigidbodyGroupEntryState1.angularVelocityDelta;
                rigidbodyGroupEntryState1.rotation =
                    (rigidbodyGroupEntryState1.rotationDelta * rigidbodyGroupEntryState2.rotation).normalized;
                rigidbodyGroupState2.group[index2] = rigidbodyGroupEntryState1;
            }

            history[key] = rigidbodyGroupState2;
            rigidbodyGroupState1 = rigidbodyGroupState2;
        }

        return rigidbodyGroupState1;
    }

    public static void OnSyncMsg(ExtendedRigidbodyGroupSyncMsg msg)
    {
        if (!NetworkClient.spawned.TryGetValue(msg.netId, out var identity) || identity == null)
            return;
        if (!identity.TryGetComponent(out PredictedRigidbodyGroup prg))
            return;
        if (prg.isServer || msg.tick <= prg._lastTick)
            return;
        prg._lastTick = msg.tick;
        ProcessPayload(prg, msg.payload);
    }

    private static void ProcessPayload(PredictedRigidbodyGroup prg, ExtendedRigidbodyGroupPayload payload)
    {
        var ext = GetExtendedData(prg);
        var remoteTimeStamp = NetworkClient.connection.remoteTimeStamp;
        int count = payload.count;

        if (count != ext.lastRecorded.group.Count)
        {
            ext.stateHistory.Clear();
            prg.lastRecordTime = 0.0;
            ext.lastRecorded = new ExtendedRigidbodyGroupState();
        }

        if (count <= 0)
            return;

        double dt = payload.dt;
        var timestamp = remoteTimeStamp + dt;
        if (prg.oneFrameAhead)
            timestamp += dt;

        var groupState = GetRigidbodyGroupState(prg, payload, timestamp);

        var entitiesChanged = false;
        if (count == ext.lastRecorded.group.Count)
        {
            for (var index = 0; index < count; ++index)
            {
                if (groupState.group[index].entity != ext.lastRecorded.group[index].entity)
                {
                    entitiesChanged = true;
                    break;
                }
            }
        }
        else
            entitiesChanged = true;

        if (entitiesChanged)
        {
            ext.stateHistory.Clear();
            prg.lastRecordTime = 0.0;
            ext.lastRecorded = new ExtendedRigidbodyGroupState();
            for (var index = 0; index < count; ++index)
                ext.lastRecorded.group.Add(new RigidbodyGroupEntryState { entity = groupState.group[index].entity });
        }

        OnReceivedState(prg, in groupState);
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.OnUpdatePresentationLate))]
    [HarmonyPrefix]
    public static bool OnUpdatePresentationLate(PredictedRigidbodyGroup __instance)
    {
        if (!__instance._serverSendRpc)
            return false;
        __instance._serverSendRpc = false;
        if (!NetworkServer.active)
            return false;

        var msg = new ExtendedRigidbodyGroupSyncMsg
        {
            netId = __instance.netId,
            tick = __instance._serverTick++,
            payload = ServerGetPayload(__instance)
        };
        NetworkServer.SendToReadyObservers(__instance.netIdentity, msg, true, Channels.Unreliable);
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.OnSerialize))]
    [HarmonyPrefix]
    public static bool OnSerialize(PredictedRigidbodyGroup __instance, NetworkWriter writer, bool initialState)
    {
        if (initialState)
            writer.WriteExtendedRigidbodyGroupPayload(ServerGetPayload(__instance));
        __instance._serverSendRpc = true;
        return false;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.OnDeserialize))]
    [HarmonyPrefix]
    public static bool OnDeserialize(PredictedRigidbodyGroup __instance, NetworkReader reader, bool initialState)
    {
        if (!initialState)
            return false;
        ProcessPayload(__instance, reader.ReadExtendedRigidbodyGroupPayload());
        return false;
    }

    private static ExtendedRigidbodyGroupPayload ServerGetPayload(PredictedRigidbodyGroup prg)
    {
        var payload = ExtendedRigidbodyGroupPayload.Create();
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[Server] ServerGetPayload called when server was not active");
            return payload;
        }

        var group = GetExtendedGroup(prg);
        var n = Math.Min(group.Count, byte.MaxValue);
        payload.count = (byte)n;
        if (n == 0)
            return payload;

        payload.dt = Time.deltaTime;

        for (var index = 0; index < n; ++index)
        {
            var entity = group[index];
            var exists = entity.Exists();

            byte flags = 0;
            var moving = false;
            if (exists)
            {
                var prbg = entity.predictedRigidbodyGroup;
                moving = prbg.IsMoving();
                if (moving) flags |= ExtendedRigidbodyGroupPayload.MovingBit;
                if (prbg.constrainedUpRight) flags |= ExtendedRigidbodyGroupPayload.UprightBit;
            }

            payload.flags.Add(flags);

            if (index > 0)
                payload.entities.Add(entity);

            if (exists)
            {
                entity.predictedRigidbodyGroup.tf.GetPositionAndRotation(out var position, out var rotation);
                payload.positions.Add(position);
                payload.rotations.Add(rotation);
                if (moving)
                {
                    var rb = entity.rigidbody;
                    payload.velocities.Add(rb.velocity);
                    payload.angularVelocities.Add(rb.angularVelocity);
                }
                else
                {
                    payload.velocities.Add(Vector3.zero);
                    payload.angularVelocities.Add(Vector3.zero);
                }
            }
            else
            {
                payload.positions.Add(Vector3.zero);
                payload.rotations.Add(Quaternion.identity);
                payload.velocities.Add(Vector3.zero);
                payload.angularVelocities.Add(Vector3.zero);
            }
        }

        return payload;
    }

    private static void ClearExtendedState(PredictedRigidbodyGroup prg)
    {
        var ext = GetExtendedData(prg);
        ext.lastRecorded = new ExtendedRigidbodyGroupState();
        ext.stateHistory.Clear();
        prg.lastRecordTime = 0.0;
    }

    [HarmonyPatch(nameof(PredictedRigidbodyGroup.ClientClearState))]
    [HarmonyPrefix]
    public static bool ClientClearState_Prefix(PredictedRigidbodyGroup __instance)
    {
        if (!NetworkClient.active) return true;
        ClearExtendedState(__instance);
        return false;
    }

    public static void ClientClearState(Entity[] entities)
    {
        foreach (var e in entities)
        {
            if (!e.Exists()) continue;
            ClearExtendedState(e.predictedRigidbodyGroup);
        }
    }
}