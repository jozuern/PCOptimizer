using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Apps;
using Optimizer.Core.Backup;
using Optimizer.Core.Catalog;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

/// <summary>Turns a switch into apply or undo: undo when this app made the opposite change, else apply the new state.</summary>
internal static class Switching
{
    public static async Task<bool> SetAsync(AppServices services, ChangeRunner runner, Func<bool, TweakDefinition?> build, bool on)
    {
        if (build(!on) is { } opposite && services.Store.Exists(opposite.Id)) return await runner.UndoAsync(opposite);
        return build(on) is { } t && await runner.ApplyAsync(t);
    }
}
