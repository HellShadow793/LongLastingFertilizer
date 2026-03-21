#if IL2CPP
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
#else
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
#endif
using System.Reflection;

namespace LongLastingFertilizer;

/// <summary>
///   Centralized helpers for pot identification, soil checks, and fertilizer detection.
/// </summary>
internal static class PotHelper {
  internal static string GetId(Pot pot) {
    return pot.GUID.ToString();
  }

  internal static bool HasSoilRemaining(Pot pot) {
#if IL2CPP
    return pot._remainingSoilUses > 0;
#else
    var field = typeof(GrowContainer).GetField("_remainingSoilUses",
      BindingFlags.NonPublic | BindingFlags.Instance);
    var value = (int)(field?.GetValue(pot) ?? 0);

    return value > 0;
#endif
  }

  internal static bool HasAdditives(Pot pot) {
    return pot.AppliedAdditives is { Count: > 0 };
  }

  internal static bool IsFertilizer(AdditiveDefinition additive) {
    return additive.YieldMultiplier != 0f || additive.QualityChange != 0f;
  }
}