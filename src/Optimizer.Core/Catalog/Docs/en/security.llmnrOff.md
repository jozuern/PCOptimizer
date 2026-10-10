# LLMNR off

## Summary
Turns off Link-Local Multicast Name Resolution, which asks every device on the local network for a name DNS could not find. Can break name lookups on small networks without DNS.

## How it works
LLMNR sends name queries by multicast to other devices on the same subnet and works without a DNS server [1]. With the policy "Turn off multicast name resolution" (EnableMulticast = 0), LLMNR is disabled on all network adapters [1].

## Why it can help
An attacker on the same network cannot answer these broadcast queries to capture sign-in attempts, for example in public Wi-Fi.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
Devices that are found only by LLMNR, for example some printers or NAS boxes on a home network without DNS names, may need their IP address.

## When not to use it
If you reach devices on your home network by name and that stops working.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-dnsclient
