using MelonLoader;

[assembly: MelonInfo(typeof(LongLastingFertilizer.Mod), "LongLastingFertilizer", "1.0.0", "Sensanaty")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LongLastingFertilizer {
  public class Mod : MelonMod {
    internal static Mod Instance { get; private set; } = null;

    public override void OnInitializeMelon() {
      Instance = this;
      LoggerInstance.Msg("[LongLastingFertilizer] Initialized");
    }
  }
}