using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// Runs a long tool (winget, sfc, dism) without a window and reports every output line while it runs.
/// Cancellation kills the process tree. Output is logged once at the end (truncated).
/// </summary>
public static class StreamingProcess
{
    /// <param name="started">Called right after the process has started (for example to release a file held open until then).</param>
    public static async Task<int> RunAsync(string file, string arguments, IProgress<string>? lines, CancellationToken ct = default, Encoding? encoding = null,
        Action? started = null)
    {
        var psi = new ProcessStartInfo(ProcessHardening.ResolveSystemTool(file), arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = encoding ?? Encoding.UTF8,
            StandardErrorEncoding = encoding ?? Encoding.UTF8,
        };
        ProcessHardening.Apply(psi);
        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var all = new List<string>();
        void OnLine(string? line)
        {
            if (line is null) return;
            // sfc and dism print progress with carriage returns; keep the last segment of each line.
            var text = line.Split('\r').LastOrDefault(s => s.Trim().Length > 0)?.Trim() ?? "";
            if (text.Length == 0) return;
            lock (all) OutputLines.Add(all, text, int.MaxValue);
            lines?.Report(text);
        }
        p.OutputDataReceived += (_, e) => OnLine(e.Data);
        p.ErrorDataReceived += (_, e) => OnLine(e.Data);
        if (!p.Start()) throw new InvalidOperationException($"Cannot start {file}");
        started?.Invoke();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            ProcessHardening.KillTree(p);
            throw;
        }
        string output;
        lock (all) output = string.Join(Environment.NewLine, all);
        Log.Info("command", $"{Path.GetFileName(file)} {arguments}", new { exitCode = p.ExitCode, output = output.Length > 4000 ? output[^4000..] : output });
        return p.ExitCode;
    }

    /// <summary>sfc.exe writes UTF-16 to a redirected pipe.</summary>
    public static readonly Encoding Utf16 = Encoding.Unicode;

    /// <summary>
    /// The OEM code page of the system (850 on German Windows), which console tools such as dism, ipconfig, chkdsk,
    /// netsh, net, reagentc and Windows PowerShell write to a redirected pipe. Read as UTF-8, umlauts come out as
    /// replacement characters. UTF-8 when the code page is not available.
    /// </summary>
    public static readonly Encoding Oem = OemEncoding();

    private static Encoding OemEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding((int)GetOEMCP());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.UTF8;
        }
    }

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetOEMCP();
}
