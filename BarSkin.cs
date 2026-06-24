// BarSkin.cs
// Represents one custom-art tilesheet skin for the health bar.
//
// TILESHEET FORMAT
// ────────────────
// A skin is a single PNG with exactly 3 rows and (4 + N) columns of square tiles.
// Everything is derived from the image dimensions:
//
//   tileSize     = imageHeight / 3          (must divide evenly)
//   N (fill cols)= imageWidth / tileSize − 4
//
// Column layout (left to right in the PNG):
//
//   Col 0         Col 1         Col 2         Col 3         Col 4 … 4+N−1
//   ┌──────┐      ┌──────┐      ┌──────┐      ┌──────┐      ┌──────┬──────┐
//   │ LB   │      │ LI   │      │ RI   │      │ RB   │      │ F₁   │ F₂… │  ← top row
//   ├──────┤      ├──────┤      ├──────┤      ├──────┤      ├──────┼──────┤
//   │ LB   │      │ LI   │      │ RI   │      │ RB   │      │ F₁   │ F₂… │  ← middle row
//   ├──────┤      ├──────┤      ├──────┤      ├──────┤      ├──────┼──────┤
//   │ LB   │      │ LI   │      │ RI   │      │ RB   │      │ F₁   │ F₂… │  ← bottom row
//   └──────┘      └──────┘      └──────┘      └──────┘      └──────┴──────┘
//  Left border  Left inner   Right inner  Right border  Repeating fill
//
//  Columns are ordered left-to-right in the PNG exactly as they appear on screen.
//
// Bar assembly order (left → right on screen):
//   [Col 0: LB] [Col 1: LI] [Col 4+: fill, repeating] [Col 2: RI] [Col 3: RB]
//
// The fill columns (col 4 onward) tile across the body between the two inner cap tiles.
//
// If there is only 1 fill column (N=1), that column repeats across the entire body.
// Multiple fill columns play left-to-right, then loop.
//
// HOW IT RENDERS
// ──────────────
// The skin is drawn ON TOP of the programmatic health fill so transparent pixels
// in the skin let the fill color and gradient show through.
// Drawing order:
//   1. Colored health fill  (0 … health% of bar width)
//   2. Skin tiles           (left cap | repeating body | right cap)
//
// No programmatic border is drawn when a skin is active — the skin art defines
// the bar's own frame/border look.

using Microsoft.Xna.Framework.Graphics;

namespace HealthBars;

public class BarSkin
{
    /// <summary>Display name shown in the config menu (the PNG file name without extension).</summary>
    public string Name { get; }

    /// <summary>The loaded tilesheet texture (3 rows × (4+N) columns of square tiles).</summary>
    public Texture2D Sheet { get; }

    /// <summary>
    /// Width (and height) of each source tile in pixels.
    /// Derived as <c>Sheet.Height / 3</c>.
    /// </summary>
    public int TileSize { get; }

    /// <summary>
    /// Number of repeatable fill columns (columns 4 and beyond).
    /// Derived as <c>Sheet.Width / TileSize − 4</c>.
    /// </summary>
    public int FillColumns { get; }

    public BarSkin(string name, Texture2D sheet, int tileSize, int fillColumns)
    {
        Name        = name;
        Sheet       = sheet;
        TileSize    = tileSize;
        FillColumns = fillColumns;
    }
}
