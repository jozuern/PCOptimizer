# DNS over HTTPS for Cloudflare, Google and Quad9

## Summary
When an adapter uses Cloudflare, Google or Quad9 DNS, Windows sends the lookups encrypted over HTTPS. Takes effect only with one of these DNS servers.

## How it works
Windows knows the DNS over HTTPS (DoH) addresses of these providers. With AutoUpgrade on, Windows uses DoH automatically for a known server that an adapter uses [1][2]. The app turns AutoUpgrade on for their IPv4 addresses with Set-DnsClientDohServerAddress and leaves the fallback setting as it is [2]. Combine it with one of the DNS presets above.

## Why it can help
Your DNS lookups are encrypted, so the network you are on (public Wi-Fi, your provider) cannot read or change which sites you look up.

## Evidence
Documented by Microsoft [1][2]. DoH adds the HTTPS connection to each server; DNS only finds servers, so ping and frame rate in games do not change.

## Trade-offs & risks
If a network blocks DoH and the server does not allow a fallback to plain DNS, names cannot be resolved until you undo the tweak or pick other DNS servers. Without one of these providers set as DNS server, nothing changes.

## When not to use it
In company or school networks with their own DNS, or on networks that block encrypted DNS.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/networking/dns/doh-client-support
2. https://learn.microsoft.com/en-us/powershell/module/dnsclient/set-dnsclientdohserveraddress
