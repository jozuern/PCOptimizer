using System.Diagnostics;
using System.Text;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// Runs a long tool (winget, sfc, dism) without a window and reports every output line while it runs.
/// Cancellation kills the process tree. Output is logged once at the end (truncated).
/// </summary>
public static class StreamingProcess
{
    public static async Task<int> RunAsync(string file, string arguments, IProgress<string>? lines, CancellationToken ct = default, Encoding? encoding = null)
    {
        var psi = new ProcessStartInfo(file, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = encoding ?? Encoding.UTF8,
            StandardErrorEncoding = encoding ?? Encoding.UTF8,
        };
        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var all = new StringBuilder();
        void OnLine(string? line)
        {
            if (line is null) return;
            // sfc and dism print progress with carriage returns; keep the last segment of each line.
            var text = line.Split('\r').LastOrDefault(s => s.Trim().Length > 0)?.Trim() ?? "";
            if (text.Length == 0) return;
            lock (all) all.AppendLine(text);
            lines?.Report(text);
        }
        p.OutputDataReceived += (_, e) => OnLine(e.Data);
        p.ErrorDataReceived += (_, e) => OnLine(e.Data);
        if (!p.Start()) throw new InvalidOperationException($"Cannot start {file}");
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try
            {
                p.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            throw;
        }
        var output = all.ToString();
        Log.Info("command", $"{Path.GetFileName(file)} {arguments}", new { exitCode = p.ExitCode, output = output.Length > 4000 ? output[^4000..] : output });
        return p.ExitCode;
    }

    /// <summary>sfc.exe writes UTF-16 to a redirected pipe.</summary>
    public static readonly Encoding Utf16 = Encoding.Unicode;
}
