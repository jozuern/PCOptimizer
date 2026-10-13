using System.Diagnostics;
using System.Text;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// How this app starts another program, in one place: a plain tool name resolves to System32
/// (<see cref="ProcessHardening.ResolveSystemTool"/>), there is no shell, and the environment has none of the variables
/// a user could set to load code into a tool that runs elevated (<see cref="ProcessHardening.Apply"/>). Programs that
/// run as the signed-in user start through <see cref="DeElevatedLauncher"/>; this app restarting itself keeps its own
/// environment rules (Program.cs, the update).
/// </summary>
public static class ProcessLauncher
{
    /// <summary>
    /// Start info for <paramref name="file"/>, hardened and without a shell. <paramref name="capture"/> redirects both
    /// outputs in that encoding (console tools write in the OEM code page: <see cref="StreamingProcess.Oem"/>).
    /// </summary>
    public static ProcessStartInfo Info(string file, string? arguments = null, bool hidden = true, Encoding? capture = null)
    {
        var psi = new ProcessStartInfo(ProcessHardening.ResolveSystemTool(file), arguments ?? "") { UseShellExecute = false, CreateNoWindow = hidden };
        if (capture is not null)
        {
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = capture;
            psi.StandardErrorEncoding = capture;
        }
        ProcessHardening.Apply(psi);
        return psi;
    }

    /// <summary>Start info with each argument quoted by .NET (paths and queries with spaces or quotes in them).</summary>
    public static ProcessStartInfo Info(string file, IEnumerable<string> arguments, bool hidden = true, Encoding? capture = null)
    {
        var psi = Info(file, (string?)null, hidden, capture);
        foreach (var a in arguments) psi.ArgumentList.Add(a);
        return psi;
    }

    public static Process Start(ProcessStartInfo psi) =>
        Process.Start(psi) ?? throw new InvalidOperationException($"{Path.GetFileName(psi.FileName)} did not start");

    /// <summary>Starts a program and lets it run on its own (a window of its own, a script that outlives this app).</summary>
    public static void StartDetached(ProcessStartInfo psi) => Start(psi).Dispose();

    /// <summary>
    /// Runs a console tool to its end and returns its exit code and output (stdout, then stderr), logged as a command.
    /// After <paramref name="timeout"/> the tool and what it started are ended and a <see cref="TimeoutException"/> follows.
    /// </summary>
    public static (int ExitCode, string Output) Run(ProcessStartInfo psi, TimeSpan timeout)
    {
        var what = $"{Path.GetFileName(psi.FileName)} {(psi.ArgumentList.Count > 0 ? string.Join(' ', psi.ArgumentList) : psi.Arguments)}".TrimEnd();
        var watch = Stopwatch.StartNew();
        using var p = Start(psi);
        var stdout = psi.RedirectStandardOutput ? p.StandardOutput.ReadToEndAsync() : Task.FromResult("");
        var stderr = psi.RedirectStandardError ? p.StandardError.ReadToEndAsync() : Task.FromResult("");
        if (!p.WaitForExit(timeout))
        {
            ProcessHardening.KillTree(p);
            throw new TimeoutException($"{what} timed out");
        }
        // A program the tool started can keep the output pipes open after the tool ended: do not wait for it forever.
        if (!Task.WaitAll([stdout, stderr], TimeSpan.FromSeconds(10)))
            Log.Warn("command", $"{what}: output still open after the tool ended; using what was read");
        var output = (stdout.IsCompletedSuccessfully ? stdout.Result : "") + (stderr.IsCompletedSuccessfully ? stderr.Result : "");
        Log.Info("command", what, new { exitCode = p.ExitCode, ms = watch.ElapsedMilliseconds, output = output.Length > 2000 ? output[..2000] : output });
        return (p.ExitCode, output);
    }
}
