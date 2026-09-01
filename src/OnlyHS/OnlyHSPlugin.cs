using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace OnlyHS;

/// <summary>
/// !onlyhs toggles headshot-only damage on and off. Enabling remembers the current
/// values; disabling (or an unload, or optionally a map change) writes them back.
/// </summary>
public sealed class OnlyHSPlugin : BasePlugin, IPluginConfig<OnlyHSConfig>
{
    public override string ModuleName => "CS2-OnlyHS";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "LeqenddC";
    public override string ModuleDescription => "Toggle headshot-only damage with a chat command";

    public OnlyHSConfig Config { get; set; } = new();

    private CvarToggle? _toggle;
    private string[] _requiredFlags = Array.Empty<string>();

    public void OnConfigParsed(OnlyHSConfig config)
    {
        Config = config;
        _requiredFlags = config.Permission
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (config.Commands.Count == 0)
            Logger.LogWarning("Commands is empty - the plugin has nothing to respond to");
        if (config.Cvars.Count == 0)
            Logger.LogWarning("Cvars is empty - enabling will do nothing");
    }

    public override void Load(bool hotReload)
    {
        _toggle = new CvarToggle(Logger);

        foreach (var name in Config.Commands.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var command = name.StartsWith("css_", StringComparison.OrdinalIgnoreCase) ? name : $"css_{name}";
            AddCommand(command, "Toggle headshot-only mode on/off", OnToggleCommand);
        }

        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        Logger.LogInformation("Loaded: commands={Commands} permission={Permission} cvars={Count} resetOnMapChange={Reset}",
            string.Join(",", Config.Commands), Config.Permission, Config.Cvars.Count, Config.ResetOnMapChange);
    }

    // Leaving headshot-only switched on after the plugin is gone would make it impossible to turn off
    // from chat, so an unload (including the first half of a hot reload) always restores.
    public override void Unload(bool hotReload)
    {
        if (_toggle?.IsEnabled == true)
        {
            _toggle.Disable();
            Logger.LogInformation("Restored cvars on unload");
        }
    }

    private void OnMapStart(string mapName)
    {
        if (!Config.ResetOnMapChange || _toggle?.IsEnabled != true) return;
        _toggle.Disable();
        Logger.LogInformation("Map changed to {Map}: headshot-only cvars restored", mapName);
    }

    private void OnToggleCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!HasPermission(player))
        {
            info.ReplyToCommand(Localize(player, "prefix") + Localize(player, "no_permission"));
            return;
        }

        if (_toggle!.IsEnabled)
        {
            _toggle.Disable();
            Announce("disabled", player);
            return;
        }

        if (_toggle.Enable(Config.Cvars) == 0)
        {
            info.ReplyToCommand(Localize(player, "prefix") + Localize(player, "no_cvars"));
            return;
        }

        Announce("enabled", player);
    }

    private bool HasPermission(CCSPlayerController? player)
    {
        if (player is null) return true;                 // server console / RCON
        if (_requiredFlags.Length == 0) return true;      // configured as open to everyone
        return AdminManager.PlayerHasPermissions(player, _requiredFlags);
    }

    // ---- messages -----------------------------------------------------------------------------

    /// <summary>
    /// Prints the announcement to every player, in each configured language. With "auto" a
    /// player gets their own language (css_lang, or the server default), so the same line can
    /// come out in one language for one player and another for the next.
    /// </summary>
    private void Announce(string key, CCSPlayerController? actor)
    {
        var console = actor is null;
        var actorName = actor?.PlayerName ?? string.Empty;

        foreach (var target in Utilities.GetPlayers())
        {
            if (!target.IsValid || target.IsBot || target.IsHLTV) continue;

            foreach (var culture in CulturesFor(target))
            {
                var name = console ? Localize(culture, "console_name") : actorName;
                target.PrintToChat(Localize(culture, "prefix") + Localize(culture, key, name));
            }
        }

        if (console)
            Logger.LogInformation("{Key} by console", key);
        else
            Logger.LogInformation("{Key} by {Name} ({SteamId})", key, actorName, actor!.SteamID);
    }

    private IEnumerable<CultureInfo> CulturesFor(CCSPlayerController player)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Config.MessageLanguages)
        {
            CultureInfo culture;
            if (string.IsNullOrWhiteSpace(entry) || entry.Equals("auto", StringComparison.OrdinalIgnoreCase))
                culture = player.GetLanguage();
            else
            {
                try { culture = CultureInfo.GetCultureInfo(entry); }
                catch (CultureNotFoundException)
                {
                    Logger.LogWarning("MessageLanguages contains an unknown culture: {Culture}", entry);
                    continue;
                }
            }

            if (seen.Add(culture.Name)) yield return culture;
        }

        if (seen.Count == 0) yield return player.GetLanguage();
    }

    private string Localize(CCSPlayerController? player, string key, params object[] args)
        => player is null
            ? Localize(PlayerLanguageManager.Instance.GetDefaultLanguage(), key, args)
            : Localizer.ForPlayer(player, key, args);

    // CounterStrikeSharp's JSON localizer resolves against the current UI culture; this is the
    // same trick its own Localizer.ForPlayer uses, just with a culture we choose. Colour tags
    // such as {green} are expanded by the localizer itself.
    private string Localize(CultureInfo culture, string key, params object[] args)
    {
        var prevCulture = CultureInfo.CurrentCulture;
        var prevUi = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try { return Localizer[key, args]; }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUi;
        }
    }
}
