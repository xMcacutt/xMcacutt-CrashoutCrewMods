using System;
using System.Collections.Generic;
using Aggro.Core;
using UnityEngine;

namespace CrashoutCrew_SkysTheLimit;

public class ExtendedPredictedRigidbodyGroup : MonoBehaviour
{
    public ExtendedRigidbodyGroupState lastRecorded = new();
    public List<Entity> serverGroup = new();
    public readonly SortedList<double, ExtendedRigidbodyGroupState> stateHistory = new();
}