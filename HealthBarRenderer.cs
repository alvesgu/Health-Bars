// HealthBarRenderer.cs
// Responsible for drawing the health bar during gameplay AND the live preview
// shown inside the Generic Mod Config Menu (GMCM) settings panel.
//
// OVERVIEW OF WHAT GETS DRAWN
// ────────────────────────────
//  In-game (Draw):
//    • A health bar floating above the player, centered horizontally.
//
//  GMCM preview panel (DrawPreview):
//    • A tiled outdoor background (loaded from the game's own tilesheet).
//    • The player's farmer sprite in the idle pose.
//    • The health bar at 65 % health so all three gradient colors are visible.
//
// COORDINATE SYSTEM
// ─────────────────
// All positions are in screen pixels. (0,0) is the top-left of the game window.
// The player's "standing position" is their feet, so we subtract VerticalOffset
// to place the bar above their head.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace HealthBars;

public class HealthBarRenderer
{
    private readonly ModConfig _config;

    // ── Preview panel layout constants ────────────────────────────────────────

    /// <summary>Width of the preview panel in the config menu, in pixels.</summary>
    public const int PreviewWidth = 300;

    /// <summary>Empty space above the tile background before content starts.</summary>
    public const int PreviewPadTop = 8;

    /// <summary>Empty space below the character's feet (where the bar sits).</summary>
    public const int PreviewPadBot = 20;

    // The farmer sprite is 16×32 source pixels rendered at 4× zoom (game's native zoom),
    // producing a 64×128 pixel character on screen.
    private const int CharScale   = 4;
    private const int CharSpriteW = 16;
    private const int CharSpriteH = 32;
    private const int CharW       = CharSpriteW * CharScale; // 64 px on screen
    private const int CharH       = CharSpriteH * CharScale; // 128 px on screen

    // Thin dark border drawn around the whole preview panel.
    private const int PreviewBorder = 2;

    public HealthBarRenderer(ModConfig config) => _config = config;

    // ── In-game drawing ───────────────────────────────────────────────────────

    /// <summary>
    /// Called every frame during gameplay to draw the health bar above the player.
    /// </summary>
    public void Draw(SpriteBatch b, Farmer player)
    {
        // healthPercent is 0.0 (dead) … 1.0 (full health).
        float healthPercent = Math.Clamp((float)player.health / player.maxHealth, 0f, 1f);

        // Convert the player's world position to screen pixels, then apply offsets.
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, player.getStandingPosition());
        int barX = (int)screenPos.X - _config.BarWidth / 2 + _config.HorizontalOffset;
        int barY = (int)screenPos.Y - _config.VerticalOffset;

