# Intel Application Optimization (APO)

## Summary
::: status Info
{{cpu}} supports Intel APO, which improves performance in a list of supported games. It needs the board's Dynamic Tuning driver and the APO app.
:::
::: status Ok,Unknown,Problem,Unsupported
Checks whether the processor supports Intel Application Optimization.
:::

## Why it matters
Intel APO adjusts how a supported game's threads are spread over performance and efficient cores. Intel lists the supported games; in those, frame rates improve, in other games nothing changes [1]. It only works on specific K processors and needs support from the mainboard (Intel Dynamic Tuning Technology driver).
::: variant dttFound
The Dynamic Tuning driver is installed on this PC.
:::
::: variant dttNotFound
We did not find the Dynamic Tuning driver. It may be missing or use a name we do not know.
:::

## How we detected it
We match the processor name with the list of supported models and look for the Dynamic Tuning services. Whether the APO app is installed and active cannot be read reliably.

## How to fix
1. Install the **Intel Dynamic Tuning Technology** driver from the support page of your {{board}} (not from Intel directly; it is board-specific).
2. Install **Intel Application Optimization** from the Microsoft Store.
3. Some boards need APO enabled in the BIOS (often under Intel Dynamic Tuning or "Application Optimization").
4. Open the APO app and check that it is on for your games.

## How to check the fix
The APO app lists supported games and shows whether it is active.

## Sources
1. https://www.kitguru.net/components/cpu/joao-silva/intel-apo-receives-12-new-game-profiles-core-ultra-200k-series-now-supported/
