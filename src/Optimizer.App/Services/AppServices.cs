using System.IO;
using System.Net.NetworkInformation;
using Optimizer.Core.Actions;
using Optimizer.Core.Backup;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.Services;

/// <summary>Wires the engine to the real system: session user's hive, secured backup store, restore points.</summary>
public sealed class AppServices
{
    public AppServices()
    {
        Elevation = ElevationInfo.Read();
        Os = BuildInfo.Read();
        var root = BackupStore.DefaultRoot;
        // Not elevated (Debug build): read-only use, the ProgramData ACL cannot be set and nothing can be applied anyway.
        Store = new BackupStore(root, secure: Elevation.IsElevated);
        Context = SystemNotify.CreateContext(Elevation.SessionUserSid ?? Elevation.ProcessUserSid, Store.ExportFolder, ActiveInterfaceIds());
        RestorePoints = new RestorePointService(Context.Processes);
        Engine = new TweakEngine(Context, Store, RestorePoints, typeof(AppServices).Assembly.GetName().Version?.ToString(3) ?? "0", Os.Build, Os.BuildString);

        // bcdedit needs admin rights; without them the BCD part of F21 is reported as unknown.
        LeftoverCheck.BcdElements = Elevation.IsElevated ? () => Context.Bcd.CurrentElements() : () => null;
        Log.Info("app", "services ready", new { Elevation.IsElevated, Elevation.AdministratorProtection, user = Elevation.SessionUser });
    }

    public ElevationInfo Elevation { get; }
    public BuildInfo Os { get; }
    public BackupStore Store { get; }
    public ActionContext Context { get; }
    public RestorePointService RestorePoints { get; }
    public TweakEngine Engine { get; }
    public TweakCatalog Catalog => TweakCatalog.Current;

    private static IReadOnlyList<string> ActiveInterfaceIds()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet)
                .Select(n => n.Id)
                .ToList();
        }
        catch (NetworkInformationException)
        {
            return [];
        }
    }

    public static string DataFolder => Optimizer.Core.Platform.DataPaths.Root;

    /// <summary>Extracted helper tools (PresentMon) and capture files.</summary>
    public static string ToolsFolder => Path.Combine(DataFolder, "tools");

    /// <summary>SID of the signed-in user (not the elevated account), for per-user paths and registry.</summary>
    public string? UserSid => Elevation.SessionUserSid ?? Elevation.ProcessUserSid;

    /// <summary>Profile folder of the signed-in user, from the profile list.</summary>
    public string? ProfilePath => UserSid is { } sid
        ? Reg.HklmString($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{sid}", "ProfileImagePath")
        : null;
}

/// <summary>Dialogs the view model needs; implemented by the views.</summary>
public interface IDialogs
{
    /// <summary>Shows the exact changes; returns null when cancelled, otherwise whether the anti-cheat warning was acknowledged.</summary>
    ConfirmResult? ConfirmApply(ConfirmRequest request);

    /// <summary>System Protection is off: Enable / Continue / Cancel.</summary>
    RestorePointChoice AskRestorePoint();

    bool ConfirmExpertMode();

    bool ConfirmUndoAll(int count);

    /// <summary>Generic question; returns true when the primary button was chosen.</summary>
    bool Ask(string title, string text, string primary);
}

public sealed record ConfirmRequest(
    string Title,
    string Summary,
    IReadOnlyList<ChangeLine> Changes,
    IReadOnlyList<string> Badges,
    IReadOnlyList<string> Warnings,
    string? AntiCheatWarning,
    bool IsUndo);

public sealed record ConfirmResult(bool AcknowledgedAntiCheat);

public enum RestorePointChoice { Enable, Continue, Cancel }
