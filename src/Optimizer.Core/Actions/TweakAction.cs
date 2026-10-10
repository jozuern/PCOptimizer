using System.Text.Json.Serialization;

namespace Optimizer.Core.Actions;

public enum ActionState { Applied, NotApplied, Unsupported }

/// <summary>A stored value: whether it existed, its kind and a serialized form. Used for originals and applied values.</summary>
public sealed record StoredValue(bool Existed, string? Kind = null, string? Data = null)
{
    public static readonly StoredValue Missing = new(false);

    public string Display => !Existed ? "(not set)" : Data ?? "";

    /// <summary>Same presence and data, and the same registry type when both are registry values (a REG_SZ "1" is not a DWORD 1).</summary>
    public bool SameAs(StoredValue other) =>
        Existed == other.Existed && (!Existed || (string.Equals(Data, other.Data, StringComparison.Ordinal) &&
                                                  (!IsRegistryKind(Kind) || !IsRegistryKind(other.Kind) || string.Equals(Kind, other.Kind, StringComparison.Ordinal))));

    // Other actions use the kind for their own purposes (a label, or the raw bytes of a startup entry).
    private static bool IsRegistryKind(string? kind) => kind is "dword" or "qword" or "string" or "expandString" or "multiString" or "binary" or "none";
}

/// <summary>What an undo did with one target.</summary>
public enum RestoreOutcome
{
    /// <summary>The original is back.</summary>
    Restored,

    /// <summary>Nothing restored: the value is no longer the one this app wrote (Windows or the user changed it).</summary>
    ChangedSince,

    /// <summary>The parts still as applied were restored; parts changed since (one side of a power setting) were left.</summary>
    PartlyRestored,
}

/// <summary>One exact change for the confirmation dialog and the generated "What changes" section.</summary>
public sealed record ChangeLine(string Target, string Before, string After)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Target;
}

/// <summary>
/// One step of a tweak. Every action can read its current state, describe the change, apply it and restore a
/// stored original. Actions never decide policy (that is the engine's and the guard rules' job).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RegistryAction), "registry")]
[JsonDerivedType(typeof(RegistryBitsAction), "registryBits")]
[JsonDerivedType(typeof(RegistryTokenAction), "registryToken")]
[JsonDerivedType(typeof(RegistryBinaryBitsAction), "registryBinaryBits")]
[JsonDerivedType(typeof(ServiceAction), "service")]
[JsonDerivedType(typeof(PowerSettingAction), "powerSetting")]
[JsonDerivedType(typeof(PowerSchemeAction), "powerScheme")]
[JsonDerivedType(typeof(BcdAction), "bcd")]
[JsonDerivedType(typeof(ScheduledTaskAction), "scheduledTask")]
[JsonDerivedType(typeof(HibernationAction), "hibernation")]
[JsonDerivedType(typeof(MemoryCompressionAction), "memoryCompression")]
[JsonDerivedType(typeof(DisplayModeAction), "displayMode")]
[JsonDerivedType(typeof(NvidiaDrsAction), "nvidiaDrs")]
[JsonDerivedType(typeof(PowerModeAction), "powerMode")]
[JsonDerivedType(typeof(NicPropertyAction), "nicProperty")]
[JsonDerivedType(typeof(DnsAction), "dns")]
[JsonDerivedType(typeof(Startup.StartupApprovedAction), "startupApproved")]
[JsonDerivedType(typeof(Tools.OptionalFeatureAction), "optionalFeature")]
public abstract class TweakAction
{
    /// <summary>Stable key of the changed target, used for the first-original backup rule.</summary>
    public abstract string TargetKey { get; }

    /// <summary>The value this action writes (for drift detection and feature-update-aware undo).</summary>
    public abstract StoredValue Desired(ActionContext c);

    /// <summary>Current value of the target, or null when the target does not exist on this PC (-> Unsupported).</summary>
    public abstract StoredValue? Read(ActionContext c);

    public abstract void Apply(ActionContext c);

    /// <summary>Writes a stored original back (deleting the target when it did not exist).</summary>
    public abstract void Restore(ActionContext c, StoredValue original);

    /// <summary>Human-readable target, e.g. "HKLM\SYSTEM\...\HwSchMode".</summary>
    public abstract string Describe(ActionContext c);

    /// <summary>Boot configuration and similar changes that the app's own undo cannot fix if the PC does not start.</summary>
    public virtual bool IsBootCritical => false;

    /// <summary>
    /// The change takes effect only after a restart and <see cref="Read"/> returns the running value, so the old value
    /// until then (memory compression). The engine stores the desired value as applied and undoes it also before the restart.
    /// </summary>
    public virtual bool TakesEffectAfterRestart => false;

    public virtual ActionState State(ActionContext c)
    {
        var current = Read(c);
        if (current is null) return ActionState.Unsupported;
        return current.SameAs(Desired(c)) ? ActionState.Applied : ActionState.NotApplied;
    }

    /// <summary>
    /// Undo check: is the value we applied still in place? (If not, Windows or the user changed it and undo leaves it.)
    /// Actions whose target is shared or scheme-specific override this.
    /// </summary>
    public virtual bool IsStillApplied(ActionContext c, StoredValue applied) => Read(c) is not { } current || current.SameAs(applied);

    /// <summary>
    /// Undo of one target: restores the original unless the value is no longer what this app wrote. Null
    /// <paramref name="applied"/> restores unconditionally. Actions made of several parts (mains and battery, several
    /// adapter keywords) override this to compare and restore each part on its own.
    /// </summary>
    public virtual RestoreOutcome RestoreIfUnchanged(ActionContext c, StoredValue original, StoredValue? applied)
    {
        if (applied is not null && !IsStillApplied(c, applied)) return RestoreOutcome.ChangedSince;
        Restore(c, original);
        return RestoreOutcome.Restored;
    }

    public ChangeLine Change(ActionContext c)
    {
        var current = Read(c);
        return new ChangeLine(Describe(c), current?.Display ?? "(unavailable)", Desired(c).Display);
    }
}
