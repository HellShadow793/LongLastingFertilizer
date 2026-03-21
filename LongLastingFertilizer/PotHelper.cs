using Il2CppScheduleOne.ObjectScripts;

namespace LongLastingFertilizer;

internal static class PotHelper {
  internal static string GetId(Pot pot) {
    return pot.GUID.ToString();
  }

  internal static bool HasSoilRemaining(Pot pot) {
    return pot._remainingSoilUses > 0;
  }

  internal static bool HasAdditives(Pot pot) {
    return pot.AppliedAdditives is { Count: > 0 };
  }
}