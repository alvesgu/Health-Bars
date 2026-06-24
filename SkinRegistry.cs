// SkinRegistry.cs
// Scans the mod's assets/skins/ folder for PNG tilesheets, validates them,
// and provides a lookup table used by HealthBarRenderer at draw time.
//
// HOW TO ADD A SKIN
// ─────────────────
// Place a PNG file in:  <mod folder>/assets/skins/<name>.png
// Rules for the PNG:
//   • Height must be divisible by 3  (3 tile rows)
//   • Width  must be a multiple of (Height / 3)  (whole columns only)
//   • Minimum 5 columns (≥1 fill): [LB][LI][RI][RB][fill…]
//   • Column indices: 0=LB, 1=LI, 2=RI, 3=RB, 4+=fill
//
// The skin name shown in the config menu equals the file name without extension.
// Skins are loaded once at game launch; restart required to pick up new files.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.IO;

namespace HealthBars;

public static class SkinRegistry
{
    private static readonly Dictionary<string, BarSkin> _skins = new();

    /// <summary>All successfully loaded skins, keyed by name.</summary>
    public static IReadOnlyDictionary<string, BarSkin> Skins => _skins;

    /// <summary>Names of all loaded skins (does NOT include "None").</summary>
    public static IEnumerable<string> Names => _skins.Keys;

    /// <summary>Returns the skin for <paramref name="name"/>, or null if not found / "None".</summary>
    public static BarSkin? Get(string? name) =>
        name != null && _skins.TryGetValue(name, out var skin) ? skin : null;

    /// <summary>
    /// Scans assets/skins/*.png, validates each file, and populates the registry.
    /// Call this once during <c>OnGameLaunched</c> before GMCM registration.
    /// </summary>
    public static void Load(IModHelper helper, IMonitor monitor)
    {
        _skins.Clear();

        string skinsDir = Path.Combine(helper.DirectoryPath, "assets", "skins");
        if (!Directory.Exists(skinsDir))
            return;

        foreach (string file in Directory.GetFiles(skinsDir, "*.png"))
        {
            string skinName = Path.GetFileNameWithoutExtension(file);
            try
            {
                // Load directly from disk so no SMAPI content-pipeline restrictions apply.
                Texture2D tex;
                using (var stream = File.OpenRead(file))
                    tex = Texture2D.FromStream(StardewValley.Game1.graphics.GraphicsDevice, stream);

                // Texture2D.FromStream loads straight (non-premultiplied) alpha.
                // MonoGame's SpriteBatch expects premultiplied alpha, so semi-transparent
                // pixels would otherwise render too dark or with black halos.
                var pixels = new Color[tex.Width * tex.Height];
                tex.GetData(pixels);
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.FromNonPremultiplied(pixels[i].R, pixels[i].G, pixels[i].B, pixels[i].A);
                tex.SetData(pixels);

                // ── Validation ────────────────────────────────────────────
                if (tex.Height % 3 != 0)
                {
                    monitor.Log($"Skin '{skinName}': height ({tex.Height} px) must be divisible by 3 — skipped.", LogLevel.Warn);
                    continue;
                }

                int tileSize = tex.Height / 3;

                if (tileSize == 0)
                {
                    monitor.Log($"Skin '{skinName}': tile size is 0 — skipped.", LogLevel.Warn);
                    continue;
                }

                if (tex.Width % tileSize != 0)
                {
                    monitor.Log($"Skin '{skinName}': width ({tex.Width} px) is not a multiple of tile size ({tileSize} px) — skipped.", LogLevel.Warn);
                    continue;
                }

                int columns = tex.Width / tileSize;
                if (columns < 5)
                {
                    monitor.Log($"Skin '{skinName}': needs at least 5 columns (left border, left inner, right inner, right border, 1 fill). Found {columns} — skipped.", LogLevel.Warn);
                    continue;
                }

                int fillColumns = columns - 4;
                _skins[skinName] = new BarSkin(skinName, tex, tileSize, fillColumns);
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to load skin '{skinName}': {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
