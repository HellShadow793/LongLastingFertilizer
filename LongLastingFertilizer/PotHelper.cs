#if IL2CPP
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
#else
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
#endif

namespace LongLastingFertilizer;

internal static class PotHelper {
  internal static string GetId(Pot pot) {
    return pot.GUID.ToString();
  }

  internal static bool HasSoilRemaining(Pot pot) {
    return pot.CurrentSoil.Uses > 0;
  }

  internal static bool HasAdditives(Pot pot) {
    return pot.AppliedAdditives is { Count: > 0 };
  }

  internal static bool IsFertilizer(AdditiveDefinition additive) {
    return additive.YieldMultiplier != 0f || additive.QualityChange != 0f;
  }
}