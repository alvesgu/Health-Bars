// ModEntry.cs
// The mod's entry point — the first code Stardew Valley / SMAPI runs.
//
// WHAT THIS FILE DOES
// ───────────────────
//  1. Loads the player's saved settings from config.json (ModConfig).
//  2. Subscribes to game events so the bar is drawn each frame in-game.
//  3. Registers the optional Generic Mod Config Menu (GMCM) so the player can
//     adjust every setting through an in-game UI without editing JSON.
//
// GMCM LIVE-PREVIEW PATTERN
// ─────────────────────────
// GMCM has a quirk: when a slider is moved, it calls setValue() immediately so
// the preview can update — but when the player clicks Save, it calls setValue()
// again using its own internally cached value, which may be stale if the player
// also clicked a swatch or triggered a preset.
//
// To avoid that bug entirely, we maintain TWO config objects:
//   _config     – the persisted config (read on launch, written on Save).
//   _liveConfig – a working copy updated every time any GMCM control changes.
//
// The live preview always reads from _liveConfig, so it reflects changes
// instantly. On Save, _liveConfig is copied into _config and written to disk.
// On menu open, _liveConfig is reset from _config so unsaved changes are discarded.
//
// For R/G/B color values specifically, we bypass GMCM's number options entirely
// and draw custom sliders inside AddComplexOption callbacks. These callbacks run
// every frame and read directly from _liveConfig, so swatch clicks and preset
// changes are always immediately visible — GMCM never touches these values.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace HealthBars;

public class ModEntry : Mod
{
    // Persisted config (only written to disk on Save).
    private ModConfig _config = null!;

    // Live working copy (updated by every GMCM control, drives the live preview).
    private ModConfig _liveConfig = null!;

    // Draws the health bar both in-game and in the GMCM preview panel.
    private HealthBarRenderer _renderer = null!;

    // ── Color palette ─────────────────────────────────────────────────────────
    // Ten preset swatches shown in each color picker row.
    // Players can click any swatch to instantly apply that color to the R/G/B sliders.
    private static readonly Color[] Palette = new[]
    {
        new Color(30,  30,  30),   // Near-black
        new Color(255, 255, 255),  // White
        new Color(220, 30,  30),   // Red
        new Color(220, 130, 30),   // Orange
        new Color(220, 200, 30),   // Yellow
        new Color(30,  200, 30),   // Green
        new Color(30,  200, 130),  // Teal
        new Color(30,  200, 200),  // Cyan
        new Color(30,  100, 220),  // Blue
        new Color(130, 30,  220),  // Purple
    };

    // ── Mod entry point ───────────────────────────────────────────────────────

