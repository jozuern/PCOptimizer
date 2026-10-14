namespace Optimizer.Core.Findings;

/// <summary>
/// Results the user ignores: a game library on a hard disk kept there on purpose (a backup, copying being faster than
/// downloading), a check that does not apply to how this PC is used. One result is ignored, not the whole check, and
/// only as it read when it was ignored: when its status changes (an unknown disk reports a problem, a problem turns
/// critical) it shows again. An ignored result counts nowhere: not in the lists or the score, and not in the facts, so
/// nothing recommended for it is applied, in the window or by --apply-recommended.
/// </summary>
public static class IgnoredResults
{
    /// <summary>
    /// The check, the one result (its instance and its subject: an instance key such as a disk number can move to
    /// another device after a hardware change, the device name does not) and its status.
    /// </summary>
    public static string Key(Finding f) => $"{f.Key}|{f.Subject}@{f.Status}{(f.Critical ? "!" : "")}";

    /// <summary>A problem, a note or an unknown on the Overview or Manual checks; not a passed check or game access.</summary>
    public static bool CanIgnore(Finding f) =>
        f.Kind is FindingKind.Finding or FindingKind.Advisor && f.Status is not (FindingStatus.Ok or FindingStatus.Unsupported);

    public static bool IsIgnored(Finding f, IReadOnlyCollection<string> keys) => keys.Count > 0 && CanIgnore(f) && keys.Contains(Key(f));

    /// <summary>The results without the ignored ones.</summary>
    public static IReadOnlyList<Finding> Without(IReadOnlyList<Finding> findings, IReadOnlyCollection<string> keys) =>
        keys.Count == 0 ? findings : findings.Where(f => !IsIgnored(f, keys)).ToList();
}
