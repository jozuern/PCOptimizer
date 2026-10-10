using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Optimizer.Core.Network;

public sealed record DnsServerResult(string Name, IPAddress Server, double? MedianMs, double? BestMs, int Answered, int Sent)
{
    public double SuccessRate => Sent == 0 ? 0 : (double)Answered / Sent;
}

/// <summary>
/// Opt-in DNS benchmark: sends plain UDP DNS queries (RFC 1035, type A) for common domains to each server and
/// measures the response time. Runs only when the user starts it; it sends a few dozen small packets to the listed
/// public resolvers and the current DNS servers, nothing else.
/// </summary>
public static class DnsBenchmark
{
    public static readonly (string Name, IPAddress Address)[] PublicServers =
    [
        ("Cloudflare", IPAddress.Parse("1.1.1.1")),
        ("Google", IPAddress.Parse("8.8.8.8")),
        ("Quad9", IPAddress.Parse("9.9.9.9")),
    ];

    public static readonly string[] Domains =
    [
        "www.google.com", "www.youtube.com", "store.steampowered.com", "www.twitch.tv", "discord.com",
        "www.microsoft.com", "www.reddit.com", "www.amazon.com", "www.wikipedia.org", "www.epicgames.com",
    ];

    /// <summary>IPv4 DNS servers configured on the active adapters (from DHCP or set by hand).</summary>
    public static IReadOnlyList<IPAddress> CurrentServers() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().DnsAddresses)
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
            .Distinct()
            .ToList();

    public static async Task<IReadOnlyList<DnsServerResult>> RunAsync(IEnumerable<(string Name, IPAddress Address)> servers, int rounds = 2,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var results = new List<DnsServerResult>();
        foreach (var (name, address) in servers)
        {
            progress?.Report(name);
            var times = new List<double>();
            var sent = 0;
            // The first round warms the server's cache like normal use does; all rounds count.
            for (var r = 0; r < rounds; r++)
            {
                foreach (var domain in Domains)
                {
                    ct.ThrowIfCancellationRequested();
                    sent++;
                    if (await QueryAsync(address, domain, TimeSpan.FromSeconds(1), ct) is { } ms) times.Add(ms);
                }
            }
            times.Sort();
            results.Add(new DnsServerResult(name, address, times.Count > 0 ? Median(times) : null, times.Count > 0 ? times[0] : null, times.Count, sent));
        }
        return results.OrderBy(r => r.MedianMs ?? double.MaxValue).ToList();
    }

    public static double Median(IReadOnlyList<double> sorted) =>
        sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;

    /// <summary>One query; returns the round-trip time in ms, or null on timeout, error or a mismatched answer.</summary>
    public static async Task<double?> QueryAsync(IPAddress server, string domain, TimeSpan timeout, CancellationToken ct)
    {
        var id = (ushort)Random.Shared.Next(ushort.MaxValue);
        var packet = BuildQuery(id, domain);
        using var udp = new UdpClient(server.AddressFamily);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        var sw = Stopwatch.StartNew();
        try
        {
            await udp.SendAsync(packet, new IPEndPoint(server, 53), cts.Token);
            while (true)
            {
                var reply = await udp.ReceiveAsync(cts.Token);
                if (!reply.RemoteEndPoint.Address.Equals(server) || !IsAnswerTo(reply.Buffer, id)) continue;
                return sw.Elapsed.TotalMilliseconds;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    /// <summary>RFC 1035 query: header (id, RD flag, one question) + QNAME labels + QTYPE A + QCLASS IN.</summary>
    public static byte[] BuildQuery(ushort id, string domain)
    {
        var bytes = new List<byte> { (byte)(id >> 8), (byte)id, 0x01, 0x00, 0, 1, 0, 0, 0, 0, 0, 0 };
        foreach (var label in domain.TrimEnd('.').Split('.'))
        {
            var ascii = System.Text.Encoding.ASCII.GetBytes(label);
            if (ascii.Length is 0 or > 63) throw new ArgumentException($"invalid label in {domain}");
            bytes.Add((byte)ascii.Length);
            bytes.AddRange(ascii);
        }
        bytes.AddRange([0, 0, 1, 0, 1]);
        return [.. bytes];
    }

    /// <summary>A response (QR bit) with our id and RCODE 0 (no error).</summary>
    public static bool IsAnswerTo(byte[] reply, ushort id) =>
        reply.Length >= 12 && reply[0] == (byte)(id >> 8) && reply[1] == (byte)id && (reply[2] & 0x80) != 0 && (reply[3] & 0x0F) == 0;
}
