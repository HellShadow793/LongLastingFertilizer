using LongLastingFertilizer;
using MelonLoader;

[assembly: MelonInfo(typeof(Mod), "LongLastingFertilizer", "1.0.0", "Sensanaty")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LongLastingFertilizer;

public class Mod : MelonMod {
  internal static Mod Instance { get; private set; }

  public override void OnInitializeMelon() {
    Instance = this;
    LoggerInstance.Msg("[LongLastingFertilizer] Initialized");
  }

  public override void OnApplicationQuit() {
    FertilizerStore.Save();
  }
}