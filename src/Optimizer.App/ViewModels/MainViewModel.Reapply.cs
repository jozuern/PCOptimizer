using Optimizer.App.Services;
using Optimizer.Core.Backup;
using Optimizer.Core.Platform;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

public sealed partial class MainViewModel
{
    private bool? _reapplyOn;

    /// <summary>
    /// The re-apply task (<see cref="ReapplyTask"/>) can be set up: the published exe, run as administrator by the
    /// signed-in account itself, in a folder only administrators can change (the task starts it elevated without asking).
    /// </summary>
    public bool ReapplyAvailable => Install.IsPublishedExe && Install.Elevated && !_services.Elevation.UserMismatch &&
        _services.Elevation.SessionUserSid is not null && ReapplyTask.CanRegister(Install.ExePath, TrustedPath.IsAdminOnlyBelowSharedFolders);

    /// <summary>What the switch does, or why it is off here.</summary>
    public string ReapplyHint => Loc.Instance[
        ReapplyAvailable ? "Reapply_Hint"
        : !Install.IsPublishedExe || !Install.Elevated ? "Reapply_NotHere"
        : _services.Elevation.UserMismatch ? "Reapply_OtherAccount"
        : "Reapply_UserFolder"];

    /// <summary>The task is set up (its backup is on the Changes page). Switching runs the change through the runner, with its confirmation.</summary>
    public bool ReapplyOn
    {
        get => _reapplyOn ??= HasBackup(ReapplyTask.TweakId);
        set
        {
            if (value == ReapplyOn) return;
            SetReapplyAsync(value).Forget("re-apply task");
        }
    }

    private bool HasBackup(string id)
    {
        try
        {
            return _services.Store.Get(id) is not null;
        }
        catch (BackupUnreadableException)
        {
            return true;
        }
    }

    private async Task SetReapplyAsync(bool on)
    {
        if (on && Install.ExePath is { } exe && _services.Elevation.SessionUserSid is { } sid) await Runner.ApplyAsync(ReapplyTask.Definition(exe, sid));
        else if (!on && _services.Engine.Resolve(ReapplyTask.TweakId) is { } t) await Runner.UndoAsync(t);
        RefreshReapply();
    }

    /// <summary>After any change (an undo on the Changes page, Undo all): the switch shows the task as it is.</summary>
    private void RefreshReapply()
    {
        _reapplyOn = null;
        OnPropertyChanged(nameof(ReapplyOn));
    }
}
