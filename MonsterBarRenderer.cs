// MonsterBarRenderer.cs
// Draws a health bar above each visible monster in the current location.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;

namespace HealthBars;

internal static class MonsterBarRenderer
{
    // ── Public API ────────────────────────────────────────────────────────────

    public static void Draw(SpriteBatch b, Monster monster, ModConfig config)
    {
        if (!ShouldRender(monster, config)) return;

        // Some monsters initialize MaxHealth at 0 before their first update tick; clamp it up.
        monster.MaxHealth = Math.Max(monster.Health, monster.MaxHealth);
        float pct = Math.Clamp((float)monster.Health / Math.Max(1, monster.MaxHealth), 0f, 1f);

        Vector2 pos = monster.getLocalPosition(Game1.viewport);
        int w = config.MonsterBarWidth;
        int h = config.MonsterBarHeight;
        int x = (int)(pos.X + monster.Sprite.SpriteWidth * Game1.pixelZoom / 2f - w / 2f);
        // +41 places the bar just above the monster's visible head rather than at the raw sprite-bounds top.
        int y = (int)(pos.Y - monster.Sprite.SpriteHeight * Game1.pixelZoom - h + 41);

        float alpha    = Math.Clamp(config.Opacity, 0f, 1f);
        Color fill     = ResolveFillColor(pct, config) * alpha;
        bool  rounded  = config.BarStyle != "Flat";
        int   fillCapW = h / 2;
        int   bs       = config.MonsterBorderSize;

        // 1. Empty background: 20% yellow, drawn directly over the dungeon so it's transparent.
        var bg = new Color(220, 180, 30) * (alpha * 0.2f);
        if (rounded)
            HealthBarRenderer.DrawRoundedFill(b, x, y, w, h, fillCapW, w, bg);
        else
            HealthBarRenderer.DrawRect(b, x, y, w, h, bg);

        // 2. Health fill.
        int fw = (int)(w * pct);
        if (fw > 0)
        {
            if (rounded)
                HealthBarRenderer.DrawRoundedFill(b, x, y, w, h, fillCapW, fw, fill);
            else
                HealthBarRenderer.DrawRect(b, x, y, fw, h, fill);

            if (config.BarStyle == "Striped")
                HealthBarRenderer.DrawBodyStripes(b, x, y, w, h, fillCapW, fw, fill);
        }

        // 3. Border ring drawn last — only the ring pixels, center left transparent.
        if (bs > 0)
        {
            if (rounded)
                HealthBarRenderer.DrawRoundedRing(b, x, y, w, h, bs, new Color(0, 0, 0) * alpha);
            else
            {
                // Flat border: four edge rectangles, no filled center.
                var bc = new Color(0, 0, 0) * alpha;
                HealthBarRenderer.DrawRect(b, x - bs, y - bs, w + 2 * bs, bs, bc); // top
                HealthBarRenderer.DrawRect(b, x - bs, y + h,  w + 2 * bs, bs, bc); // bottom
                HealthBarRenderer.DrawRect(b, x - bs, y,      bs,          h,  bc); // left
                HealthBarRenderer.DrawRect(b, x + w,  y,      bs,          h,  bc); // right
            }
        }
    }

    // ── Color resolution ──────────────────────────────────────────────────────

    private static Color ResolveFillColor(float pct, ModConfig config) => config.MonsterColorMode switch
    {
        "Red"    => new Color(220, 30,  30),
        "Green"  => new Color(30,  200, 30),
        "Blue"   => new Color(30,  100, 220),
        "Yellow" => new Color(220, 200, 30),
        "Cyan"   => new Color(30,  200, 200),
        "White"  => Color.White,
        _        => HealthBarRenderer.ResolveFillColor(pct, config),
    };

    // ── Visibility filter ─────────────────────────────────────────────────────

    private static bool ShouldRender(Monster monster, ModConfig config)
    {
        if (!config.ShowMonsterBars) return false;
        if (!config.ShowMonsterBarsAtFullHealth && monster.Health >= monster.MaxHealth) return false;
        if (monster.IsInvisible) return false;
        if (!Utility.isOnScreen(monster.position.Value, 3 * Game1.tileSize)) return false;

        if (monster is RockCrab  && monster.Sprite.CurrentFrame % 4 == 0) return false;
        if (monster is RockGolem && monster.Sprite.CurrentFrame == 16)    return false;
        if (monster is Spiker)                                             return false;

        return true;
    }
}
