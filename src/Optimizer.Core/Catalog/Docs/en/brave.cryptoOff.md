# Brave: no Rewards, Wallet and VPN

## Summary
Removes Brave Rewards with its ads, the built-in crypto wallet with web3 features and the paid Brave VPN from the browser.

## How it works
Three Brave policies under HKLM\SOFTWARE\Policies\BraveSoftware\Brave [1]: BraveRewardsDisabled hides Rewards, so no Brave ads and no earning of tokens; BraveWalletDisabled removes the wallet, web3 and decentralized DNS features; BraveVPNDisabled removes the VPN button and subscription offers [1].

## Why it can help
A browser without crypto and subscription offers. It changes neither frame rate nor latency.

## Evidence
Documented Brave policies [1].

## Trade-offs & risks
If you use the wallet, Rewards or the VPN, they stop working in Brave. Because these are policies, Brave shows "Managed by your organization" in its menu, and brave://policy lists them. Undo removes them.

## When not to use it
If you use Brave Rewards, the wallet or Brave VPN.

## Sources
1. https://support.brave.app/hc/en-us/articles/360039248271-Group-Policy
