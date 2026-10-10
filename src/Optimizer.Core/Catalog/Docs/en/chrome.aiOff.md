# Google Chrome: generative AI features off

## Summary
Turns off the generative AI features Google covers by one default, Gemini in Chrome and AI Mode in the address bar, and stops the download of Chrome's local AI model.

## How it works
GenAiDefaultSettings set to 2 makes "Do not allow" the default for the generative AI features it covers; a feature policy set on its own takes precedence [1]. GeminiSettings and AIModeSettings only know allow and do not allow, so the app sets both to do not allow [2][3]. GenAILocalFoundationalModelSettings set to 1 stops the download of the local AI model and deletes one already downloaded [4]. Chrome reads these policies from HKLM\SOFTWARE\Policies\Google\Chrome, also on PCs without a domain [5].

## Why it can help
Chrome sends no page content or prompts to Google's AI services, and the local model takes no disk space.

## Evidence
Documented Chrome policies [1][2][3][4]. It changes neither frame rate nor latency.

## Trade-offs & risks
Gemini, AI Mode and the other covered AI features are gone. Because these are policies, Chrome shows that it is managed by your organization, and the matching settings are locked. Undo removes them.

## When not to use it
If you use Gemini or other AI features in Chrome; the option without model training keeps them.

## Sources
1. https://chromeenterprise.google/policies/#GenAiDefaultSettings
2. https://chromeenterprise.google/policies/#GeminiSettings
3. https://chromeenterprise.google/policies/#AIModeSettings
4. https://chromeenterprise.google/policies/#GenAILocalFoundationalModelSettings
5. https://support.google.com/chrome/a/answer/9131254?hl=en
