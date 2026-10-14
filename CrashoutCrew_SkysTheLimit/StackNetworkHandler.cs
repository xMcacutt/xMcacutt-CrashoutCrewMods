using System.Linq;
using Aggro.Core;
using Aggro.Core.Networking;
using CrashoutCrew_SkysTheLimit.Patches;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

namespace CrashoutCrew_SkysTheLimit;

public static class StackNetworkHandler
{
    private static bool _init;
    public static void Init()
    {
        if (!NetworkClient.active)
            return;
        if (!_init)
        {
            _init = true;
            RegisterSerializers();
            NetworkClient.RegisterHandler<ServerTeleportMsg1>(OnClientReceiveTeleport1);
            NetworkClient.RegisterHandler<ServerTeleportMsg2>(OnClientReceiveTeleport2);
            NetworkClient.RegisterHandler<ServerTeleportMsg3>(OnClientReceiveTeleport3);
            NetworkClient.RegisterHandler<ExtendedRigidbodyGroupSyncMsg>(PredictedRigidbodyGroup_Patches.OnSyncMsg);
            NetworkClient.RegisterHandler<StackContextMsg>(Grabbable_Patches.OnStackContext);

        }
        else
        {
            NetworkClient.ReplaceHandler<ServerTeleportMsg1>(OnClientReceiveTeleport1);
            NetworkClient.ReplaceHandler<ServerTeleportMsg2>(OnClientReceiveTeleport2);
            NetworkClient.ReplaceHandler<ServerTeleportMsg3>(OnClientReceiveTeleport3);
            NetworkClient.ReplaceHandler<ExtendedRigidbodyGroupSyncMsg>(PredictedRigidbodyGroup_Patches.OnSyncMsg);
            NetworkClient.ReplaceHandler<StackContextMsg>(Grabbable_Patches.OnStackContext);

        }
    }
    
    private static void RegisterSerializers()
    {
        Writer<ServerTeleportMsg1>.write = WriteServerTeleportMsg1;
        Reader<ServerTeleportMsg1>.read = ReadServerTeleportMsg1;

        Writer<ServerTeleportMsg2>.write = WriteServerTeleportMsg2;
        Reader<ServerTeleportMsg2>.read = ReadServerTeleportMsg2;

        Writer<ServerTeleportMsg3>.write = WriteServerTeleportMsg3;
        Reader<ServerTeleportMsg3>.read = ReadServerTeleportMsg3;
        
        Writer<ExtendedRigidbodyGroupSyncMsg>.write = (w, m) =>
        {
            w.WriteUInt(m.netId);
            w.WriteVarUInt(m.tick);
            w.WriteExtendedRigidbodyGroupPayload(m.payload);
        };
        Reader<ExtendedRigidbodyGroupSyncMsg>.read = r =>
        {
            var m = new ExtendedRigidbodyGroupSyncMsg();
            m.netId = r.ReadUInt();
            m.tick = r.ReadVarUInt();
            m.payload = r.ReadExtendedRigidbodyGroupPayload();
            return m;
        };

        Writer<StackContextMsg>.write = (w, m) =>
        {
            w.WriteUInt(m.netId);
            w.WriteEntityArray(m.stack);
        };
        Reader<StackContextMsg>.read = r =>
        {
            var m = new StackContextMsg();
            m.netId = r.ReadUInt();
            m.stack = r.ReadEntityArray();
            return m;
        };
    }

