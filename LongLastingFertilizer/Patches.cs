using System;
using System.Collections;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
#if IL2CPP
using Il2CppFishNet;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Persistence;

#else
using ScheduleOne.Employees;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence;
#endif

namespace LongLastingFertilizer;

// ------------------------------------------------------------------
//  Harvest: save fertilizer state or clean up depleted soil.
// ------------------------------------------------------------------
[HarmonyPatch(typeof(Pot), "OnPlantFullyHarvested")]
internal static class Patch_Harvest {
  [HarmonyPrefix]
#if IL2CPP
  public static void Prefix(Pot __instance) {
    try {
      if (__instance?.Plant == null) return;
#else
  public static void Prefix(object __instance) {
    try {
      var pot = __instance as Pot;

      if (pot?.Plant == null) return;
#endif
      var id = PotHelper.GetId(__instance);

      if (__instance._remainingSoilUses > 1 && PotHelper.HasAdditives(__instance)) {
        FertilizerStore.CaptureState(__instance, id);
      }
      else {
        FertilizerStore.Remove(id, "soil depleted at harvest");
      }
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Harvest prefix error: {ex.Message}");
    }
  }
}

// ------------------------------------------------------------------
//  Planting: restore saved effects to the new plant.
// ------------------------------------------------------------------
#if IL2CPP
[HarmonyPatch(typeof(Pot), "PlantSeed_Server")]
#else
[HarmonyPatch(typeof(Pot), "RpcLogic___PlantSeed_Server_606697822")]
#endif
internal static class Patch_Plant {
  [HarmonyPostfix]
#if IL2CPP
  public static void Postfix(Pot __instance) {
    var pot = __instance;

    if (pot == null || !InstanceFinder.IsServer) return;
#else
  public static void Postfix(object __instance, string seedID, float normalizedSeedProgress) {
    var pot = __instance as Pot;

    if (pot == null || !InstanceFinder.IsServer) return;
#endif
    var id = PotHelper.GetId(pot);

    if (!FertilizerStore.TryGet(id, out var data)) return;

    if (pot.Plant != null) {
      RestorePlantValues(pot, data, id);
    }
    else {
      MelonCoroutines.Start(WaitThenRestore(pot, data, id));
    }
  }

  private static void RestorePlantValues(Pot pot, PotFertilizerData data, string potId) {
    try {
#if IL2CPP
      pot.Plant.YieldMultiplier = data.YieldLevel;
      pot.Plant.QualityLevel = data.QualityLevel;
#else
      var yieldField = typeof(Plant).GetField("<YieldMultiplier>k__BackingField",
        BindingFlags.NonPublic | BindingFlags.Instance);
      var qualityField = typeof(Plant).GetField("<QualityLevel>k__BackingField",
        BindingFlags.NonPublic | BindingFlags.Instance);

      yieldField?.SetValue(pot.Plant, data.YieldLevel);
      qualityField?.SetValue(pot.Plant, data.QualityLevel);
#endif

      Melon<Mod>.Logger.Msg(
        $"Restored yield={data.YieldLevel:F2}, quality={data.QualityLevel:F2} on pot {potId}");
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Restore error on pot {potId}: {ex.Message}");
    }
  }

  private static IEnumerator WaitThenRestore(Pot pot, PotFertilizerData data, string potId) {
    for (var i = 0; i < 100 && pot?.Plant == null; i++)
      yield return null;

    if (pot?.Plant == null) {
      Melon<Mod>.Logger.Warning($"Plant never appeared for pot {potId}, skipping restore.");

      yield break;
    }

    for (var i = 0; i < 10; i++)
      yield return null;

    RestorePlantValues(pot, data, potId);
  }
}

// ------------------------------------------------------------------
//  Block manual fertilizer re-application on pots with saved data.
// ------------------------------------------------------------------
[HarmonyPatch(typeof(Pot), "CanApplyAdditive")]
internal static class Patch_CanApply {
  [HarmonyPrefix]
#if IL2CPP
  public static bool Prefix(Pot __instance, AdditiveDefinition additiveDef,
    ref string invalidReason, ref bool __result) {
    if (__instance == null || additiveDef == null) return true;

    if (!PotHelper.IsFertilizer(additiveDef)) return true;
#else
  public static bool Prefix(object __instance, object additiveDef,
    ref string invalidReason, ref bool __result) {
    var pot = __instance as Pot;
    var def = additiveDef as AdditiveDefinition;

    if (pot == null || def == null) return true;
    if (!PotHelper.IsFertilizer(def)) return true;
#endif

    var id = PotHelper.GetId(__instance);

    if (!FertilizerStore.ValidateOrClean(__instance, id, "CanApplyAdditive")) {
      return true;
    }

    invalidReason = "This soil is already fertilized!";
    __result = false;

    return false;
  }
}

// ------------------------------------------------------------------
//  Prevent botanist NPCs from fertilizing pots with saved data.
// ------------------------------------------------------------------
[HarmonyPatch(typeof(Botanist), "GetGrowContainersForAdditives")]
internal static class Patch_Botanist {
  [HarmonyPostfix]
#if IL2CPP
  public static void Postfix(ref List<GrowContainer> __result) {
    if (__result == null || __result.Count == 0) return;

    var toRemove = new System.Collections.Generic.List<GrowContainer>();

    foreach (var container in __result) {
      var pot = container?.TryCast<Pot>();

      if (pot == null) continue;

      var id = PotHelper.GetId(pot);

      if (FertilizerStore.ValidateOrClean(pot, id, "botanist filter")) {
        toRemove.Add(container);
      }
    }

    foreach (var c in toRemove)
      __result.Remove(c);
  }
#else
  public static void Postfix(object __result) {
    var list = __result as List<GrowContainer>;

    if (list == null || list.Count == 0) return;

    list.RemoveAll(container => {
      var pot = container as Pot;

      if (pot == null) return false;

      var id = PotHelper.GetId(pot);

      return FertilizerStore.ValidateOrClean(pot, id, "botanist filter");
    });
  }
#endif
}

// ------------------------------------------------------------------
//  Persist on game save.
// ------------------------------------------------------------------
[HarmonyPatch(typeof(SaveManager), "Save", typeof(string))]
internal static class Patch_SaveWithPath {
  [HarmonyPrefix]
  public static void Prefix() {
    FertilizerStore.Save();
  }
}

[HarmonyPatch(typeof(SaveManager), "Save", new Type[] { })]
internal static class Patch_SaveNoArgs {
  [HarmonyPrefix]
  public static void Prefix() {
    FertilizerStore.Save();
  }
}

// ------------------------------------------------------------------
//  Load fertilizer data when a save game is loaded.
// ------------------------------------------------------------------
[HarmonyPatch(typeof(LoadManager), "StartGame")]
internal static class Patch_Load {
  [HarmonyPostfix]
#if IL2CPP
  public static void Postfix(SaveInfo info) {
    if (info != null) {
      FertilizerStore.SetSaveSlot($"SaveGame_{info.SaveSlotNumber}");
      FertilizerStore.Load();
    }
    else {
      FertilizerStore.SetSaveSlot("NewGame");
      FertilizerStore.Clear();
    }
  }
#else
  public static void Postfix(object info) {
    var saveInfo = info as SaveInfo;

    if (saveInfo != null) {
      FertilizerStore.SetSaveSlot($"SaveGame_{saveInfo.SaveSlotNumber}");
      FertilizerStore.Load();
    }
    else {
      FertilizerStore.SetSaveSlot("NewGame");
      FertilizerStore.Clear();
    }
  }
#endif
}
