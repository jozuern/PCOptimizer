using Optimizer.Core.Startup;

namespace Optimizer.Core.Actions;

/// <summary>
/// A scheduled task of this app, registered from XML the app builds (the opt-in re-apply task); undo deletes it. Only
/// paths in Task Scheduler's \Tunero\ folder: the action never replaces or removes a task of Windows or of another
/// program. The task runs as one account, so its backup is that account's: another administrator neither sees it as on
/// nor undoes it.
/// </summary>
public sealed class OwnTaskAction : TweakAction
{
    public const string Folder = @"\Tunero\";

    public string Path { get; init; } = "";

    /// <summary>The task's registration XML.</summary>
    public string Xml { get; init; } = "";

    public override string TargetKey => $"owntask:user:{Path}".ToLowerInvariant();

    public override TargetText TargetLabel(ActionContext c) => TargetText.Of("target.ownTask", Path);

    public override StoredValue Desired(ActionContext c) => new(true, "owntask", "present");

    /// <summary>
    /// "present" only when the task there runs what this XML runs: a task left by a copy that was uninstalled or moved
    /// (another exe path) is "other", so turning the setting on registers the task anew.
    /// </summary>
    public override StoredValue? Read(ActionContext c) => c.Tasks.ExportXml(Path) is not { } xml ? StoredValue.Missing
        : new(true, "owntask", ScheduledTaskXml.Command(xml) is { } command && string.Equals(command, ScheduledTaskXml.Command(Xml), StringComparison.OrdinalIgnoreCase)
            ? "present" : "other");

    public override void Apply(ActionContext c)
    {
        Check();
        c.Tasks.Register(Path, Xml);
    }

    /// <summary>
    /// Deletes the task. A task that was there before is not registered again: only this app writes in its folder, so it
    /// came from a copy that may be gone, and it would start that path with administrator rights.
    /// </summary>
    public override void Restore(ActionContext c, StoredValue original)
    {
        Check();
        if (c.Tasks.ExportXml(Path) is not null) c.Tasks.Delete(Path);
    }

    private void Check()
    {
        if (!Path.StartsWith(Folder, StringComparison.OrdinalIgnoreCase) || Path.Length <= Folder.Length || Path[Folder.Length..].Contains('\\'))
            throw new InvalidOperationException($"{Path}: the app only registers its own tasks, directly in {Folder}");
    }
}
