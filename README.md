# Long Lasting Fertilizer

You know how fertilizer disappears after a harvest even though the soil's still good? Yeah, that's annoying. You shouldn't have to re-fertilize every single cycle when your long-life soil has plenty of uses left.

This mod makes fertilizer effects stick around for as long as the soil does. Plant, harvest, replant — your quality bonus carries over. Simple as that.

## How It Works

- Fertilizer effects (yield and quality) carry over between grow cycles on the same soil
- Once the soil runs out of uses, the fertilizer clears and you can start fresh
- Can't double-dip — no stacking fertilizer on top of already-fertilized soil
- Your botanist employees won't waste fertilizer on pots that already have it
- Everything saves and loads with your game, no data lost between sessions
- Works for both Mono & IL2CPP backends

## Known Issues

- The fertilizer visual effects don't persist between grow cycles. Your pot will look unfertilized even though the effects are still active. You can verify by trying to apply fertilizer with a bottle, it'll block you if the pot already has it.

## Installation

1. Install [MelonLoader 0.7.0](https://github.com/LavaGang/MelonLoader/releases) if you haven't already
2. Drop `LongLastingFertilizer.dll` into your `Schedule I\Mods\` folder
3. That's it. Get back to the grind.

## Building From Source

1. Clone this repo
2. Copy `LongLastingFertilizer/Directory.Build.props.example` to `LongLastingFertilizer/Directory.Build.props`
3. Edit `Directory.Build.props` and set `ScheduleIDir` to your Schedule I install path
4. Launch the game once with MelonLoader installed (generates the assemblies we need)
5. Open the solution in Rider or Visual Studio and build

### Build Configurations

| Configuration | Target | Branch |
|---|---|---|
| **Debug** | IL2CPP + debug symbols | Main |
| **IL2CPP** | IL2CPP release | Main |
| **Mono** | Mono | Alternate (Steam beta) |

Pick your configuration from the IDE toolbar. The built DLL auto-copies to your Mods folder.

**Note:** Your Steam branch needs to match your build config. Main branch = IL2CPP, alternate branch = Mono.

## Maintenance

I probably won't be actively fixing bugs here, but it's MIT licensed so feel free to steal the code and reuse it in your own fork or mod.

## License

MIT