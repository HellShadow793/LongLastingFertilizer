namespace LongLastingFertilizer;

[Serializable]
public class FertilizerEffect {
  public string Name { get; set; } = "";
  public float YieldMultiplier { get; set; }
  public float QualityChange { get; set; }
  public float InstantGrowth { get; set; }
}

[Serializable]
public class PotFertilizerData {
  public List<FertilizerEffect> Effects { get; set; } = [];
  public float YieldLevel { get; set; }
  public float QualityLevel { get; set; }
  public bool HasSpeedGrow { get; set; }
}

[Serializable]
public class FertilizerSaveFile {
  public List<FertilizerSaveEntry> Entries { get; set; } = [];
}

[Serializable]
public class FertilizerSaveEntry {
  public string PotId { get; set; } = "";
  public PotFertilizerData Data { get; set; } = new();
}