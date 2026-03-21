using System;
using System.Collections.Generic;

namespace LongLastingFertilizer;

[Serializable]
public class FertilizerEffect {
  public float YieldMultiplier { get; set; }
  public float QualityChange { get; set; }
  public float InstantGrowth { get; set; }
}

[Serializable]
public class PotFertilizerData {
  public List<FertilizerEffect> Effects { get; set; } = new();
  public float YieldLevel { get; set; }
  public float QualityLevel { get; set; }
  public bool HasSpeedGrow { get; set; }
}

[Serializable]
public class FertilizerSaveEntry {
  public string PotId { get; set; } = "";
  public PotFertilizerData Data { get; set; } = new();
}

[Serializable]
public class FertilizerSaveFile {
  public List<FertilizerSaveEntry> Entries { get; set; } = new();
}