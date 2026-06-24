// ModConfig.cs
// Holds every user-adjustable setting for the Health Bars mod.
//
// SMAPI automatically reads this from "config.json" in the mod folder when the
// game starts, and writes it back when the player clicks Save in the config menu.
// If no config.json exists yet, SMAPI creates one using the default values
// defined here (the = ... parts on each property).

namespace HealthBars;

public class ModConfig
{
    // ── Bar shape ──────────────────────────────────────────────────────────────

    /// <summary>How wide the health bar is, in pixels.</summary>
    public int BarWidth { get; set; } = 120;

    /// <summary>How tall the health bar is, in pixels.</summary>
    public int BarHeight { get; set; } = 24;

    /// <summary>
    /// How many pixels above the character's feet the bar is drawn.
    /// Larger values move the bar higher.
    /// </summary>
    public int VerticalOffset { get; set; } = 140;

    /// <summary>
    /// Horizontal shift from the character's center.
    /// Positive = right, negative = left, 0 = perfectly centered.
    /// </summary>
    public int HorizontalOffset { get; set; } = 0;

    /// <summary>
    /// Overall opacity of the bar (0.0 = invisible, 1.0 = fully solid).
    /// Applied to both the fill and the border.
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Name of a custom-art skin PNG from assets/skins/, or "None" to use procedural styles.
    /// When a skin is active, BorderSize/Color are ignored — the skin art provides its own frame.
    /// BarStyle still applies: it controls the shape of the health fill visible through the skin.
    /// </summary>
    public string SkinName { get; set; } = "Original";

    /// <summary>
    /// Visual style of the bar (only used when SkinName is "None"):
    ///   "Flat"    – plain rectangle with chamfered corners
    ///   "Rounded" – pill / capsule shape
    ///   "Striped" – pill with diagonal highlight stripes on the fill
    /// </summary>
    public string BarStyle { get; set; } = "Rounded";

    /// <summary>
    /// Thickness of the border ring in pixels.
    /// The border grows outward so it never shrinks the fill area.
    /// </summary>
    public int BorderSize { get; set; } = 2;

    // ── Fill color ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Which coloring mode to use for the health fill:
    ///   "Gradient" – smoothly shifts through three colors as health changes
    ///   "Custom"   – one fixed color chosen by the player
    ///   "Red" / "Green" / "Blue" – quick preset colors
    /// </summary>
    public string ColorMode { get; set; } = "Gradient";

    // These three are only used when ColorMode = "Custom".
    // Each channel is 0–255.
    public int FillR { get; set; } = 30;
    public int FillG { get; set; } = 200;
    public int FillB { get; set; } = 30;

    // ── Gradient stops (used when ColorMode = "Gradient") ─────────────────────
    //
    // The gradient has three color stops:
    //   GradStart = color shown at 0 % health  (low / danger)
    //   GradMid   = color shown at 50 % health (mid)
    //   GradEnd   = color shown at 100 % health (full / safe)
    //
    // The fill lerps from Start→Mid as health goes 0→50 %,
    // then from Mid→End as health goes 50→100 %.

    public int GradStartR { get; set; } = 255;
    public int GradStartG { get; set; } = 0;
    public int GradStartB { get; set; } = 0;

    public int GradMidR { get; set; } = 255;
    public int GradMidG { get; set; } = 255;
    public int GradMidB { get; set; } = 0;

    public int GradEndR { get; set; } = 0;
    public int GradEndG { get; set; } = 200;
    public int GradEndB { get; set; } = 0;

    // ── Border color ───────────────────────────────────────────────────────────

    public int BorderR { get; set; } = 30;
    public int BorderG { get; set; } = 30;
    public int BorderB { get; set; } = 30;

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Resets every setting to its default value (same as a fresh install).</summary>
    public void Reset() => CopyFrom(new ModConfig());

    /// <summary>
    /// Copies every setting from <paramref name="source"/> into this instance.
    /// Used to sync the "live preview" config with the saved config whenever the
    /// settings menu opens or the player clicks Save.
    /// </summary>
    public void CopyFrom(ModConfig source)
    {
        BarWidth         = source.BarWidth;
        BarHeight        = source.BarHeight;
        VerticalOffset   = source.VerticalOffset;
        HorizontalOffset = source.HorizontalOffset;
        Opacity          = source.Opacity;
        ColorMode        = source.ColorMode;
        SkinName         = source.SkinName;
        BarStyle         = source.BarStyle;
        BorderSize       = source.BorderSize;
        FillR            = source.FillR;
        FillG            = source.FillG;
        FillB            = source.FillB;
        GradStartR       = source.GradStartR;
        GradStartG       = source.GradStartG;
        GradStartB       = source.GradStartB;
        GradMidR         = source.GradMidR;
        GradMidG         = source.GradMidG;
        GradMidB         = source.GradMidB;
        GradEndR         = source.GradEndR;
        GradEndG         = source.GradEndG;
        GradEndB         = source.GradEndB;
        BorderR          = source.BorderR;
        BorderG          = source.BorderG;
        BorderB          = source.BorderB;
    }
}
