# Network file transfers: no SMB throttling

## Summary
Lets Windows copy files from network shares at full speed over connections with high latency. No effect on games or internet downloads.

## How it works
The SMB client normally throttles throughput on high-latency links to avoid network timeouts; DisableBandwidthThrottling = 1 turns that off and allows higher throughput [1]. Microsoft lists it in its SMB client tuning guide and notes that such settings are not right for every computer [1].

## Why it can help
Copying to or from a NAS or another PC over VPN or a slow link can get faster.

## Evidence
Documented by Microsoft [1]. It affects only SMB file transfers; local networks with low latency rarely hit the throttle.

## Trade-offs & risks
On unreliable links, transfers can run into timeouts the throttle was meant to avoid.

## When not to use it
If you do not copy files over the network, or transfers start failing afterwards.

## Sources
1. https://learn.microsoft.com/en-US/windows-server/administration/performance-tuning/role/file-server
