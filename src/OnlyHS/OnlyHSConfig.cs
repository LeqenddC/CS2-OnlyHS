using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace OnlyHS;

public sealed class OnlyHSConfig : BasePluginConfig
{
    /// <summary>
    /// Chat/console command names without the "css_" prefix. "onlyhs" registers css_onlyhs, so
    /// players can type !onlyhs or /onlyhs in chat and admins can run css_onlyhs from the console/RCON.
    /// </summary>
    [JsonPropertyName("Commands")]
    public List<string> Commands { get; set; } = new() { "onlyhs" };

    /// <summary>
    /// CounterStrikeSharp permission flag(s) required to use the command, e.g. "@css/generic".
    /// Several flags may be separated by commas (all are required). An empty string lets
    /// everyone use the command. The server console always has access.
    /// </summary>
    [JsonPropertyName("Permission")]
    public string Permission { get; set; } = "@css/generic";

    /// <summary>
    /// Restore the original cvar values automatically when a new map starts, so headshot-only
    /// never leaks into the next map. On by default.
    /// </summary>
    [JsonPropertyName("ResetOnMapChange")]
    public bool ResetOnMapChange { get; set; } = true;

    /// <summary>
    /// Which language(s) chat announcements use. "auto" = each player's own CounterStrikeSharp
    /// language (css_lang, falling back to ServerLanguage in core.json). A culture code such as
    /// "en" or "de" forces that language. Listing several, e.g. ["en", "de"], prints one line
    /// per language so everyone can read it.
    /// </summary>
    [JsonPropertyName("MessageLanguages")]
    public List<string> MessageLanguages { get; set; } = new() { "auto" };

    /// <summary>
    /// Cvars applied when headshot-only is enabled. The value each cvar had right before enabling
    /// is remembered and written back when the mode is disabled. mp_damage_headshot_only is CS2's
    /// built-in switch ("Determines whether non-headshot hits do any damage"); the older
    /// mp_damage_scale_ct_body / mp_damage_scale_t_body = 0 approach also works if you prefer it.
    /// </summary>
    [JsonPropertyName("Cvars")]
    public Dictionary<string, string> Cvars { get; set; } = new()
    {
        ["mp_damage_headshot_only"] = "1",
    };
}
