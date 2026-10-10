# Google Chrome: AI features without improving Google's models

## Summary
Keeps Chrome's generative AI features but by default does not let Google use your prompts, inputs and outputs to improve its AI models.

## How it works
GenAiDefaultSettings sets the default for the covered generative AI features [1]. With 0, the usual default, Google may use relevant data such as prompts, inputs, outputs and feedback to improve its models, and people may review it [1]. The app sets 1: the features stay, without that use [1]. Chrome reads the policy from HKLM\SOFTWARE\Policies\Google\Chrome, also on PCs without a domain [2].

## Why it can help
You keep the features and share less data for training. It changes neither frame rate nor latency.

## Evidence
A documented Chrome policy [1]. It sets a default: a feature policy you set yourself takes precedence [1].

## Trade-offs & risks
Because these are policies, Chrome shows that it is managed by your organization, and the matching settings are locked. Undo removes them.

## When not to use it
If you want the AI features off completely; use the other Chrome AI option instead.

## Sources
1. https://chromeenterprise.google/policies/#GenAiDefaultSettings
2. https://support.google.com/chrome/a/answer/9131254?hl=en
