# Gaming impact research (October 2026)

What really changes frame rate, frame time consistency, latency or load times on Windows 11 24H2 and newer, and what
Tunero does with it. Five research passes on 2026-10-12/13 (GPU and display, CPU and power, memory and storage, network
and input, measured studies) read the sources below; every number here comes from a page that was opened. Ratings follow
CONTRIBUTING: impact 0 to 5 only as far as the evidence goes.

## The short version

- The large, repeated, measured effects come from **conditions the scan can detect**, not from registry tweaks: memory
  integrity on processors limited games, Resizable BAR off on Intel Arc, memory below its XMP or EXPO speed, a slow PCIe
  link for 8 GB cards once video memory overflows, a dual-CCD Ryzen X3D whose cache scheduling fails, a saturated
  network.
- Classic "gaming tweaks" (priority, affinity, timer resolution, core parking, Nagle, interrupt moderation, standby list
  cleaning, services off) still have no reputable Windows 11 measurement. Their 0 ratings stand.
- No reputable outlet has benchmarked optimizer tools (Razer Cortex, WinUtil, Hone, Process Lasso). The one debloated
  Windows test (PCGH, AtlasOS, 2024) did not report the memory integrity state, so its 2 to 8 % cannot be credited to
  debloating.

## Done in this change

| Item | What | Rating | Evidence |
|---|---|---|---|
| F31.memoryIntegrity | New check: memory integrity running (Info), fix = Expert tweak security.vbsOff | 2, 3 without MBEC/GMET | ComputerBase 2023: 6 % average, 8 % 1 % lows, Ryzen 7 5800X, 720p, 14 games [1]; close to 10 % on a Ryzen 7 1800X [2]; Windows turns it on for more PCs from October 2026 [3] |
| security.vbsOff | Basis measured, hotpatch trade-off, VBS-only cost marked as disputed | 2 / 3 | [1][2][4][3] |
| A.rebar | Rating per vendor | Arc 4, AMD 2, NVIDIA 1 | Arc A770: 77 % of performance without it at 1080p [5]; AMD up to 17.5 % in recent games [6], 3 % average [7]; NVIDIA 0 to 2 % average [7] |
| A.xmp | Rating per processor | 3, X3D and Intel 13th/14th gen 1 | TechSpot 2024, 13 games: 7700X +12 %, 12700K +11 %, 7800X3D and 14700K +4 % [8] |
| gpu.gameMode | Override on dual-CCD Ryzen X3D | 3 there, 1 elsewhere | AMD requires Game Mode and Game Bar for X3D scheduling [9]; a 7950X3D without the required updates was slower than a 7950X, Factorio +80 % on the right CCD [10]; Game Mode processor power profile [11] |
| F7.nvidiaGlobal + fix.nvidiaFrameCap | G-SYNC with V-Sync forced on and no cap: global frame limit 3 FPS below refresh (Preview) | 2 (latency) | Blur Busters: 2 to 6 frames of V-Sync delay at the G-SYNC ceiling, 2 FPS below refresh avoids it [12][13]; FRL_FPS_ID in NVIDIA's public header [14] |

## Next candidates, ranked

1. **A.pcieLink by bandwidth.** Judge generation times width: Gen 5 x8 or Gen 4 x16 costs about 1 % on an RTX 5090,
   Gen 3 x16 2 to 4 %, Gen 4 x4 6 to 11 % [15][16]. Today a Gen 5 card in a Gen 5 x8 slot is a Problem with impact 2.
2. **8 GB card with 8 or fewer lanes on a PCIe 3.0 or 4.0 slot** (Info, 3 situational). RTX 5060 Ti 8 GB: 1 % lows +111 %
   from PCIe 3.0 to 4.0 once a game exceeds video memory; little effect within it [17]. All inputs are read already.
3. **Dual-CCD X3D prerequisites in F10:** Game Mode off, Game Bar missing or older than 5.823.1271.0, the V-Cache
   optimizer service missing, chipset driver older than 5.02.16.347 [9][10]. 3 situational.
4. **Network busy check:** Delivery Optimization, Steam or OneDrive downloading while you play. RTINGS measured very bad
   Counter-Strike 2 latency on all 11 routers with a full connection and no router QoS [18]. 3, only when saturated.
5. **Gaming over Wi-Fi while an Ethernet port is free.** RTINGS: 6 to 13 ms median latency and up to 10.4 ms jitter on
   Wi-Fi under load, about 10 ms more through a mesh satellite [19]. 2 measured.
6. **16 GB RAM with 8 GB of video memory or less** (extend F13.lowRam). TechSpot 2026: 1 % lows in Spider-Man 2 more than
   60 % higher with 32 GB on an 8 GB RX 9060 XT; 16 GB was enough with 12 GB or more video memory [20]. 2 measured.
7. **Known regressions table:** Windows update KB5066835 with an NVIDIA driver older than 581.94 (NVIDIA confirmed lower
   performance [21]); the inpoutx64 driver from RGB software (Microsoft confirmed game crashes [22]).
8. **NVIDIA App game filters on:** 2 to 12 % lower in Tom's Hardware's test with the option on but unused [23]; off by
   default since App 11.0.1. Needs a verified place where the App stores it.