    /// <summary>
    /// SMAPI calls this once at startup, before the game's title screen appears.
    /// Set up everything here: load config, create objects, subscribe to events.
    /// </summary>
    public override void Entry(IModHelper helper)
    {
        _config     = helper.ReadConfig<ModConfig>();
        _liveConfig = new ModConfig();
        _liveConfig.CopyFrom(_config);
        _renderer   = new HealthBarRenderer(_config);

        helper.Events.Display.RenderedWorld  += OnRenderedWorld;
        helper.Events.Display.MenuChanged    += OnMenuChanged;
        helper.Events.GameLoop.GameLaunched  += OnGameLaunched;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when any menu opens or closes (including GMCM).
    /// When the config menu opens, reset _liveConfig from _config so the player
    /// always starts editing from the last-saved state.
    /// </summary>
    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu?.GetType().FullName?.Contains("GenericModConfigMenu") == true)
            _liveConfig.CopyFrom(_config);
    }

    /// <summary>
    /// Fired once after all mods are loaded and the game's title screen appears.
    /// Safe to access other mods' APIs here.
    /// </summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Load custom art skins from assets/skins/*.png before GMCM registration
        // so the skin names are available for the dropdown allowed-values list.
        SkinRegistry.Load(Helper, Monitor);

        // Try to get GMCM's API object. If GMCM is not installed, gmcm is null
        // and we simply skip registration — the mod still works, just no in-game UI.
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        // Register this mod with GMCM.
        // reset: called when the player clicks "Reset to Default".
        // save:  called when the player clicks "Save". Copy live → saved, then write to disk.
        gmcm.Register(
            mod:             ModManifest,
            reset:           () => _liveConfig.Reset(),
            save:            () => { _config.CopyFrom(_liveConfig); Helper.WriteConfig(_config); },
            titleScreenOnly: false
        );

        // ═══════════════════════════════════════════════════════════════════
        //  MAIN PAGE
        // ═══════════════════════════════════════════════════════════════════

        gmcm.AddSectionTitle(ModManifest, () => "Health Bar");

        // The live preview panel — drawn every frame via AddComplexOption so it
        // immediately reflects any slider or swatch change.
        gmcm.AddComplexOption(ModManifest,
            name:   () => "Preview",
            draw:   (b, pos) => HealthBarRenderer.DrawPreview(b, pos + new Vector2(8, 8), _liveConfig),
            height: () => HealthBarRenderer.GetPreviewHeight());

        // ── Shape / layout controls ───────────────────────────────────────

        gmcm.AddNumberOption(ModManifest,
            getValue: () => _liveConfig.BarWidth,
            setValue: v  => _liveConfig.BarWidth = v,
            name:     () => "Width",
            tooltip:  () => "Width of the health bar in pixels.",
            min: 40, max: 160, interval: 4,
            fieldId: "bar-width");

        gmcm.AddNumberOption(ModManifest,
            getValue: () => _liveConfig.BarHeight,
            setValue: v  => _liveConfig.BarHeight = v,
            name:     () => "Height",
            tooltip:  () => "Height of the health bar in pixels.",
            min: 4, max: 48, interval: 2,
            fieldId: "bar-height");

        gmcm.AddNumberOption(ModManifest,
            getValue: () => _liveConfig.VerticalOffset,
            setValue: v  => _liveConfig.VerticalOffset = v,
            name:     () => "Vertical Offset",
            tooltip:  () => "Pixels above the character to draw the bar.",
            min: 60, max: 200, interval: 4,
            fieldId: "vertical-offset");

        gmcm.AddNumberOption(ModManifest,
            getValue: () => _liveConfig.HorizontalOffset,
            setValue: v  => _liveConfig.HorizontalOffset = v,
            name:     () => "Horizontal Offset",
            tooltip:  () => "Horizontal shift from center. Positive = right.",
            min: -100, max: 100, interval: 4,
            fieldId: "horizontal-offset");

        gmcm.AddNumberOption(ModManifest,
            getValue:     () => _liveConfig.Opacity,
            setValue:     v  => _liveConfig.Opacity = v,
            name:         () => "Opacity",
            tooltip:      () => "Bar opacity. 1.0 = fully visible, 0.0 = invisible.",
            min: 0.0f, max: 1.0f, interval: 0.05f,
            formatValue:  v  => $"{(int)(v * 100)}%",
            fieldId: "opacity");

        // ── Skin / style ──────────────────────────────────────────────────

        gmcm.AddSectionTitle(ModManifest, () => "Bar Style");

        // Skin picker: "None" uses the procedural Style below; any other value
        // loads a PNG tilesheet from assets/skins/ and ignores Style + Border.
        var skinOptions = new[] { "None" }.Concat(SkinRegistry.Names).ToArray();
        gmcm.AddTextOption(ModManifest,
            getValue:           () => _liveConfig.SkinName,
            setValue:           v  => _liveConfig.SkinName = v,
            name:               () => "Skin",
            tooltip:            () => "Custom art skin from assets/skins/. When set, Style and Border settings below are ignored.",
            allowedValues:      skinOptions,
            formatAllowedValue: v  => v,
            fieldId: "skin-name");

        gmcm.AddTextOption(ModManifest,
            getValue:           () => _liveConfig.BarStyle,
            setValue:           v  => { _liveConfig.BarStyle = v; BarStyleFactory.ClearCache(); },
            name:               () => "Style",
            tooltip:            () => "Flat: solid rectangle. Rounded: pill shape. Striped: pill with diagonal stripes. (Ignored when a Skin is selected.)",
            allowedValues:      new[] { "Flat", "Rounded", "Striped" },
            formatAllowedValue: v  => v,
            fieldId: "bar-style");

        gmcm.AddNumberOption(ModManifest,
            getValue: () => _liveConfig.BorderSize,
            setValue: v  => { _liveConfig.BorderSize = v; BarStyleFactory.ClearCache(); },
            name:     () => "Border Thickness",
            tooltip:  () => "Border ring thickness in pixels. (Ignored when a Skin is selected.)",
            min: 1, max: 10, interval: 1,
            fieldId: "border-size");

        // ── Fill color ────────────────────────────────────────────────────

        gmcm.AddSectionTitle(ModManifest, () => "Fill Color");

        // Choosing "Red", "Green", or "Blue" here also syncs FillR/G/B to the
        // matching preset via OnFieldChanged, so custom sliders stay in sync.
        gmcm.AddTextOption(ModManifest,
            getValue:           () => _liveConfig.ColorMode,
            setValue:           v  => _liveConfig.ColorMode = v,
            name:               () => "Color Mode",
            tooltip:            () => "Gradient: 3-stop shift with health. Custom: fixed color. Others: named presets.",
            allowedValues:      new[] { "Gradient", "Custom", "Blue", "Green", "Red" },
            formatAllowedValue: v  => v,
            fieldId: "color-mode");

        // Links to sub-pages for detailed color editing.
        gmcm.AddPageLink(ModManifest, "gradient",
            text:    () => "Edit Gradient Colors →",
            tooltip: () => "Customize the 3 gradient stops (Low / Mid / Full health). Used when Mode = Gradient.");

        gmcm.AddPageLink(ModManifest, "custom-fill",
            text:    () => "Edit Custom Fill Color →",
            tooltip: () => "Choose a single fixed fill color. Used when Mode = Custom.");

        // ── Border color ──────────────────────────────────────────────────

        gmcm.AddSectionTitle(ModManifest, () => "Border Color");
        AddColorPicker(gmcm,
            () => _liveConfig.BorderR, v => _liveConfig.BorderR = v,
            () => _liveConfig.BorderG, v => _liveConfig.BorderG = v,
            () => _liveConfig.BorderB, v => _liveConfig.BorderB = v);

        // ── Field changed handler ─────────────────────────────────────────
        // GMCM fires this callback whenever any registered control's value changes.
        // We mirror the change into _liveConfig so the preview updates immediately.
        // R/G/B values are NOT handled here because they use custom sliders inside
        // AddComplexOption, which write to _liveConfig directly every frame.
        gmcm.OnFieldChanged(ModManifest, (fieldId, value) =>
        {
            switch (fieldId)
            {
                case "bar-width"         when value is int    bw:  _liveConfig.BarWidth        = bw;  break;
                case "bar-height"        when value is int    bh:  _liveConfig.BarHeight        = bh;  BarStyleFactory.ClearCache(); break;
                case "vertical-offset"   when value is int    vo:  _liveConfig.VerticalOffset   = vo;  break;
                case "horizontal-offset" when value is int    ho:  _liveConfig.HorizontalOffset = ho;  break;
                case "opacity"           when value is float  tr:  _liveConfig.Opacity     = tr;  break;
                case "skin-name"         when value is string sn:  _liveConfig.SkinName          = sn;  break;
                case "bar-style"         when value is string s:   _liveConfig.BarStyle         = s;   BarStyleFactory.ClearCache(); break;
                case "border-size"       when value is int    bsz: _liveConfig.BorderSize       = bsz; BarStyleFactory.ClearCache(); break;

                // When a named color mode is selected, also sync FillR/G/B to the
                // matching preset so custom-fill sliders show the correct color.
                case "color-mode" when value is string cm:
                    _liveConfig.ColorMode = cm;
                    (int pr, int pg, int pb) = cm switch
                    {
                        "Red"    => (220, 30,  30),
                        "Green"  => (30,  200, 30),
                        "Blue"   => (30,  100, 220),
                        "Yellow" => (220, 200, 30),
                        "Cyan"   => (30,  200, 200),
                        "White"  => (255, 255, 255),
                        _        => (-1,  -1,  -1),
                    };
                    if (pr >= 0) { _liveConfig.FillR = pr; _liveConfig.FillG = pg; _liveConfig.FillB = pb; }
                    break;
            }
        });

        // ═══════════════════════════════════════════════════════════════════
        //  PAGE: Gradient Colors
        // ═══════════════════════════════════════════════════════════════════

        gmcm.AddPage(ModManifest, "gradient", () => "Gradient Colors");

        gmcm.AddSectionTitle(ModManifest, () => "Gradient (Low → Mid → Full Health)");

        // A horizontal color strip that blends across all three stops in real time.
        gmcm.AddComplexOption(ModManifest,
            name:   () => "Preview",
            draw:   DrawGradientStrip,
            height: () => 36);

        gmcm.AddSectionTitle(ModManifest, () => "Low Health (0%)");
        AddColorPicker(gmcm,
            () => _liveConfig.GradStartR, v => _liveConfig.GradStartR = v,
            () => _liveConfig.GradStartG, v => _liveConfig.GradStartG = v,
            () => _liveConfig.GradStartB, v => _liveConfig.GradStartB = v);

        gmcm.AddSectionTitle(ModManifest, () => "Mid Health (50%)");
        AddColorPicker(gmcm,
            () => _liveConfig.GradMidR, v => _liveConfig.GradMidR = v,
            () => _liveConfig.GradMidG, v => _liveConfig.GradMidG = v,
            () => _liveConfig.GradMidB, v => _liveConfig.GradMidB = v);

        gmcm.AddSectionTitle(ModManifest, () => "Full Health (100%)");
        AddColorPicker(gmcm,
            () => _liveConfig.GradEndR, v => _liveConfig.GradEndR = v,
            () => _liveConfig.GradEndG, v => _liveConfig.GradEndG = v,
            () => _liveConfig.GradEndB, v => _liveConfig.GradEndB = v);

        // ═══════════════════════════════════════════════════════════════════
        //  PAGE: Custom Fill Color
        // ═══════════════════════════════════════════════════════════════════

        gmcm.AddPage(ModManifest, "custom-fill", () => "Custom Fill Color");

        gmcm.AddSectionTitle(ModManifest, () => "Fill Color (used when Mode = Custom)");
        AddColorPicker(gmcm,
            () => _liveConfig.FillR, v => _liveConfig.FillR = v,
            () => _liveConfig.FillG, v => _liveConfig.FillG = v,
            () => _liveConfig.FillB, v => _liveConfig.FillB = v);
    }

    // ── GMCM color picker helpers ─────────────────────────────────────────────

    /// <summary>
    /// Registers one color picker (palette swatches + R/G/B sliders) as a single
    /// GMCM complex option. The get/set function pairs let this method work for any
    /// of the five color roles (fill, grad-start, grad-mid, grad-end, border).
    /// </summary>
    private void AddColorPicker(IGenericModConfigMenuApi gmcm,
        Func<int> getR, Action<int> setR,
        Func<int> getG, Action<int> setG,
        Func<int> getB, Action<int> setB)
    {
        // Height breakdown: 36 px palette row + 8 px gap + 3 × (24 px slider row + 4 px gap) = 124 px.
        // Everything is a single complex option so GMCM never touches R/G/B with stale cached values.
        gmcm.AddComplexOption(ModManifest,
            name:   () => "",
            draw:   (b, pos) => DrawColorPicker(b, pos, getR, setR, getG, setG, getB, setB),
            height: () => 124);
    }

    /// <summary>
    /// Draws the palette swatches and R/G/B sliders for one color role.
    /// Called every frame by GMCM's render loop so all values are always current.
    ///
    /// The get/set lambdas are captured from AddColorPicker, pointing to whichever
    /// three _liveConfig properties this picker controls.
    /// </summary>
    private static void DrawColorPicker(SpriteBatch b, Vector2 pos,
        Func<int> getR, Action<int> setR,
        Func<int> getG, Action<int> setG,
        Func<int> getB, Action<int> setB)
    {
        // Read current channel values fresh every frame.
        int r  = getR();
        int g  = getG();
        int bv = getB();
        var current = new Color(r, g, bv);

        int sx = (int)pos.X + 8;
        int sy = (int)pos.Y + 4;
        int sh = 28; // swatch height

        // ── Current color preview ─────────────────────────────────────────
        // Dark border, then the actual color inside.
        b.Draw(Game1.staminaRect, new Rectangle(sx,      sy,      50,      sh),     new Color(20, 20, 20));
        b.Draw(Game1.staminaRect, new Rectangle(sx + 2,  sy + 2,  46,      sh - 4), current);

        // ── Palette swatches ──────────────────────────────────────────────
        var  mouse    = Mouse.GetState();
        bool clicking = mouse.LeftButton == ButtonState.Pressed;
        int  px0      = sx + 58;    // x of the first swatch

        for (int i = 0; i < Palette.Length; i++)
        {
            int rx   = px0 + i * 22;
            var rect = new Rectangle(rx, sy, 20, sh);

            // Draw a white outline around whichever swatch closely matches the current color.
            bool matches =
                Math.Abs(Palette[i].R - current.R) < 10 &&
                Math.Abs(Palette[i].G - current.G) < 10 &&
                Math.Abs(Palette[i].B - current.B) < 10;
            if (matches)
                b.Draw(Game1.staminaRect, new Rectangle(rx - 1, sy - 1, 22, sh + 2), Color.White);

            b.Draw(Game1.staminaRect, rect, Palette[i]);

            // Clicking a swatch immediately updates all three channels in _liveConfig.
            if (clicking && rect.Contains(mouse.X, mouse.Y))
            {
                setR(Palette[i].R);
                setG(Palette[i].G);
                setB(Palette[i].B);
            }
        }

        // ── R / G / B channel sliders ─────────────────────────────────────
        // Custom sliders rather than GMCM's AddNumberOption so they always reflect
        // the live value, even after a swatch click or preset sync.
        const int sliderW = 220;
        DrawChannelSlider(b, mouse, clicking, sx, sy + 36, sliderW, r,  setR, new Color(200,  50,  50));
        DrawChannelSlider(b, mouse, clicking, sx, sy + 64, sliderW, g,  setG, new Color( 50, 200,  50));
        DrawChannelSlider(b, mouse, clicking, sx, sy + 92, sliderW, bv, setB, new Color( 50,  50, 200));
    }

    /// <summary>
    /// Draws one R, G, or B channel slider row: a colored dot label, a track, a
    /// fill bar, and a draggable handle. Handles mouse drag to update the value.
    /// </summary>
    private static void DrawChannelSlider(SpriteBatch b, MouseState mouse, bool clicking,
        int x, int y, int width, int value, Action<int> setValue, Color channelColor)
    {
        const int trackH  = 8;    // height of the track bar
        const int handleW = 4;    // width of the drag handle
        const int handleH = 16;   // height of the drag handle
        const int rowH    = 24;   // total row height (used for vertical centering)

        int trackY  = y + (rowH - trackH)  / 2;
        int handleY = y + (rowH - handleH) / 2;

        // Small colored square as a label (red, green, or blue dot).
        b.Draw(Game1.staminaRect, new Rectangle(x, y + (rowH - 10) / 2, 10, 10), channelColor);

        int trackX = x + 16;    // leave room for the dot label

        // Track: dark background, then colored fill up to the current value.
        b.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, width,  trackH), new Color(60, 60, 60));
        int fillW = (int)(value / 255f * width);
        if (fillW > 0)
            b.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, fillW, trackH), channelColor);

        // White handle positioned at the current value.
        b.Draw(Game1.staminaRect, new Rectangle(trackX + fillW - 2, handleY, handleW, handleH), Color.White);

        // If the player is holding the mouse button inside this row, update the value.
        if (clicking && new Rectangle(trackX, y, width, rowH).Contains(mouse.X, mouse.Y))
            setValue((int)(Math.Clamp((mouse.X - trackX) / (float)width, 0f, 1f) * 255 + 0.5f));
    }

    /// <summary>
    /// Draws a horizontal gradient strip on the Gradient Colors sub-page.
    /// Reads live from _liveConfig every frame so it updates as sliders are dragged.
    /// </summary>
    private void DrawGradientStrip(SpriteBatch b, Vector2 pos)
    {
        var start = new Color(_liveConfig.GradStartR, _liveConfig.GradStartG, _liveConfig.GradStartB);
        var mid   = new Color(_liveConfig.GradMidR,   _liveConfig.GradMidG,   _liveConfig.GradMidB);
        var end   = new Color(_liveConfig.GradEndR,   _liveConfig.GradEndG,   _liveConfig.GradEndB);

        // Divide the strip into 20 evenly-spaced segments and fill each with the
        // interpolated color at that position.
        const int segCount = 20;
        const int stripW   = 276;
        int segW = stripW / segCount;
        int sy   = (int)pos.Y + 4;

        for (int i = 0; i < segCount; i++)
        {
            float t = (i + 0.5f) / segCount;   // center of this segment, 0.0 – 1.0
            Color c = t <= 0.5f
                ? Color.Lerp(start, mid, t * 2f)
                : Color.Lerp(mid,   end, (t - 0.5f) * 2f);
            b.Draw(Game1.staminaRect, new Rectangle((int)pos.X + 8 + i * segW, sy, segW + 1, 28), c);
        }
    }

    // ── In-game rendering ─────────────────────────────────────────────────────

    /// <summary>
    /// Fired after the world is rendered each frame.
    /// Skips drawing if there is no world loaded, an event is playing, or HUD is hidden.
    /// </summary>
    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp || !Game1.displayHUD || Game1.fadeToBlack || Game1.globalFade)
            return;

        _renderer.Draw(e.SpriteBatch, Game1.player);
    }
}
