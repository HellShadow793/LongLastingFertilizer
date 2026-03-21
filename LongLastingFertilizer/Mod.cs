using LongLastingFertilizer;
using MelonLoader;

[assembly: MelonInfo(typeof(Mod), "LongLastingFertilizer", "1.0.0", "Sensanaty")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LongLastingFertilizer;

public class Mod : MelonMod {
  public override void OnInitializeMelon() {
    LoggerInstance.Msg("LongLastingFertilizer loaded.");
  }

  public override void OnApplicationQuit() {
    FertilizerStore.Save();
  }
}