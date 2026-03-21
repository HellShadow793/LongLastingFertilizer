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

namespace LongLastingFertilizer;

/// <summary>
///   Owns the in-memory fertilizer state and handles JSON persistence.
/// </summary>
internal static class FertilizerStore {
  private static readonly Dictionary<string, PotFertilizerData> Data = new();
  private static string _saveSlot = "default";

  private static string FilePath {
    get {
      var sanitized = Path.GetInvalidFileNameChars()
        .Aggregate(_saveSlot, (current, c) => current.Replace(c, '_'));

      return Path.Combine(MelonEnvironment.UserDataDirectory, $"LongLastingFertilizer_{sanitized}.json");
    }
  }

  // --- Query / Mutate ---

  internal static bool Has(string potId) {
    return Data.ContainsKey(potId);
  }

  internal static bool TryGet(string potId, out PotFertilizerData data) {
    return Data.TryGetValue(potId, out data);
  }

  internal static void Remove(string potId, string reason) {
    if (!Data.Remove(potId)) return;
    Melon<Mod>.Logger.Msg($"Cleared pot {potId}: {reason}");
  }

  /// <summary>
  ///   Captures the current fertilizer state of a pot into the store.
  ///   Only saves additives that affect yield or quality (not pure speed-grow).
  /// </summary>
  internal static void CaptureState(Pot pot, string potId) {
    if (pot.AppliedAdditives == null || pot.AppliedAdditives.Count == 0 || pot.Plant == null) {
      return;
    }

    var effects = new List<FertilizerEffect>();
    var hasSpeedGrow = false;
    var count = pot.AppliedAdditives.Count;

    for (var i = 0; i < count; i++)
      try {
        var additive = pot.AppliedAdditives[i];

        if (additive == null) continue;

        if (PotHelper.IsFertilizer(additive)) {
          effects.Add(new FertilizerEffect {
            YieldMultiplier = additive.YieldMultiplier,
            QualityChange = additive.QualityChange,
            InstantGrowth = additive.InstantGrowth,
          });
        }

        if (additive.InstantGrowth > 0f) {
          hasSpeedGrow = true;
        }
      }
      catch (Exception ex) {
        Melon<Mod>.Logger.Error($"Error reading additive [{i}]: {ex.Message}");
      }

    if (effects.Count == 0) return;

    try {
      Data[potId] = new PotFertilizerData {
        Effects = effects,
        YieldLevel = pot.Plant.YieldMultiplier,
        QualityLevel = pot.Plant.QualityLevel,
        HasSpeedGrow = hasSpeedGrow,
      };

      Melon<Mod>.Logger.Msg(
        $"Captured {effects.Count} fertilizer(s) for pot {potId} " +
        $"(yield={pot.Plant.YieldMultiplier:F2}, quality={pot.Plant.QualityLevel:F2})");
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Error capturing plant values: {ex.Message}");
    }
  }

  /// <summary>
  ///   Validates a stored entry is still relevant. Cleans up stale data if soil is depleted.
  /// </summary>
  internal static bool ValidateOrClean(Pot pot, string potId, string context) {
    if (!Has(potId)) return false;
    if (PotHelper.HasSoilRemaining(pot)) return true;

    Remove(potId, $"stale data ({context})");

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
      foreach (var kvp in Data)
        file.Entries.Add(new FertilizerSaveEntry { PotId = kvp.Key, Data = kvp.Value });

      File.WriteAllText(FilePath, JsonConvert.SerializeObject(file, Formatting.Indented));
      Melon<Mod>.Logger.Msg($"Saved {file.Entries.Count} pot(s) to {FilePath}");
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Save failed: {ex.Message}");
    }
  }

  internal static void Load() {
    Data.Clear();

    try {
      if (!File.Exists(FilePath)) {
        Melon<Mod>.Logger.Msg("No save file found, starting fresh.");

        return;
      }

      var loaded = JsonConvert.DeserializeObject<FertilizerSaveFile>(File.ReadAllText(FilePath));

      if (loaded?.Entries == null) return;

      foreach (var entry in loaded.Entries.Where(e => !string.IsNullOrEmpty(e.PotId)))
        Data[entry.PotId] = entry.Data;

      Melon<Mod>.Logger.Msg($"Loaded {Data.Count} pot(s) from {FilePath}");
    }
    catch (Exception ex) {
      Melon<Mod>.Logger.Error($"Load failed: {ex.Message}");
      Data.Clear();
    }
  }
}