    public static void WriteExtendedRigidbodyGroupPayload(this NetworkWriter writer,
        ExtendedRigidbodyGroupPayload payload)
    {
        writer.WriteByte(payload.count);
        if (payload.count == 0)
            return;
        writer.WriteFloat(payload.dt);

        for (var index = 0; index < payload.count; ++index)
        {
            var f = payload.flags[index];
            var moving = (f & ExtendedRigidbodyGroupPayload.MovingBit) != 0;
            var upright = (f & ExtendedRigidbodyGroupPayload.UprightBit) != 0;

            writer.WriteByte(f);
            if (index > 0)
                writer.WriteEntity(payload.entities[index - 1]);

            var position = payload.positions[index];
            writer.WriteUShort(Compression.ScaleFloatToUShort(position.x, -200f, 500f, 0, ushort.MaxValue));
            writer.WriteUShort(Compression.ScaleFloatToUShort(position.y, -200f, 500f, 0, ushort.MaxValue));
            writer.WriteUShort(Compression.ScaleFloatToUShort(position.z, -200f, 500f, 0, ushort.MaxValue));

            if (upright)
            {
                var rotation = payload.rotations[index];
                rotation.Normalize();
                writer.WriteUShort(Compression.ScaleFloatToUShort(
                    (float)(Mathf.Sign(rotation.y) * (double)Mathf.Acos(rotation.w) * 2.0),
                    -6.2831855f, 6.2831855f, 0, ushort.MaxValue));
                if (moving)
                {
                    var v = payload.velocities[index];
                    writer.WriteUShort((ushort)math.f32tof16(v.x));
                    writer.WriteUShort((ushort)math.f32tof16(v.y));
                    writer.WriteUShort((ushort)math.f32tof16(v.z));
                    writer.WriteUShort((ushort)math.f32tof16(payload.angularVelocities[index].y));
                }
            }
            else
            {
                writer.WriteUInt(Compression.CompressQuaternion(payload.rotations[index]));
                if (moving)
                {
                    var v = payload.velocities[index];
                    writer.WriteUShort((ushort)math.f32tof16(v.x));
                    writer.WriteUShort((ushort)math.f32tof16(v.y));
                    writer.WriteUShort((ushort)math.f32tof16(v.z));
                    var av = payload.angularVelocities[index];
                    writer.WriteUShort((ushort)math.f32tof16(av.x));
                    writer.WriteUShort((ushort)math.f32tof16(av.y));
                    writer.WriteUShort((ushort)math.f32tof16(av.z));
                }
            }
        }
    }
    public static ExtendedRigidbodyGroupPayload ReadExtendedRigidbodyGroupPayload(this NetworkReader reader)
    {
        var count = reader.ReadByte();
        var payload = ExtendedRigidbodyGroupPayload.Create();
        payload.count = count;
        if (count == 0)
            return payload;
        payload.dt = reader.ReadFloat();

        for (var index = 0; index < count; ++index)
        {
            var f = reader.ReadByte();
            var moving = (f & ExtendedRigidbodyGroupPayload.MovingBit) != 0;
            var upright = (f & ExtendedRigidbodyGroupPayload.UprightBit) != 0;
            payload.flags.Add(f);

            if (index > 0)
                payload.entities.Add(reader.ReadEntity());

            var px = Compression.ScaleUShortToFloat(reader.ReadUShort(), 0, ushort.MaxValue, -200f, 500f);
            var py = Compression.ScaleUShortToFloat(reader.ReadUShort(), 0, ushort.MaxValue, -200f, 500f);
            var pz = Compression.ScaleUShortToFloat(reader.ReadUShort(), 0, ushort.MaxValue, -200f, 500f);
            payload.positions.Add(new Vector3(px, py, pz));

            if (upright)
            {
                var angle = Compression.ScaleUShortToFloat(reader.ReadUShort(), 0, ushort.MaxValue,
                    -6.2831855f, 6.2831855f);
                payload.rotations.Add(new Quaternion(0f, Mathf.Sin(angle / 2f), 0f, Mathf.Cos(angle / 2f)));
                if (moving)
                {
                    var vx = math.f16tof32(reader.ReadUShort());
                    var vy = math.f16tof32(reader.ReadUShort());
                    var vz = math.f16tof32(reader.ReadUShort());
                    payload.velocities.Add(new Vector3(vx, vy, vz));
                    payload.angularVelocities.Add(new Vector3(0f, math.f16tof32(reader.ReadUShort()), 0f));
                }
                else
                {
                    payload.velocities.Add(Vector3.zero);
                    payload.angularVelocities.Add(Vector3.zero);
                }
            }
            else
            {
                payload.rotations.Add(Compression.DecompressQuaternion(reader.ReadUInt()));
                if (moving)
                {
                    var vx = math.f16tof32(reader.ReadUShort());
                    var vy = math.f16tof32(reader.ReadUShort());
                    var vz = math.f16tof32(reader.ReadUShort());
                    payload.velocities.Add(new Vector3(vx, vy, vz));
                    var ax = math.f16tof32(reader.ReadUShort());
                    var ay = math.f16tof32(reader.ReadUShort());
                    var az = math.f16tof32(reader.ReadUShort());
                    payload.angularVelocities.Add(new Vector3(ax, ay, az));
                }
                else
                {
                    payload.velocities.Add(Vector3.zero);
                    payload.angularVelocities.Add(Vector3.zero);
                }
            }
        }

        return payload;
    }
    
    private static void WriteEntityArray(this NetworkWriter writer, Entity[] entities)
    {
        writer.WriteInt(entities.Length);
        foreach (var t in entities)
            writer.WriteEntity(t);
    }

    private static Entity[] ReadEntityArray(this NetworkReader reader)
    {
        var length = reader.ReadInt();
        var array = new Entity[length];
        for (var i = 0; i < length; i++)
            array[i] = reader.ReadEntity();
        return array;
    }