9. **Mouse report rate test** (started by the user): at 4 to 8 kHz some games lose most of their frame rate on Windows
   11 24H2 in Godot's measurements; most processors reach the limit between 1 and 2 kHz [33]. The fix is a step (1000 Hz
   in the mouse software).
10. **F6.turbo extension:** also read PROCTHROTTLEMAX1, PROCFREQMAX/PROCFREQMAX1 and PERFEPP/PERFEPP1 [24][25][26].
11. **Laptop on mains power not in "Best performance"** (1, documented mechanism only [27]) and an information check for
    the laptop maker's performance mode (one measured laptop: 12 to 16 % in Turbo [28]).
12. **Rating corrections still open:** F12.gamesOnHdd and A.systemHdd to measured load times (TechSpot 2025: more than four
    minutes in Kingdom Come: Deliverance II from a hard disk [29]); network.nicPowerSavingOff to 0 except Intel I225-V.
13. **Needs an owner decision:** a Wi-Fi low-latency game session (documented WlanSetInterface calls that turn off
    background scanning; each scan leaves a gap of 100 ms or more [30]). It only lasts while Tunero runs, which conflicts
    with "no resident component" unless offered as a session the user starts and ends.

## Rejected, with the reason

- Process priority, affinity, timer resolution tools, core parking, E-cores off: no reputable Windows 11 measurement;
  timer requests are per process since Windows 10 2004 [31].
- Power throttling off on mains power: background apps are moved to efficient cores only on battery [32].
- Defender exclusions, Defender or BitLocker off, Dev Drive for games: no game measurement, real security cost.
- Standby list cleaners, SysMain off, memory compression off, search indexing off, page file placement: no measurement.
- Nagle off, interrupt moderation, RSS tuning, QoS DSCP tags, Teredo off: microseconds on the PC, while ping is
  milliseconds on the route.
- HAGS, Game Mode, power plan comparisons, optimizations for windowed games: no measurement newer than 2020 that changes
  the ratings.
- NVIDIA Ultra Low Latency through undocumented values, forced Resizable BAR, forcing the native NVMe driver: risky and
  undocumented, see not-included.md.
- AMD Anti-Lag or Chill control: measured 0.5 to 1.9 ms in one game, but the settings library (ADLX) is under AMD's own
  license, which needs a review before it can go into a GPL-3.0-only exe.

## Sources

1. https://www.computerbase.de/2023-03/windows-11-vbs-hvci-spiele-benchmarks/
2. https://www.computerbase.de/artikel/betriebssysteme/windows-11-ul-benchmarks-weist-auf-leistungsverlust-durch-vbs-hin.78126/
3. https://techcommunity.microsoft.com/blog/windows-itpro-blog/expanding-memory-integrity-protection-across-windows-devices/4551984
4. https://www.tomshardware.com/news/windows-11-gaming-benchmarks-performance-vbs-hvci-security
5. https://thinkcomputers.org/intel-arc-a7-gpus-become-useless-without-resizable-bar/
6. https://www.club386.com/?p=145731
7. https://www.techspot.com/review/2234-nvidia-resizable-bar/
8. https://www.techspot.com/review/2866-ddr5-ram-stock-vs-xmp-expo/
9. https://www.tomshardware.com/news/amd-ryzen-7000x3d-install-guide
10. https://www.techspot.com/review/2636-amd-ryzen-9-7950x3d/
11. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
12. https://blurbusters.com/gsync/gsync101-input-lag-tests-and-settings/4/
13. https://blurbusters.com/gsync/gsync101-input-lag-tests-and-settings/5/
14. https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
15. https://www.techpowerup.com/review/nvidia-geforce-rtx-5090-pci-express-scaling/33.html
16. https://gamersnexus.net/gpus/nvidia-rtx-5090-pcie-50-vs-40-vs-30-x16-scaling-benchmarks
17. https://www.techspot.com/review/3004-nvidia-rtx-5060-ti-pcie-benchmark/
18. https://www.rtings.com/router/learn/research/router-latency
19. https://www.rtings.com/router/learn/research/wireless-latency
20. https://www.techspot.com/review/3076-how-much-ram-2026/
21. https://overclock3d.net/news/software/nvidia-geforce-hotfix-581-94-addresses-windows-update-performance-regression/
22. https://www.bleepingcomputer.com/news/microsoft/microsoft-rolls-out-fix-for-windows-11-crashes-gaming-issues/
23. https://www.tomshardware.com/pc-components/gpus/we-tested-nvidia-app-performance-problems-games-run-up-to-15-percent-slower-with-the-app
24. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-maxperformance
25. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-maxfrequency
26. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfenergypreference
27. https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/customize-power-slider
28. https://www.notebookcheck.net/Turbo-vs-Performance-mode-how-one-setting-dramatically-changes-the-Asus-TUF-Gaming-A18.1168170.0.html
29. https://www.techspot.com/article/3023-ssd-gaming-comparison-load-times/
30. https://learn.microsoft.com/en-us/windows/win32/api/wlanapi/nf-wlanapi-wlansetinterface
31. https://learn.microsoft.com/en-us/windows/win32/api/timeapi/nf-timeapi-timebeginperiod
32. https://learn.microsoft.com/en-us/windows/win32/procthread/quality-of-service
33. https://godotengine.org/article/fixing-high-polling-rate-mice-on-windows/
