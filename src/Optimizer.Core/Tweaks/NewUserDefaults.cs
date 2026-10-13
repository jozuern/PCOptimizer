using Optimizer.Core.Actions;

namespace Optimizer.Core.Tweaks;

/// <summary>
/// "Also for new accounts": the registry values of the per-user tweaks that are on, written to the Default profile, the
/// template Windows copies when it creates an account. One change on the Changes page, undone there like any other
/// (the Default profile gets its earlier values back). Accounts that exist already keep their own settings.
/// Only tweaks made entirely of registry values in the user's hive go along: a tweak that also calls Windows (a system
/// parameter, a restart of Explorer) or writes the machine would arrive half done.
/// </summary>
public static class NewUserDefaults
{
    public const string Id = "newusers.copy";

    /// <summary>
    /// A visible tweak whose every action (for this PC) is a value in the signed-in user's hive. Software\Classes is left
    /// out: an account's classes come from its own UsrClass.dat, so the key of that name in NTUSER.DAT is never read.
    /// </summary>
    public static bool Qualifies(TweakDefinition t, IReadOnlyList<TweakAction> actions) =>
        !t.Hidden && actions.Count > 0 && actions.All(a => a is RegistryAction { Hive: Hive.User } r
            && !r.Path.StartsWith(@"Software\Classes", StringComparison.OrdinalIgnoreCase));

    /// <summary>The change for the applied tweaks given with their actions; null when none of them qualifies.</summary>
    public static TweakDefinition? Build(IEnumerable<(TweakDefinition Tweak, IReadOnlyList<TweakAction> Actions)> applied)
    {
        var actions = applied.Where(x => Qualifies(x.Tweak, x.Actions))
            .SelectMany(x => x.Actions.Cast<RegistryAction>())
            .Select(a => a.ForHive(Hive.DefaultUser))
            .DistinctBy(a => a.TargetKey)
            .ToList();
        if (actions.Count == 0) return null;
        return new TweakDefinition
        {
            Id = Id,
            Docs = Id,
            Category = "System",
            Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Risk = Risk.Safe,
            Hidden = true,
            Actions = [.. actions],
            Sources = ["https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/customize-the-default-user-profile-by-using-copyprofile"],
        };
    }
}