    private static void WriteServerTeleportMsg1(this NetworkWriter writer, ServerTeleportMsg1 msg)
    {
        writer.WriteEntityArray(msg.entities);
        writer.WriteArray(msg.positions);
        writer.WriteArray(msg.rotations);
    }

    private static ServerTeleportMsg1 ReadServerTeleportMsg1(this NetworkReader reader)
    {
        return new ServerTeleportMsg1
        {
            entities = reader.ReadEntityArray(),
            positions = reader.ReadArray<Vector3>(),
            rotations = reader.ReadArray<Quaternion>()
        };
    }

    private static void WriteServerTeleportMsg2(this NetworkWriter writer, ServerTeleportMsg2 msg)
    {
        writer.WriteEntityArray(msg.entities);
        writer.WriteArray(msg.positions);
        writer.WriteQuaternion(msg.rotation);
    }

    private static ServerTeleportMsg2 ReadServerTeleportMsg2(this NetworkReader reader)
    {
        return new ServerTeleportMsg2
        {
            entities = reader.ReadEntityArray(),
            positions = reader.ReadArray<Vector3>(),
            rotation = reader.ReadQuaternion()
        };
    }

    private static void WriteServerTeleportMsg3(this NetworkWriter writer, ServerTeleportMsg3 msg)
    {
        writer.WriteEntityArray(msg.entities);
        writer.WriteArray(msg.positions);
        writer.WriteVector3(msg.velocity);
        writer.WriteQuaternion(msg.rotation);
    }

    private static ServerTeleportMsg3 ReadServerTeleportMsg3(this NetworkReader reader)
    {
        return new ServerTeleportMsg3
        {
            entities = reader.ReadEntityArray(),
            positions = reader.ReadArray<Vector3>(),
            velocity = reader.ReadVector3(),
            rotation = reader.ReadQuaternion()
        };
    }
    
    public static void TeleportInternal(
        in Entity[] entities,
        in Vector3[] positions,
        in Quaternion[] rotations,
        in Vector3[] velocities)
    {
        for (var index = 0; index < entities.Length; ++index)
        {
            var entity = entities[index];
            var position = positions[index];
            var quaternion = rotations[index];
            if (!entity.Exists()) 
                continue;
            var rigidbody = entity.rigidbody;
            rigidbody.position = position;
            rigidbody.rotation = PhysicsUtil.Constrain(quaternion, rigidbody.constraints);
            if (!rigidbody.isKinematic)
            {
                rigidbody.velocity = velocities[index];
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.WakeUp();
            }
            entity.transform.SetPositionAndRotation(position, quaternion);
        }
    }
    
    private static void OnClientReceiveTeleport1(ServerTeleportMsg1 msg)
    {
        if (NetworkServer.active) return;
        TeleportInternal(in msg.entities, in msg.positions, in msg.rotations, new Vector3[msg.entities.Length]);
        PredictedRigidbodyGroup_Patches.ClientClearState(msg.entities);
    }
    
    private static void OnClientReceiveTeleport2(ServerTeleportMsg2 msg)
    {
        if (NetworkServer.active) return;
        var rotations = Enumerable.Repeat(msg.rotation, msg.entities.Length).ToArray();
        TeleportInternal(in msg.entities, in msg.positions, in rotations, new Vector3[msg.entities.Length]);
        PredictedRigidbodyGroup_Patches.ClientClearState(msg.entities);
    }
    
    private static void OnClientReceiveTeleport3(ServerTeleportMsg3 msg)
    {
        if (NetworkServer.active) return;
        var rotations = Enumerable.Repeat(msg.rotation, msg.entities.Length).ToArray();
        var velocities = Enumerable.Repeat(msg.velocity, msg.entities.Length).ToArray();
        TeleportInternal(in msg.entities, in msg.positions, in rotations, velocities);
        PredictedRigidbodyGroup_Patches.ClientClearState(msg.entities);
    }
}

public struct ServerTeleportMsg1 : NetworkMessage
{
    public Entity[] entities;
    public Vector3[] positions;
    public Quaternion[] rotations;
}

public struct ServerTeleportMsg2 : NetworkMessage
{
    public Entity[] entities;
    public Vector3[] positions;
    public Quaternion rotation;
}

public struct ServerTeleportMsg3 : NetworkMessage
{
    public Entity[] entities;
    public Vector3[] positions;
    public Vector3 velocity;
    public Quaternion rotation;
}

public struct StackContextMsg : NetworkMessage
{
    public uint netId;
    public Entity[] stack;
}

public struct ExtendedRigidbodyGroupSyncMsg : NetworkMessage
{
    public uint netId;
    public uint tick;
    public ExtendedRigidbodyGroupPayload payload;
}