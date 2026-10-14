using System.Collections.Generic;
using Aggro.Core;

namespace CrashoutCrew_SkysTheLimit;

public struct ExtendedRigidbodyGroupState()
{
    public double timestamp = 0.0;
    public List<RigidbodyGroupEntryState> group = [];

    public static ExtendedRigidbodyGroupState Interpolate(
        in ExtendedRigidbodyGroupState a,
        in ExtendedRigidbodyGroupState b,
        float t)
    {
        var result = new ExtendedRigidbodyGroupState();
        var count = a.group.Count;
        for (var index = 0; index < count; ++index)
        {
            var a1 = a.group[index];
            var b1 = b.group[index];
            result.group.Add(RigidbodyGroupEntryState.Interpolate(in a1, in b1, t));
        }
        return result;
    }
}