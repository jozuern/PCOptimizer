# Google Chrome: no promotions and price tracking

## Summary
Chrome shows no product promotions, such as pages asking you to sign in or make Chrome the default, and no price tracking for products.

## How it works
PromotionsEnabled set to off stops Chrome's product promotional content, including welcome pages for signing in, setting Chrome as default browser and new features [1]. ShoppingListEnabled set to off removes the option to track the price of a product on the current page [2]. Chrome reads the policies from HKLM\SOFTWARE\Policies\Google\Chrome, also on PCs without a domain [3].

## Why it can help
Fewer prompts and offers while you browse. It changes neither frame rate nor latency.

## Evidence
Documented Chrome policies [1][2].

## Trade-offs & risks
No price alerts for products. Because these are policies, Chrome shows that it is managed by your organization, and the matching settings are locked. Undo removes them.

## When not to use it
If you track prices in Chrome.

## Sources
1. https://chromeenterprise.google/policies/#PromotionsEnabled
2. https://chromeenterprise.google/policies/#ShoppingListEnabled
3. https://support.google.com/chrome/a/answer/9131254?hl=en
