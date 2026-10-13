using System.Text;

namespace Optimizer.Core.Actions;

/// <summary>
/// A scheduled task of this app, registered from XML the app builds (the opt-in re-apply task); undo deletes it, or
/// registers again the task that was there before. Only paths in Task Scheduler's \Tunero\ folder: the action never
/// replaces or removes a task of Windows or of another program.
/// </summary>
public sealed class OwnTaskAction : TweakAction
{
    public const string Folder = @"\Tunero\";

    public string Path { get; init; } = "";

    /// <summary>The task's registration XML.</summary>
    public string Xml { get; init; } = "";

    public override string TargetKey => $"owntask:{Path}".ToLowerInvariant();

    public override TargetText TargetLabel(ActionContext c) => TargetText.Of("target.ownTask", Path);

    public override StoredValue Desired(ActionContext c) => Present(Xml);

    /// <summary>Present or missing; a present task's XML travels in the kind, so undo can register it again.</summary>
    public override StoredValue? Read(ActionContext c) => c.Tasks.ExportXml(Path) is { } xml ? Present(xml) : StoredValue.Missing;

    private static StoredValue Present(string xml) => new(true, "owntask:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)), "present");

    public override void Apply(ActionContext c)
    {
        Check();
        c.Tasks.Register(Path, Xml);
    }

    public override void Restore(ActionContext c, StoredValue original)
    {
        Check();
        if (original.Existed && original.Kind is { } kind && kind.StartsWith("owntask:", StringComparison.Ordinal))
        {
            c.Tasks.Register(Path, Encoding.UTF8.GetString(Convert.FromBase64String(kind["owntask:".Length..])));
            return;
        }
        if (c.Tasks.ExportXml(Path) is not null) c.Tasks.Delete(Path);
    }

    private void Check()
    {
        if (!Path.StartsWith(Folder, StringComparison.OrdinalIgnoreCase) || Path.Length <= Folder.Length || Path[Folder.Length..].Contains('\\'))
            throw new InvalidOperationException($"{Path}: the app only registers its own tasks, directly in {Folder}");
    }
}
