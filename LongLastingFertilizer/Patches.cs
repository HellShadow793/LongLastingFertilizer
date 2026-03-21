using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
#if IL2CPP
using Il2CppFishNet;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Persistence;
#else
using FishNet;
using ScheduleOne.Employees;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence;
#endif

namespace LongLastingFertilizer;

public class Patches {
  /// <summary>
  ///   On Harvest: Save fertilizer state or clean up depleted soil
  /// </summary>
  [HarmonyPatch(typeof(Pot), "OnPlantFullyHarvested")]
  internal static class Patch_Harvest {
    [HarmonyPrefix]
    public static void Prefix(Pot instance) {
      if (instance?.Plant == null) return;

      var id = PotHelper.GetId(instance);

      if (PotHelper.HasSoilRemaining(instance) && PotHelper.HasAdditives(instance)) {
        FertilizerStore.CaptureState(instance, id);
      }
      else {
        FertilizerStore.Remove(id, "soil depleted at harvest");
      }
    }
  }

  [HarmonyPatch(typeof(Pot), "PlantSeed_Server")]
  internal static class Patch_Plant {
    [HarmonyPostfix]
    public static void Postfix(Pot instance) {
      if (instance == null || !InstanceFinder.IsServer) return;

      var id = PotHelper.GetId(instance);

      if (!FertilizerStore.TryGet(id, out var data)) return;

      if (!PotHelper.HasSoilRemaining(instance) || !PotHelper.HasAdditives(instance)) {
        FertilizerStore.Remove(id, "stale at planting");

        return;
      }

      Melon<Mod>.Logger.Msg($"Restoring fertilizers for pot {id}");
      MelonCoroutines.Start(WaitThenRestore(instance, data, id));
    }

    private static IEnumerator WaitThenRestore(Pot pot, PotFertilizerData data, string potId) {
      // Wait for the Plant object to be created (up to ~100 frames).
      for (var i = 0; i < 100 && pot?.Plant == null; i++)
        yield return null;

      if (pot?.Plant == null) {
        Melon<Mod>.Logger.Warning($"Plant never appeared for pot {potId}, skipping restore.");

        yield break;
      }

      // Small extra delay for network sync.
      for (var i = 0; i < 10; i++)
        yield return null;

#if IL2CPP
      pot.Plant.YieldMultiplier = data.YieldLevel;
      pot.Plant.QualityLevel = data.QualityLevel;
#else
      // In Mono, these setters are private so we use reflection here instead.
      var plantType = pot.Plant.GetType();
      plantType.GetProperty("YieldMultiplier")?.SetValue(pot.Plant, data.YieldLevel);
      plantType.GetProperty("QualityLevel")?.SetValue(pot.Plant, data.QualityLevel);
#endif

      Melon<Mod>.Logger.Msg(
        $"Restored yield={data.YieldLevel:F2}, quality={data.QualityLevel:F2} on pot {potId}");
    }
  }

  [HarmonyPatch(typeof(Pot), "CanApplyAdditive")]
  internal static class Patch_CanApply {
    [HarmonyPrefix]
    public static bool Prefix(Pot instance, AdditiveDefinition additiveDef,
      ref string invalidReason, ref bool __result) {
      if (instance == null || additiveDef == null) return true;

      // Only gate fertilizers (affect yield/quality), not pure speed-grow.
      if (!PotHelper.IsFertilizer(additiveDef)) return true;

      var id = PotHelper.GetId(instance);

      if (!FertilizerStore.ValidateOrClean(instance, id, "CanApplyAdditive")) {
        return true; // No valid saved data — let the game decide.
      }

      invalidReason = "This soil is already fertilized!";
      __result = false;

      return false; // Skip original method.
    }
  }

  [HarmonyPatch(typeof(Botanist), "GetGrowContainersForAdditives")]
  internal static class Patch_Botanist {
    [HarmonyPostfix]
#if IL2CPP
    public static void PostFix(ref List<GrowContainer> result) {
      if (result == null || result.Count == 0) return;

      var toRemove = new List<GrowContainer>();

      foreach (var container in result) {
        var pot = container?.TryCast<Pot>();

        if (pot == null) continue;

        var id = PotHelper.GetId(pot);

        if (FertilizerStore.ValidateOrClean(pot, id, "botanist filter")) {
          toRemove.Add(container);
        }
      }

      foreach (var container in toRemove) result.Remove(container);
    }
#else
    public static void Postfix(ref List<GrowContainer> result) {
      if (result == null || result.Count == 0) return;

      result.RemoveAll(container => {
        var pot = container as Pot;

        if (pot == null) return false;

        var id = PotHelper.GetId(pot);

        return FertilizerStore.ValidateOrClean(pot, id, "botanist filter");
      });
    }
#endif
  }

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
  }
}