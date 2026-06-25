# Health Bars

A [Stardew Valley](https://www.stardewvalley.net/) mod that displays a health bar above your character while you play. Fully customizable through an in-game menu.

---

## Features

- Health bar drawn above the player's head, updating in real time.
- Health bars drawn above every visible monster, centered over their head.
- Three bar styles: **Flat**, **Rounded** (pill/capsule), and **Striped**.
- Fill colors: **Gradient** (shifts through three stops as health changes), any of five **Preset** colors, or a fully **Custom** color.
- Custom color pickers with palette swatches and individual R/G/B sliders for the fill, gradient stops, and border.
- Adjustable width, height, vertical/horizontal position, border thickness, and opacity.
- Live preview panel in the config menu so you see every change before saving.
- The preview background uses your current season's real outdoor tilesheet.

---

## Requirements

| Dependency | Required? | Notes |
|---|---|---|
| [Stardew Valley](https://www.stardewvalley.net/) | Yes | Version 1.6 or later |
| [SMAPI](https://smapi.io/) | Yes | Version 4.0.0 or later |
| [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) | **No** | Without it the mod still works; edit `config.json` manually to change settings |

---

## Installation

1. Install [SMAPI](https://smapi.io/) if you haven't already.
2. Download the latest release of **Health Bars**.
3. Unzip the download and place the `HealthBars` folder into your `Stardew Valley/Mods/` folder.
4. Launch the game through SMAPI.

That's it. The health bar will appear the moment you load a save.

---

## Configuration

If you have [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) installed, open the in-game main menu → **Mod Options** → **Health Bars** to configure everything with a live preview.

Without GMCM, open `Mods/HealthBars/config.json` in any text editor. The file is created on first launch.

### All settings

| Setting | Default | Description |
|---|---|---|
| `BarWidth` | `120` | Width of the health bar in pixels. |
| `BarHeight` | `24` | Height of the health bar in pixels. |
| `VerticalOffset` | `140` | Pixels above the character's feet to draw the bar. Increase to move it higher. |
| `HorizontalOffset` | `0` | Horizontal shift from center. Positive = right, negative = left. |
| `Opacity` | `1.0` | Opacity of the bar. `1.0` = fully visible, `0.0` = invisible. |
| `SkinName` | `"Original"` | Name of a skin PNG from `assets/skins/`, or `"None"` for procedural styles. |
| `BarStyle` | `"Rounded"` | `"Flat"`, `"Rounded"`, or `"Striped"`. Controls fill shape; visible through transparent skin areas. |
| `BorderSize` | `2` | Border ring thickness in pixels. |
| `ColorMode` | `"Gradient"` | `"Gradient"`, `"Custom"`, `"Red"`, `"Green"`, or `"Blue"`. |
| `FillR/G/B` | `30, 200, 30` | Custom fill color (0–255 per channel). Used when `ColorMode` is `"Custom"`. |
| `GradStartR/G/B` | `255, 0, 0` | Gradient color at 0% health (danger / low). |
| `GradMidR/G/B` | `255, 255, 0` | Gradient color at 50% health (mid). |
| `GradEndR/G/B` | `0, 200, 0` | Gradient color at 100% health (full / safe). |
| `BorderR/G/B` | `30, 30, 30` | Border color (0–255 per channel). |
| `ShowMonsterBars` | `true` | Draw health bars above monsters. |
| `ShowMonsterBarsAtFullHealth` | `true` | When `false`, bars are hidden until a monster has taken at least one hit. |
| `MonsterBarWidth` | `60` | Width of each monster health bar in pixels. |
| `MonsterBarHeight` | `12` | Height of each monster health bar in pixels. |
| `MonsterBorderSize` | `2` | Border thickness around monster health bars. `0` = no border. |
| `MonsterColorMode` | `"Red"` | Fill color for monster bars: `"Red"`, `"Green"`, `"Blue"`, `"Yellow"`, `"Cyan"`, `"White"`, or `"Gradient"`. |

---

## Compatibility

- **Multiplayer**: works in split-screen co-op (each player sees their own bar). Untested in online multiplayer.
- **Other HUD mods**: should be compatible with most HUD mods since this mod draws during `RenderedWorld` and does not modify any game data.
- **Android**: not tested.

---

## Building from Source

Requirements: [.NET 6 SDK](https://dotnet.microsoft.com/), SMAPI, and the game installed at a standard path.

```bash
git clone https://github.com/alvesgu/Health-Bars.git
cd Health-Bars
dotnet build
```

The mod DLL and manifest are copied to `Stardew Valley/Mods/HealthBars/` automatically on a successful build (configured in the `.csproj`).

---

## Credits

- **[alvesgu](https://github.com/alvesgu)** — mod author
- [SMAPI](https://smapi.io/) by Pathoschild
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) by spacechase0
- Monster health bar concept inspired by [hp-bars](https://github.com/mk-gg/hp-bars) by mk-gg, [Enemy Health Bar](https://www.nexusmods.com/stardewvalley/mods/7889) by OrSpeeder, and [Mini Bars](https://www.nexusmods.com/stardewvalley/mods/5967) by Coldopa

---

## License

[MIT License](LICENSE) — free to use, modify, and redistribute with attribution.
