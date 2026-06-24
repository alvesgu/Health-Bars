// IGenericModConfigMenuApi.cs
// A minimal interface stub for the Generic Mod Config Menu (GMCM) API.
//
// WHAT IS GMCM?
// ─────────────
// Generic Mod Config Menu is a popular SMAPI mod by spacechase0 that provides a
// shared in-game settings screen. Instead of editing config.json manually, players
// can open the game's main menu → "Mod Options" and adjust every mod's settings
// through a friendly UI.
//
// HOW THIS FILE WORKS
// ───────────────────
// SMAPI lets mods expose an API object to other mods through
// Helper.ModRegistry.GetApi<T>(). The type parameter T must be an interface —
// SMAPI creates a proxy object at runtime that wraps the real GMCM implementation.
//
// This file declares only the GMCM methods that this mod actually uses. The real
// GMCM API has many more, but we only stub what we need so unused methods don't
// add noise or maintenance burden.
//
// If GMCM is not installed, GetApi<T>() returns null and we skip registration
// entirely (see ModEntry.cs). The health bar still works — players just edit
// config.json by hand instead.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace HealthBars;

public interface IGenericModConfigMenuApi
{
    /// <summary>
    /// Registers this mod with GMCM so it appears in the options list.
    /// Must be called before adding any options.
    /// </summary>
    /// <param name="reset">Called when the player clicks "Reset to Default".</param>
    /// <param name="save">Called when the player clicks "Save".</param>
    /// <param name="titleScreenOnly">If true, options are only accessible from the title screen.</param>
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    /// <summary>Adds a text heading inside the options page — purely visual, no value.</summary>
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

    /// <summary>Adds an integer number slider (e.g. bar width, border size).</summary>
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name,
        Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null,
        Func<int, string>? formatValue = null, string? fieldId = null);

    /// <summary>Adds a float number slider (e.g. transparency 0.0–1.0).</summary>
    void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name,
        Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null,
        Func<float, string>? formatValue = null, string? fieldId = null);

    /// <summary>Adds a dropdown/cycle control for a string value (e.g. bar style, color mode).</summary>
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name,
        Func<string>? tooltip = null, string[]? allowedValues = null,
        Func<string, string>? formatAllowedValue = null, string? fieldId = null);

    /// <summary>
    /// Adds a fully custom control drawn via a callback every frame.
    /// We use this for the live preview panel and the R/G/B color pickers,
    /// because GMCM's built-in controls cache their values internally and
    /// would not reflect changes made by swatch clicks or preset syncs.
    /// </summary>
    void AddComplexOption(IManifest mod, Func<string> name, Action<SpriteBatch, Vector2> draw,
        Func<string>? tooltip = null, Action? beforeMenuOpened = null, Action? beforeSave = null,
        Action? afterSave = null, Action? beforeReset = null, Action? afterReset = null,
        Action? beforeMenuClosed = null, Func<int>? height = null, string? fieldId = null);

    /// <summary>
    /// Registers a callback fired whenever any option's value changes.
    /// The callback receives the fieldId and the new value as an object.
    /// Used to keep _liveConfig in sync with the GMCM controls.
    /// </summary>
    void OnFieldChanged(IManifest mod, Action<string, object> onChange);

    /// <summary>Begins a new sub-page. Options added after this call appear on that page.</summary>
    void AddPage(IManifest mod, string pageId, Func<string>? pageTitle = null);

    /// <summary>Adds a clickable link that navigates the player to a sub-page.</summary>
    void AddPageLink(IManifest mod, string pageId, Func<string> text, Func<string>? tooltip = null);
}
