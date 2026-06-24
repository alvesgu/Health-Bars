// BarStyleFactory.cs
// Generates and caches the border sprite textures for each bar style.
//
// WHY GENERATE TEXTURES IN CODE?
// The border shapes (rounded pill, flat rectangle with chamfers) are perfectly
// described by math, so we build small GPU textures at runtime instead of
// shipping image files. This keeps the mod folder clean and makes it trivial to
// support any bar height or border thickness the player picks.
//
// HOW THE BORDER WORKS (important for understanding the pixel math below)
// ─────────────────────────────────────────────────────────────────────────
// A bar that is W×H pixels wide has a border that grows OUTWARD by `bs` pixels.
// We build the border sprite at the EXPANDED size (W × (H + 2*bs)) so that
// drawing it at (x-bs, y-bs) lines up perfectly with the fill drawn at (x, y).
// The interior of the border sprite is transparent, revealing the fill beneath.
//
// The border is split into three reusable pieces (9-slice / 3-slice pattern):
//   Left  cap  – the rounded or chamfered left end
//   Middle     – a 1-pixel-wide repeating center strip (top + bottom edge only)
//   Right cap  – mirror of the left cap
//
// This way a single cached sprite set works for any bar width.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace HealthBars;

public static class BarStyleFactory
{
    /// <summary>The three textures needed to draw one border style.</summary>
    public record BorderSprites(Texture2D Left, Texture2D Middle, Texture2D Right);

    // Cache key: (style name, bar height, border thickness).
    // We regenerate only when these change (e.g. player adjusts Border Size slider).
    private static readonly Dictionary<(string style, int height, int borderSize), BorderSprites> _cache = new();

    /// <summary>
    /// Returns cached border sprites for the given style/height/border combination,
    /// generating them on first use.
    /// </summary>
    public static BorderSprites Get(string style, int height, int borderSize = 2)
    {
        // Clamp border size so it can never exceed half the bar height
        // (which would make the interior disappear entirely).
        int bs  = Math.Clamp(borderSize, 1, Math.Max(1, height / 2 - 1));
        var key = (style, height, bs);

        if (!_cache.TryGetValue(key, out var sprites))
        {
            sprites      = Build(style, height, bs);
            _cache[key]  = sprites;
        }

        return sprites;
    }

    /// <summary>
    /// Disposes every cached texture and clears the cache.
    /// Call this whenever the player changes Style or Border Size so stale
    /// GPU textures don't accumulate in memory.
    /// </summary>
    public static void ClearCache()
    {
        foreach (var s in _cache.Values)
        {
            s.Left.Dispose();
            s.Middle.Dispose();
            s.Right.Dispose();
        }
        _cache.Clear();
    }

    // ── Internal builders ─────────────────────────────────────────────────────

    private static BorderSprites Build(string style, int h, int bs)
    {
        // H is the EXPANDED height (bar height + border on top + border on bottom).
        // Sprites are built at this size so the border ring sits outside the fill area.
        int H      = h + 2 * bs;
        int capW   = Math.Max(2, H / 2);       // cap width = half the expanded height
        var device = Game1.graphics.GraphicsDevice;

        return style switch
        {
            "Rounded" or "Striped" => BuildRounded(device, H, capW, bs),
            _                      => BuildFlat(device, H, capW, bs),
        };
    }

    // ── Flat style ────────────────────────────────────────────────────────────
    // Rectangular bar with small diagonal chamfers cut from each corner.

    private static BorderSprites BuildFlat(GraphicsDevice device, int h, int capW, int bs)
    {
        // `cut` is how many pixels to chamfer (diagonal cut) at each corner.
        // Kept proportional to bar height so it looks good at any size.
        int cut   = Math.Min(2, h / 6);
        var left  = MakeTexture(device, capW, h, (x, y) => FlatCapPixel(x, y, h, capW, cut, bs, isLeft: true));
        var mid   = MakeTexture(device, 1,    h, (_, y) => MidPixel(y, h, bs));
        var right = MakeTexture(device, capW, h, (x, y) => FlatCapPixel(x, y, h, capW, cut, bs, isLeft: false));
        return new BorderSprites(left, mid, right);
    }

    private static Color FlatCapPixel(int x, int y, int h, int capW, int cut, int bs, bool isLeft)
    {
        // Mirror x for the right cap so both caps share one algorithm.
        int px = isLeft ? x : (capW - 1 - x);

        // Cut the corner triangles — these pixels are always transparent.
        if (y < cut          && px < cut - y)             return Color.Transparent;
        if (y > h - 1 - cut  && px < cut - (h - 1 - y))  return Color.Transparent;

        // Determine whether this pixel belongs to the border ring.
        bool topBorder  = y  < bs;
        bool botBorder  = y  >= h - bs;
        bool leftBorder = px < bs;

        // The first non-cut pixel on the chamfer diagonal also gets drawn as border.
        bool topChamfer = y < cut          && px == cut - y;
        bool botChamfer = y > h - 1 - cut  && px == cut - (h - 1 - y);

        if (topBorder || botBorder || leftBorder || topChamfer || botChamfer)
            return Color.White;     // border pixel — renderer tints this with the border color

        return Color.Transparent;   // interior — fill shows through here
    }

    // ── Rounded / Striped style ───────────────────────────────────────────────
    // Pill / capsule shape using circle math.
    // "Striped" uses the same border shape; the stripes are drawn separately in the renderer.

    private static BorderSprites BuildRounded(GraphicsDevice device, int h, int capW, int bs)
    {
        var left  = MakeTexture(device, capW, h, (x, y) => RoundedCapPixel(x, y, h, capW, bs, isLeft: true));
        var mid   = MakeTexture(device, 1,    h, (_, y) => MidPixel(y, h, bs));
        var right = MakeTexture(device, capW, h, (x, y) => RoundedCapPixel(x, y, h, capW, bs, isLeft: false));
        return new BorderSprites(left, mid, right);
    }

    private static Color RoundedCapPixel(int x, int y, int h, int capW, int bs, bool isLeft)
    {
        // Mirror for the right cap.
        int px = isLeft ? x : (capW - 1 - x);

        // The cap is a semicircle centered at (capW, midY).
        // We test each pixel's center point (px + 0.5, y + 0.5) against two circles:
        //   outerR – outer edge of the border ring
        //   innerR – inner edge (= where the fill starts)
        float cx     = capW;
        float cy     = (h - 1) / 2f;
        float outerR = capW - 0.5f;
        float innerR = outerR - bs;

        float dx   = cx - px - 0.5f;   // distance from pixel center to circle center X
        float dy   = cy - y  - 0.5f;   // distance from pixel center to circle center Y
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > outerR) return Color.Transparent;    // outside the capsule entirely

        // Inside the inner radius AND in the "body" rows → show fill through here.
        if (dist < innerR && y >= bs && y < h - bs) return Color.Transparent;

        return Color.White;     // border ring pixel
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    // Middle strip: only the top and bottom `bs` rows are opaque (border), the rest transparent.
    private static Color MidPixel(int y, int h, int bs) =>
        (y < bs || y >= h - bs) ? Color.White : Color.Transparent;

    /// <summary>
    /// Allocates a GPU texture of size w×h and fills it using the provided
    /// per-pixel color function. Efficient for small textures like these caps.
    /// </summary>
    private static Texture2D MakeTexture(GraphicsDevice device, int w, int h, Func<int, int, Color> pixel)
    {
        var tex  = new Texture2D(device, w, h);
        var data = new Color[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                data[y * w + x] = pixel(x, y);

        tex.SetData(data);
        return tex;
    }
}