        DrawBar(b, barX, barY, healthPercent, _config);
    }

    // ── GMCM preview drawing ──────────────────────────────────────────────────

    /// <summary>
    /// Draws the live preview panel inside the Generic Mod Config Menu.
    /// Uses <paramref name="config"/> (the live/unsaved settings) so the player
    /// sees changes instantly before clicking Save.
    /// </summary>
    public static void DrawPreview(SpriteBatch b, Vector2 position, ModConfig config)
    {
        int px       = (int)position.X;
        int py       = (int)position.Y;
        int previewH = GetPreviewHeight();

        // Dark border frame around the preview panel.
        DrawRect(b, px - PreviewBorder, py - PreviewBorder,
                 PreviewWidth + PreviewBorder * 2, previewH + PreviewBorder * 2,
                 new Color(20, 20, 20));

        // Tiled outdoor background using the game's own seasonal tilesheet.
        DrawPreviewBackground(b, px, py, PreviewWidth, previewH);

        // The farmer sprite is centered horizontally and stands at the bottom of the panel.
        int standingX = px + PreviewWidth / 2;
        int standingY = py + previewH - PreviewPadBot;

        var player = Game1.player;
        if (player?.FarmerRenderer != null)
        {
            try
            {
                // Draw the player's current appearance in their idle (frame 0) pose.
                player.FarmerRenderer.draw(
                    b,
                    new FarmerSprite.AnimationFrame(0, 0),
                    0,
                    new Rectangle(0, 0, CharSpriteW, CharSpriteH),
                    new Vector2(standingX - CharW / 2, standingY - CharH),
                    Vector2.Zero,
                    0.9f,   // draw depth (above the background)
                    2,      // facing direction: down
                    Color.White,
                    0f,     // rotation
                    1f,     // scale
                    player);
            }
            catch
            {
                // FarmerRenderer can fail if called before the game fully loads.
                // Fall back to a plain gray silhouette so the preview still works.
                DrawRect(b, standingX - CharW / 2, standingY - CharH, CharW, CharH, new Color(100, 100, 100, 160));
            }
        }
        else
        {
            DrawRect(b, standingX - CharW / 2, standingY - CharH, CharW, CharH, new Color(100, 100, 100, 160));
        }

        // Draw the bar at 65 % health so the gradient's mid-color is visible.
        int barX = standingX - config.BarWidth / 2 + config.HorizontalOffset;
        // The -14 corrects for the difference between standingY (feet pixel used here)
        // and getStandingPosition() (which sits slightly above the feet pixel in Draw()).
        int barY = standingY - 14 - config.VerticalOffset;
        DrawBar(b, barX, barY, healthPercent: 0.65f, config);
    }

    /// <summary>
    /// Total height of the preview panel in pixels.
    /// The 260 px content area fits 4–5 tile rows at 4× scale plus the character.
    /// </summary>
    public static int GetPreviewHeight() =>
        260 + PreviewPadTop + PreviewPadBot; // = 288 px

    // ── Preview background ────────────────────────────────────────────────────

    // Tile coordinates decoded from a small hand-crafted Tiled map (healthbars2.json).
    // Each entry is (srcX, srcY) inside the seasonal outdoor tilesheet, where
    // every tile is 16×16 source pixels.
    //
    // The map is 5 columns × 5 rows and represents a natural outdoor ground scene:
    //   Row 0 – grass surface (top edge)
    //   Rows 1-3 – dirt / ground interior
    //   Row 4 – grass transition (bottom edge)
    //
    // Decode formula (Tiled GID → tilesheet pixel, columns=25, firstgid=1):
    //   srcX = ((gid - 1) % 25) * 16
    //   srcY = ((gid - 1) / 25) * 16
    private static readonly (int x, int y)[,] PreviewMapTiles = new (int, int)[5, 5]
    {
        { (0,128),  (64,112), (80,112), (80,128), (48,128) },  // row 0: grass surface
        { (0,144),  (32,144), (16,112), (160,368),(48,144) },  // row 1: dirt
        { (0,144),  (112,128),(16,144), (32,144), (48,144) },  // row 2: dirt
        { (0,144),  (16,144), (32,144), (16,112), (48,144) },  // row 3: dirt
        { (0,160),  (16,160), (64,144), (80,144), (48,160) },  // row 4: bottom grass edge
    };

    /// <summary>
    /// Fills the preview panel with tiled outdoor terrain using the game's own
    /// seasonal tilesheet ({season}_outdoorsTileSheet).
    ///
    /// Tiles are rendered at 4× scale (64 px per 16-px source tile) to match the
    /// game's native zoom level, making the player sprite appear the correct size
    /// relative to the ground (2 tiles tall, same as in-game).
    ///
    /// A solid color is drawn first as a fallback in case the tilesheet fails to load.
    /// </summary>
    private static void DrawPreviewBackground(SpriteBatch b, int px, int py, int w, int h)
    {
        string season = Game1.currentSeason ?? "spring";

        // Fallback solid color that roughly matches each season's palette.
        Color groundColor = season switch
        {
            "fall"   => new Color(140, 100,  50),
            "winter" => new Color(220, 230, 240),
            _        => new Color( 82, 132,  56), // spring / summer
        };
        DrawRect(b, px, py, w, h, groundColor);

        // At 4× scale each source tile (16 px) becomes 64 px on screen.
        const int tileScale = 4, tileSrc = 16, tileSize = tileSrc * tileScale; // 64 px

        // Anchor the last map row to the very bottom of the preview so the ground
        // sits under the character's feet, then let rows extend upward.
        // mapStartY will typically be above py, so the topmost rows are clipped.
        int mapStartY = py + h - 5 * tileSize;

        try
        {
            var sheet = Game1.content.Load<Texture2D>($"Maps/{season}_outdoorsTileSheet");

            // Draw one row of the tile map at vertical position ty.
            // Handles partial rows at the top/bottom so no solid fallback color bleeds through.
            void DrawRow(int ty, int row)
            {
                // Clamp destination to the panel bounds.
                int drawY   = Math.Max(ty, py);
                int drawH   = Math.Min(ty + tileSize, py + h) - drawY;
                if (drawH <= 0) return;

                // If the row is partially above the panel, start reading the source
                // tile from the corresponding pixel offset so the crop is seamless.
                int srcYOff = (drawY - ty) / tileScale;
                int sh      = (drawH + tileScale - 1) / tileScale;

                for (int tx = px, col = 0; tx < px + w; tx += tileSize, col++)
                {
                    // Wrap columns — the 5-tile map repeats horizontally to fill any width.
                    var (tileX, tileY) = PreviewMapTiles[row, col % 5];
                    int dw = Math.Min(tileSize, px + w - tx);
                    int sw = (dw + tileScale - 1) / tileScale;

                    b.Draw(sheet,
                        new Rectangle(tx,    drawY,            dw, drawH), // destination on screen
                        new Rectangle(tileX, tileY + srcYOff, sw, sh),    // source rect in tilesheet
                        Color.White);
                }
            }

            for (int row = 0; row < 5; row++)
                DrawRow(mapStartY + row * tileSize, row);
        }
        catch
        {
            // If the tilesheet can't be loaded (e.g. on title screen), the solid
            // fallback color drawn above is shown instead — no crash.
        }
    }

    // ── Bar dispatch ──────────────────────────────────────────────────────────

    /// <summary>
    /// Top-level draw entry point. Routes to skin-based or procedural rendering
    /// depending on whether a custom art skin is selected.
    /// </summary>
    private static void DrawBar(SpriteBatch b, int barX, int barY, float healthPercent, ModConfig config)
    {
        float alpha     = Math.Clamp(config.Opacity, 0f, 1f);
        int   w         = config.BarWidth;
        int   h         = config.BarHeight;
        Color fillColor = ResolveFillColor(healthPercent, config);

        // If a custom skin is loaded, use it and skip all procedural style/border logic.
        var skin = SkinRegistry.Get(config.SkinName);
        if (skin != null)
        {
            DrawSkinned(b, barX, barY, w, h, healthPercent, fillColor * alpha, alpha, skin, config.BarStyle);
            return;
        }

        // Procedural styles (Flat / Rounded / Striped):
        int   bs          = config.BorderSize;
        Color borderColor = new(config.BorderR, config.BorderG, config.BorderB);

        if (config.BarStyle == "Flat")
            DrawFlat(b, barX, barY, w, h, bs, fillColor * alpha, borderColor * alpha, alpha, healthPercent);
        else
            DrawStyled(b, barX, barY, w, h, fillColor * alpha, borderColor * alpha, healthPercent, config);
    }

    // ── Skin rendering ────────────────────────────────────────────────────────

    /// <summary>
    /// Draws the health bar using a user-created tilesheet skin.
    ///
    /// Layer order (bottom to top):
    ///   1. Colored health fill  — proportional to health%
    ///   2. Skin tiles           — left cap | repeating body | right cap
    ///
    /// Tiles scale to fit the configured bar dimensions:
    ///   rendered tile height = BarHeight / 3  (three rows: top, middle, bottom)
    ///   rendered tile width  = same (tiles are square in the source PNG)
    ///
    /// Parameters:
    ///   b        — SpriteBatch to draw into (already Begin'd by the caller)
    ///   x        — left edge of the bar in screen pixels
    ///   y        — top edge of the bar in screen pixels
    ///   w        — total bar width in screen pixels (config.BarWidth)
    ///   h        — total bar height in screen pixels (config.BarHeight)
    ///   pct      — health fraction 0.0 (empty) … 1.0 (full)
    ///   fill     — fill color already multiplied by alpha (fillColor * alpha from DrawBar)
    ///   alpha    — opacity 0.0 … 1.0 (config.Opacity, clamped); applied to skin tiles as Color.White * alpha
    ///   skin     — the loaded BarSkin (tilesheet texture + tileSize + fillColumns)
    ///   barStyle — "Flat", "Rounded", or "Striped"; controls fill shape under the skin
    /// </summary>
    private static void DrawSkinned(SpriteBatch b, int x, int y, int w, int h,
                                     float pct, Color fill, float alpha, BarSkin skin, string barStyle)
    {
        // 1. Health fill — shape respects BarStyle so transparent skin corners don't bleed.
        int fw       = (int)(w * pct);
        int fillCapW = h / 2; // same cap radius used by the non-skinned Rounded mode
        if (fw > 0)
        {
            if (barStyle is "Rounded" or "Striped")
                DrawRoundedFill(b, x, y, w, h, fillCapW, fw, fill);
            else
                DrawRect(b, x, y, fw, h, fill);

            if (barStyle == "Striped")
                DrawBodyStripes(b, x, y, w, h, fillCapW, fw, fill, alpha);
        }

        // 3. Skin tiles drawn on top of the fill.
        int src  = skin.TileSize;

        // Split bar height into three rows; the middle row absorbs any rounding remainder.
        int topH = h / 3;
        int botH = h / 3;
        int midH = h - topH - botH;

        // Rendered tile width = topH (square tiles → rendered width matches rendered row height).
        int capW = topH;

        // Draws one skin column (all 3 source rows stacked) at the given screen position.
        // srcCol  = column index in the tilesheet
        // destX   = left edge on screen
        // destW   = rendered column width (< capW only for the last partial body tile)
        // srcW    = source pixels to read (proportionally clipped when destW < capW)
        void DrawSkinColumn(int srcCol, int destX, int destW, int srcW)
        {
            int sx = srcCol * src;
            b.Draw(skin.Sheet,
                new Rectangle(destX, y,                destW, topH),
                new Rectangle(sx, 0,                   srcW,  src), Color.White * alpha);
            b.Draw(skin.Sheet,
                new Rectangle(destX, y + topH,         destW, midH),
                new Rectangle(sx, src,                 srcW,  src), Color.White * alpha);
            b.Draw(skin.Sheet,
                new Rectangle(destX, y + topH + midH,  destW, botH),
                new Rectangle(sx, src * 2,             srcW,  src), Color.White * alpha);
        }

        // PNG column layout (left → right): [col 0: LB] [col 1: LI] [col 2: RI] [col 3: RB] [col 4+: fill]
        // Column index = zero-based position in the PNG; sx = col * tileSize gives the pixel X offset.
        // Draw order: left cap → fill body → right cap (right cap last so it always renders on top).
        DrawSkinColumn(0, x,        capW, src); // LB (PNG col 0) — left outer border
        DrawSkinColumn(1, x + capW, capW, src); // LI (PNG col 1) — left inner border

        int bodyX = x + 2 * capW;
        int bodyW = w - 4 * capW;
        for (int bx = 0; bx < bodyW; bx += capW)
        {
            int fillColIdx = (bx / capW) % skin.FillColumns;
            int dw  = Math.Min(capW, bodyW - bx);
            int sw  = Math.Max(1, dw * src / capW);
            DrawSkinColumn(4 + fillColIdx, bodyX + bx, dw, sw); // fill (PNG col 4+)
        }

        DrawSkinColumn(2, x + w - 2 * capW, capW, src); // RI (PNG col 2) — right inner border
        DrawSkinColumn(3, x + w - capW,      capW, src); // RB (PNG col 3) — right outer border
    }

    // ── Flat style ────────────────────────────────────────────────────────────

    private static void DrawFlat(SpriteBatch b, int x, int y, int w, int h, int bs,
                                  Color fill, Color border, float alpha, float pct)
    {
        // Border: a solid rectangle slightly larger than the bar.
        if (bs > 0)
            DrawRect(b, x - bs, y - bs, w + bs * 2, h + bs * 2, border);

        // Dark red empty background (shows the "missing health" portion).
        DrawRect(b, x, y, w, h, new Color(60, 20, 20) * alpha);

        // Filled portion, proportional to health.
        int fw = (int)(w * pct);
        if (fw > 0)
            DrawRect(b, x, y, fw, h, fill);
    }

    // ── Rounded / Striped style ───────────────────────────────────────────────

    private static void DrawStyled(SpriteBatch b, int x, int y, int w, int h,
                                    Color fill, Color border, float pct, ModConfig config)
    {
        int   bs    = config.BorderSize;
        float alpha = Math.Clamp(config.Opacity, 0f, 1f);

        // Retrieve (or generate) the border sprites for this style + size combo.
        var sprites    = BarStyleFactory.Get(config.BarStyle, h, bs);
        int borderCapW = sprites.Left.Width;    // cap width of the expanded (h+2bs) sprite
        int fillCapW   = h / 2;                 // cap radius of the fill area itself

        // Dark red empty background clipped to the capsule shape.
        DrawRoundedFill(b, x, y, w, h, fillCapW, w, new Color(60, 20, 20) * alpha);

        // Health fill, also clipped to the capsule.
        int fw = (int)(w * pct);
        if (fw > 0)
        {
            DrawRoundedFill(b, x, y, w, h, fillCapW, fw, fill);

            if (config.BarStyle == "Striped")
                DrawBodyStripes(b, x, y, w, h, fillCapW, fw, fill, alpha);
        }

        // Border drawn at the expanded bounds (x-bs, y-bs) so it grows outward.
        Draw9Slice(b, sprites, x - bs, y - bs, w + 2 * bs, h + 2 * bs, borderCapW, border);
    }

    /// <summary>
    /// Draws the fill area row-by-row, clipping each horizontal strip to the
    /// capsule (pill) outline.
    ///
    /// Each pixel row has a left and right inset determined by where the circle
    /// equation intersects that row. The inset formula uses round-half-up rounding
    /// (+0.5 before truncation) to exactly match the transparency boundary baked
    /// into the border sprites — this eliminates any gap or overlap at the edge.
    /// </summary>
    private static void DrawRoundedFill(SpriteBatch b, int x, int y, int totalW, int h,
                                         int capW, int fillW, Color color)
    {
        float cy = (h - 1) / 2f;    // vertical center of the capsule
        float r  = capW - 0.5f;     // radius (pixel-center convention)

        for (int row = 0; row < h; row++)
        {
            float dy  = row + 0.5f - cy;          // distance from row center to capsule center
            float sq  = r * r - dy * dy;          // r² - dy² from the circle equation

            // How many pixels to skip at the left and right edges of this row.
            int inset = sq <= 0 ? capW : (int)(capW - MathF.Sqrt(sq) + 0.5f);

            int rowStart = inset;
            int rowEnd   = Math.Min(fillW, totalW - inset);
            if (rowEnd > rowStart)
                DrawRect(b, x + rowStart, y + row, rowEnd - rowStart, 1, color);
        }
    }

    /// <summary>
    /// Draws diagonal //// stripes over the health fill to create the "Striped" look.
    /// Each row clips to the capsule boundary so stripes never poke outside the pill shape.
    ///
    /// Diagonal formula: pixel (col, row) is in a stripe when (col + row) % period &lt; stripeWidth.
    /// As row increases the stripe shifts left by one pixel, producing the / angle.
    /// </summary>
    private static void DrawBodyStripes(SpriteBatch b, int x, int y, int totalW, int h,
                                         int capW, int fillW, Color fill, float alpha)
    {
        // 50% blend toward white gives enough contrast to be clearly visible.
        Color stripe = Color.Lerp(fill, Color.White, 0.5f);

        const int period = 8; // pixels per diagonal cycle
        const int sw     = 3; // stripe width in pixels

        float cy = (h - 1) / 2f;
        float r  = capW - 0.5f;

        for (int row = 0; row < h; row++)
        {
            // Capsule clip range for this row (same formula as DrawRoundedFill).
            float dy    = row + 0.5f - cy;
            float sq    = r * r - dy * dy;
            int   inset = sq <= 0 ? capW : (int)(capW - MathF.Sqrt(sq) + 0.5f);
            int   rxs   = inset;
            int   rxe   = Math.Min(fillW, totalW - inset);
            if (rxe <= rxs) continue;

            // Find the first stripe segment at or after rxs for this row's diagonal offset.
            int q     = (rxs + row) % period;
            int first = q < sw ? rxs : rxs + (period - q);

            for (int col = first; col < rxe; col += period)
            {
                int segS = Math.Max(col,      rxs);
                int segE = Math.Min(col + sw, rxe);
                if (segE > segS)
                    DrawRect(b, x + segS, y + row, segE - segS, 1, stripe);
            }
        }
    }

    /// <summary>
    /// Draws the three-piece border sprite (Left cap | stretched Middle | Right cap)
    /// to produce a border of any width from the cached textures.
    /// </summary>
    private static void Draw9Slice(SpriteBatch b, BarStyleFactory.BorderSprites s,
                                    int x, int y, int w, int h, int capW, Color color)
    {
        int midW = Math.Max(0, w - capW * 2);
        b.Draw(s.Left,   new Rectangle(x,              y, capW, h), color);
        if (midW > 0)
            b.Draw(s.Middle, new Rectangle(x + capW,       y, midW, h), color);
        b.Draw(s.Right,  new Rectangle(x + capW + midW, y, capW, h), color);
    }

    // ── Color resolution ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the fill color for the current health percentage and config.
    /// Preset color names (Red, Green, Blue …) bypass the gradient/custom logic
    /// for quick one-click color choices.
    /// </summary>
    private static Color ResolveFillColor(float pct, ModConfig config) => config.ColorMode switch
    {
        "Custom"  => new Color(config.FillR,  config.FillG,  config.FillB),
        "Red"     => new Color(220, 30,  30),
        "Green"   => new Color(30,  200, 30),
        "Blue"    => new Color(30,  100, 220),
        "Yellow"  => new Color(220, 200, 30),
        "Cyan"    => new Color(30,  200, 200),
        "White"   => Color.White,
        _         => ResolveGradient(pct, config),  // default = "Gradient"
    };

    /// <summary>
    /// Smoothly interpolates across three color stops based on health percentage:
    ///   0 % → GradStart,  50 % → GradMid,  100 % → GradEnd
    /// </summary>
    private static Color ResolveGradient(float pct, ModConfig config)
    {
        var start = new Color(config.GradStartR, config.GradStartG, config.GradStartB);
        var mid   = new Color(config.GradMidR,   config.GradMidG,   config.GradMidB);
        var end   = new Color(config.GradEndR,   config.GradEndG,   config.GradEndB);

        return pct <= 0.5f
            ? Color.Lerp(start, mid, pct * 2f)           // 0 % … 50 %
            : Color.Lerp(mid,   end, (pct - 0.5f) * 2f); // 50 % … 100 %
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a solid-color rectangle using the game's built-in 1×1 white pixel texture.
    /// This is the cheapest way to draw filled rectangles in XNA/MonoGame.
    /// </summary>
    private static void DrawRect(SpriteBatch b, int x, int y, int w, int h, Color c) =>
        b.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), c);
}
