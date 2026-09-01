using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

namespace OnlyHS;

/// <summary>
/// Applies a set of cvar values and can put back exactly what was there before.
///
/// The "before" values are read from the live cvars at the moment of enabling, not from a
/// hardcoded table, so disabling returns the server to whatever its own configs had set.
/// Values are written through the console command path (<c>name "value"</c>), which is the
/// same thing an admin would type and works for every cvar type without special casing.
/// </summary>
internal sealed class CvarToggle
{
    private readonly ILogger _log;
    private readonly Dictionary<string, string> _saved = new(StringComparer.OrdinalIgnoreCase);

    public CvarToggle(ILogger log) => _log = log;

    public bool IsEnabled { get; private set; }

    /// <summary>Snapshots and overrides the given cvars. Returns how many were applied.</summary>
    public int Enable(IReadOnlyDictionary<string, string> desired)
    {
        _saved.Clear();
        var applied = 0;

        foreach (var (name, value) in desired)
        {
            if (!IsSafeName(name))
            {
                _log.LogWarning("Skipping cvar with an invalid name: {Name}", name);
                continue;
            }

            var cvar = ConVar.Find(name);
            if (cvar is null)
            {
                _log.LogWarning("Cvar {Name} does not exist on this server, skipping", name);
                continue;
            }

            var current = ReadValue(cvar);
            if (current is null)
                _log.LogWarning("Cannot read cvar {Name} (type {Type}); it will be set but not restored", name, cvar.Type);
            else
                _saved[name] = current;

            Server.ExecuteCommand($"{name} \"{Escape(value)}\"");
            applied++;
        }

        IsEnabled = applied > 0;
        return applied;
    }

    /// <summary>Writes back the values remembered by <see cref="Enable"/>.</summary>
    public void Disable()
    {
        foreach (var (name, value) in _saved)
            Server.ExecuteCommand($"{name} \"{Escape(value)}\"");

        _saved.Clear();
        IsEnabled = false;
    }

    /// <summary>
    /// Reads a cvar as the string the console would accept back. Returns null for types that
    /// cannot be round-tripped this way (vectors, colours).
    /// </summary>
    private static string? ReadValue(ConVar cvar)
    {
        var ic = CultureInfo.InvariantCulture;
        return cvar.Type switch
        {
            ConVarType.Bool => cvar.GetPrimitiveValue<bool>() ? "1" : "0",
            ConVarType.Int16 => cvar.GetPrimitiveValue<short>().ToString(ic),
            ConVarType.UInt16 => cvar.GetPrimitiveValue<ushort>().ToString(ic),
            ConVarType.Int32 => cvar.GetPrimitiveValue<int>().ToString(ic),
            ConVarType.UInt32 => cvar.GetPrimitiveValue<uint>().ToString(ic),
            ConVarType.Int64 => cvar.GetPrimitiveValue<long>().ToString(ic),
            ConVarType.UInt64 => cvar.GetPrimitiveValue<ulong>().ToString(ic),
            ConVarType.Float32 => cvar.GetPrimitiveValue<float>().ToString("R", ic),
            ConVarType.Float64 => cvar.GetPrimitiveValue<double>().ToString("R", ic),
            ConVarType.String => cvar.StringValue,
            _ => null,
        };
    }

    // A cvar name never contains whitespace or quoting characters; anything else in the config
    // would let a typo turn into a second console command.
    private static bool IsSafeName(string name)
        => !string.IsNullOrWhiteSpace(name)
           && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');

    private static string Escape(string value) => value.Replace("\"", "").Replace(";", "");
}
