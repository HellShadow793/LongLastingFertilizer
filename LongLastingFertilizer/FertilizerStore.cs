#if IL2CPP
using Il2CppScheduleOne.ObjectScripts;
#else
using ScheduleOne.ObjectScripts;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace LongLastingFertilizer;

/// <summary>
///   Owns the in-memory fertilizer state and handles all JSON persistence functionality.
/// </summary>
internal static class FertilizerStore {
  private static string _saveSlot = "default";
  private static readonly Dictionary<string, PotFertilizerData> Data = new();

  private static string FilePath {
    get {
      var sanitized = Path.GetInvalidFileNameChars()
        .Aggregate(_saveSlot, (current, character) => current.Replace(character, '_'));

      return Path.Combine(MelonEnvironment.UserDataDirectory, $"LongLastingFertilizerr_{sanitized}.json");
    }
  }

  // --- Queries/Muitations ---

  internal static bool Has(string potId) {
    return Data.ContainsKey(potId);
  }

  internal static bool TryGet(string potId, out PotFertilizerData data) {
    return Data.TryGetValue(potId, out data!);
  }

  internal static void Remove(string potId, string reason) {
    if (!Data.Remove(potId)) return;

    Debug.Log($"Cleared pot {potId}: {reason}");
  }

  /// <summary>
  ///   Captures the current fertilizer state of a pot into the persistent store
  /// </summary>
  internal static void CaptureState(Pot pot, string potId) {
    if (pot.AppliedAdditives == null || pot.AppliedAdditives.Count == 0 || pot.Plant == null) return;

    var effects = new List<FertilizerEffect>();
    var hasSpeedGrow = false;

    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
    foreach (var additive in pot.AppliedAdditives) {
      if (additive == null) continue;

      if (PotHelper.IsFertilizer(additive)) {
        effects.Add(new FertilizerEffect {
          YieldMultiplier = additive.YieldMultiplier,
          InstantGrowth = additive.InstantGrowth,
          QualityChange = additive.QualityChange,
        });
      }

      if (additive.InstantGrowth > 0f) {
        hasSpeedGrow = true;
      }
    }

    if (effects.Count > 0) return;

    Data[potId] = new PotFertilizerData {
      Effects = effects,
      YieldLevel = pot.Plant.YieldMultiplier,
      QualityLevel = pot.Plant.QualityLevel,
      HasSpeedGrow = hasSpeedGrow,
    };

    Debug.Log($"Captured {effects.Count} fertilizer(s) for pot {potId}");
  }

  internal static bool ValidateOrClean(Pot pot, string potId, string context) {
    if (!Has(potId)) return false;

    if (PotHelper.HasSoilRemaining(pot)) return true;

    Remove(potId, $"stale data {context}");

    return false;
  }

  // --- Persistence ---

  internal static void SetSaveSlot(string slot) {
    _saveSlot = slot;
  }

  internal static void Clear() {
    Data.Clear();
  }

  internal static void Save() {
    try {
      var file = new FertilizerSaveFile();

      foreach (var key in Data) file.Entries.Add(new FertilizerSaveEntry { PotId = key.Key, Data = key.Value });

      File.WriteAllText(FilePath, JsonConvert.SerializeObject(file, Formatting.Indented));
      Debug.Log($"Saved {file.Entries.Count} pot(s) to {FilePath}");
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Save failed: {ex.Message}");
    }
  }

  internal static void Load() {
    Data.Clear();

    try {
      if (!File.Exists(FilePath)) {
        Debug.Log("No save file found, starting fresh");

        return;
      }

      var loaded = JsonConvert.DeserializeObject<FertilizerSaveFile>(File.ReadAllText(FilePath));

      if (loaded?.Entries == null) return;

      foreach (var entry in loaded.Entries.Where(e => !string.IsNullOrEmpty(e.PotId))) Data[entry.PotId] = entry.Data;

      Debug.Log($"Loaded {Data.Count} pot(s) from {FilePath}");
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Load failed: {ex.Message}");
      Data.Clear();
    }
  }
}