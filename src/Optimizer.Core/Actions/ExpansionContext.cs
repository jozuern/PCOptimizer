using Optimizer.Core.Logging;

namespace Optimizer.Core.Actions;

/// <summary>
/// What action templates expand against (<see cref="TweakAction.Expand"/>), each read at most once per tweak: the
/// active network interfaces ("{nic}"), the active power plan, the physical network adapters and the power source. A
/// read that fails gives nothing to expand to, so the template is left out rather than guessed.
/// </summary>
public sealed class ExpansionContext(ActionContext c)
{
    private IReadOnlyList<string>? _interfaces;
    private IReadOnlyList<NicAdapter>? _adapters;
    private (bool Read, Guid? Value) _scheme;
    private (bool Read, string? Value) _source;

    public ActionContext Context => c;

    /// <summary>Interface GUIDs ("{...}") of the active Ethernet and Wi-Fi adapters.</summary>
    public IReadOnlyList<string> NetworkInterfaces => _interfaces ??= c.CurrentNetworkInterfaceIds();

    /// <summary>The active power plan; null when it cannot be read.</summary>
    public Guid? ActiveScheme
    {
        get
        {
            if (_scheme.Read) return _scheme.Value;
            Guid? value;
            try
            {
                value = c.Power.ActiveScheme() is var g && g != Guid.Empty ? g : null;
            }
            catch (Exception ex)
            {
                Log.Warn("engine", $"active power plan unreadable: {ex.Message}");
                value = null;
            }
            _scheme = (true, value);
            return value;
        }
    }

    /// <summary>The physical network adapters; none when they cannot be read.</summary>
    public IReadOnlyList<NicAdapter> Adapters
    {
        get
        {
            if (_adapters is not null) return _adapters;
            try
            {
                _adapters = NicAdapters.Enumerate(c.Registry);
            }
            catch (Exception ex)
            {
                Log.Warn("engine", $"network adapters unreadable: {ex.Message}");
                _adapters = [];
            }
            return _adapters;
        }
    }

    /// <summary>"ac" or "dc" for the power mode of the current source; null when Windows does not say.</summary>
    public string? PowerSource => _source.Read ? _source.Value : (_source = (true, PowerModeAction.CurrentSource(c))).Value;
}
