using System.Collections.Generic;
using Aggro.Core;
using UnityEngine;

namespace CrashoutCrew_SkysTheLimit;

public struct ExtendedRigidbodyGroupPayload
{
    public const byte MovingBit = 1;
    public const byte UprightBit = 2;

    public byte count;
    public float dt;
    public List<byte> flags;
    public List<Entity> entities;
    public List<Vector3> positions;
    public List<Quaternion> rotations;
    public List<Vector3> velocities;
    public List<Vector3> angularVelocities;

    public static ExtendedRigidbodyGroupPayload Create()
    {
        return new ExtendedRigidbodyGroupPayload
        {
            flags = [],
            entities = [],
            positions = [],
            rotations = [],
            velocities = [],
            angularVelocities = []
        };
    }
}
