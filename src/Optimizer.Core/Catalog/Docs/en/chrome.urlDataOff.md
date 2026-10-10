# Google Chrome: no URL-keyed data collection

## Summary
Chrome no longer sends the addresses of pages you visit to Google to make searches and browsing better.

## How it works
With the policy UrlKeyedAnonymizedDataCollectionEnabled set to off, Chrome does no URL-keyed anonymized data collection, which otherwise sends URLs of visited pages to Google [1]. Chrome reads the policy from HKLM\SOFTWARE\Policies\Google\Chrome, also on PCs without a domain [2].

## Why it can help
The pages you open are not reported to Google for this purpose. It changes neither frame rate nor latency.

## Evidence
A documented Chrome policy [1].

## Trade-offs & risks
Chrome's "Make searches and browsing better" setting is off and locked. Because these are policies, Chrome shows that it is managed by your organization, and the matching settings are locked. Undo removes them.

## When not to use it
If you want to share this data with Google.

## Sources
1. https://chromeenterprise.google/policies/#UrlKeyedAnonymizedDataCollectionEnabled
2. https://support.google.com/chrome/a/answer/9131254?hl=en
