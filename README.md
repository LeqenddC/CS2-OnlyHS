# CS2-OnlyHS

A CounterStrikeSharp plugin for Counter-Strike 2 that switches the server to headshot-only damage
with a single chat command. `!onlyhs` turns it on, typing it again turns it off, and a map change
resets it automatically.

Written for an aim/AWP community server where admins wanted to run a few headshot-only rounds
without editing configs or leaving the setting behind for the next map.

## Features

- `!onlyhs`, `/onlyhs` or `css_onlyhs` from the console/RCON toggles headshot-only mode.
- Uses CS2's built-in `mp_damage_headshot_only` cvar by default; the older
  `mp_damage_scale_*_body 0` approach works too.
- Original cvar values are read at the moment of enabling and restored on disable, on map change
  and on plugin unload/reload. Your `server.cfg` and game mode settings stay the source of truth.
- Permission-gated (`@css/generic` by default). Any CounterStrikeSharp flag can be used, or the
  command can be opened to everyone.
- Chat announcements are localized per player. English is included; adding a language is one JSON
  file.

## Requirements

- Counter-Strike 2 dedicated server with [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.373 or newer

## Installation

1. Download `CS2-OnlyHS.zip` from the [latest release](../../releases/latest).
2. Extract it so that you end up with `game/csgo/addons/counterstrikesharp/plugins/CS2-OnlyHS/`
   containing `CS2-OnlyHS.dll`, `CS2-OnlyHS.deps.json` and the `lang/` folder.
3. Restart the server, or load the plugin without a restart:

   ```
   css_plugins load plugins/CS2-OnlyHS/CS2-OnlyHS.dll
   ```

The config file is generated on first load.

## Usage

| Command | Where | Effect |
|---|---|---|
| `!onlyhs` / `/onlyhs` | chat | Toggle headshot-only damage on or off |
| `css_onlyhs` | server console, RCON | Same, always allowed |

Everyone on the server sees a message like `[OnlyHS] Player enabled headshot-only mode!` in their
language. Players without the required permission get a private message instead and nothing changes.

## Configuration

`game/csgo/addons/counterstrikesharp/configs/plugins/CS2-OnlyHS/CS2-OnlyHS.json`

```json
{
  "Commands": ["onlyhs"],
  "Permission": "@css/generic",
  "ResetOnMapChange": true,
  "MessageLanguages": ["auto"],
  "Cvars": {
    "mp_damage_headshot_only": "1"
  },
  "ConfigVersion": 1
}
```

| Key | Description |
|---|---|
| `Commands` | Command names without the `css_` prefix. Each entry becomes `css_<name>`, which CounterStrikeSharp also exposes as `!<name>` and `/<name>` in chat. |
| `Permission` | CounterStrikeSharp permission flag required to use the command. Several flags can be separated with commas; all of them are then required. An empty string allows everyone. The server console is always allowed. |
| `ResetOnMapChange` | When `true`, headshot-only is switched off (cvars restored) as soon as a new map starts. |
| `MessageLanguages` | `"auto"` uses each player's CounterStrikeSharp language (`css_lang`, falling back to `ServerLanguage` in `core.json`). A culture code such as `"en"` forces that language for everyone. Several entries, e.g. `["en", "de"]`, print one line per language. |
| `Cvars` | The cvars to apply when the mode is enabled, as `"name": "value"`. Values are strings. Cvars that do not exist on the server are skipped with a warning in the log. |

Apply changes with `css_plugins reload CS2-OnlyHS`. If the mode is on at that moment it is switched
off first, so the restore always uses the values that were actually saved.

CounterStrikeSharp does not add new keys to an existing config file. After upgrading to a version that
introduces a new option, add the key by hand or delete the file to have it regenerated.

### About the default cvar

`mp_damage_headshot_only` is described by the game as "Determines whether non-headshot hits do any
damage". It is `game replicated` and not cheat-protected, and it is independent of the damage scale
cvars, so map configs that touch `mp_damage_scale_*` do not interfere with it.

If you prefer the pre-existing method used by many map configs, replace the `Cvars` block with:

```json
"Cvars": {
  "mp_damage_scale_ct_body": "0",
  "mp_damage_scale_t_body": "0"
}
```

## Translations

Messages live in `plugins/CS2-OnlyHS/lang/<culture>.json`. `en.json` is included.

```json
{
    "prefix": " {green}[OnlyHS]{default} ",
    "enabled": "{lightred}{0}{default} enabled headshot-only mode!",
    "disabled": "{lightred}{0}{default} disabled headshot-only mode!",
    "no_permission": "You do not have permission to use this command.",
    "no_cvars": "No cvar could be applied. Check the Cvars section of the config.",
    "console_name": "Console"
}
```

`{0}` is the name of the player who ran the command. CounterStrikeSharp colour tags such as
`{green}`, `{lightred}` and `{default}` work in every string. To add a language, copy `en.json` to
`<culture>.json` (for example `de.json`) and translate the values.

## Building from source

The repository contains no compiled files. With the .NET 10 SDK installed:

```
dotnet publish src/OnlyHS/CS2-OnlyHS.csproj -c Release -o build/out
```

Without a local SDK, `./build.sh` runs the same command inside the `mcr.microsoft.com/dotnet/sdk:10.0`
Docker image. The GitHub Actions workflow builds every push and attaches a zip to tagged releases.

## License

GPL-3.0. See [LICENSE](LICENSE).
