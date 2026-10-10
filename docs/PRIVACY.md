# Privacy

[Deutsch](#datenschutz)

PCOptimizer has no telemetry, no accounts, no ads and no analytics. Scan results, backups, settings and logs stay on your PC in `%ProgramData%\PCOptimizer`. A Markdown report (`--report`) is written only to the file you name.

The app contacts the internet only for the features below, and only when you start them or turn them on.

| Feature | When | What is sent, and to whom |
|---|---|---|
| Update check (Settings > Updates) | Off by default. At start when you turn it on, or when you click "Check now" | One HTTPS request to `api.github.com` for the latest release. GitHub sees your IP address and the app version (for example `PCOptimizer/0.4.7`). When you click "Update now" and confirm, the exe, its SHA-256 file and its signature file (`.sig`) are downloaded from this repository's GitHub release (`github.com`, which hands the files out from `githubusercontent.com`; user agent `PCOptimizer/update`), checked, and swapped for the running exe. |
| VirusTotal lookup (Startup page) | Only with your own API key, when you start a lookup | The SHA-256 hash of each checked file and your API key go to `virustotal.com`. Files are never uploaded. The key is stored DPAPI-encrypted for your Windows account. |
| DNS benchmark (Graphics & network) | When you start it | DNS queries for ten popular domains (Google, YouTube, Steam, Twitch, Discord, Microsoft, Reddit, Amazon, Wikipedia, Epic Games) to your current DNS servers and to Cloudflare, Google, Quad9, OpenDNS and AdGuard DNS. |
| DNS presets (Graphics & network) | When you apply one | Nothing is sent by the app. Your PC then uses that provider for DNS, so the provider sees the names your PC looks up. |
| App installs and updates, the PawnIO sensor driver | When you install something or select "Update all apps" | winget downloads the package from its source (Microsoft's winget repository and the vendor's servers). |
| Adding a Windows capability or turning on a Windows feature (Tools) | When you add one | DISM downloads the capability, or the files of a feature such as .NET Framework 3.5, from Windows Update. |
| Sync the clock (Tools > Quick fixes) | When you run it | The Windows Time service asks its configured time server (`time.windows.com` unless you changed it) for the time. |
| Repair with DISM (Health) | When you start it | Windows downloads repair files from Windows Update. |
| Links (Store pages, driver pages, sources, GitHub) | When you click one | Opens in your browser. |

The frame time benchmark uses PresentMon, which is built into the exe and runs locally. Sensor readings stay local.

The Wi-Fi check reads the band of the connected Wi-Fi network through the Windows Wi-Fi API. Windows gives desktop apps this information only when location access is allowed for them, and may ask you the first time ([Microsoft](https://learn.microsoft.com/en-us/windows/win32/nativewifi/wi-fi-access-location-changes)). Without that permission the check shows "unknown". The information stays on your PC.

---

# Datenschutz

PCOptimizer hat keine Telemetrie, keine Konten, keine Werbung und keine Analyse. Scan-Ergebnisse, Sicherungen, Einstellungen und Protokolle bleiben auf deinem PC in `%ProgramData%\PCOptimizer`. Ein Markdown-Bericht (`--report`) wird nur in die Datei geschrieben, die du angibst.

Die App geht nur für die folgenden Funktionen ins Internet, und nur wenn du sie startest oder einschaltest.

| Funktion | Wann | Was gesendet wird, und an wen |
|---|---|---|
| Update-Prüfung (Einstellungen > Updates) | Standardmäßig aus. Beim Start, wenn du sie einschaltest, oder bei "Jetzt prüfen" | Eine HTTPS-Anfrage an `api.github.com` nach der neuesten Version. GitHub sieht deine IP-Adresse und die App-Version (zum Beispiel `PCOptimizer/0.4.7`). Erst wenn du auf "Jetzt aktualisieren" klickst und bestätigst, werden die exe, ihre SHA-256-Datei und ihre Signaturdatei (`.sig`) aus dem GitHub-Release dieses Repositorys geladen (`github.com`, das die Dateien über `githubusercontent.com` ausliefert; User-Agent `PCOptimizer/update`), geprüft und gegen die laufende exe getauscht. |
| VirusTotal-Abfrage (Autostart) | Nur mit deinem eigenen API-Schlüssel, wenn du eine Abfrage startest | Der SHA-256-Hash jeder geprüften Datei und dein API-Schlüssel gehen an `virustotal.com`. Dateien werden nie hochgeladen. Der Schlüssel wird mit DPAPI für dein Windows-Konto verschlüsselt gespeichert. |
| DNS-Benchmark (Grafik & Netzwerk) | Wenn du ihn startest | DNS-Anfragen nach zehn bekannten Domains (Google, YouTube, Steam, Twitch, Discord, Microsoft, Reddit, Amazon, Wikipedia, Epic Games) an deine aktuellen DNS-Server und an Cloudflare, Google, Quad9, OpenDNS und AdGuard DNS. |
| DNS-Vorgaben (Grafik & Netzwerk) | Wenn du eine anwendest | Die App sendet nichts. Dein PC nutzt danach diesen Anbieter für DNS, der Anbieter sieht also die Namen, die dein PC nachschlägt. |
| App-Installationen und -Updates, der PawnIO-Sensortreiber | Wenn du etwas installierst oder "Alle Apps aktualisieren" wählst | winget lädt das Paket aus seiner Quelle (Microsofts winget-Verzeichnis und die Server des Herstellers). |
| Hinzufügen einer optionalen Funktion oder Einschalten eines Windows-Features (Werkzeuge) | Wenn du eine hinzufügst | DISM lädt die Funktion, oder die Dateien eines Features wie .NET Framework 3.5, über Windows Update. |
| Uhr synchronisieren (Werkzeuge > Schnelle Hilfen) | Wenn du es startest | Der Windows-Zeitdienst fragt seinen eingestellten Zeitserver (`time.windows.com`, wenn du nichts geändert hast) nach der Uhrzeit. |
| Reparatur mit DISM (Zustand) | Wenn du sie startest | Windows lädt Reparaturdateien von Windows Update. |
| Links (Store-Seiten, Treiberseiten, Quellen, GitHub) | Wenn du einen anklickst | Öffnet sich in deinem Browser. |

Der Frametime-Benchmark nutzt PresentMon, das in der exe enthalten ist und lokal läuft. Sensorwerte bleiben lokal.

Die WLAN-Prüfung liest das Frequenzband des verbundenen WLANs über die Windows-WLAN-Schnittstelle. Windows gibt Desktop-Apps diese Information nur, wenn der Standortzugriff für sie erlaubt ist, und fragt beim ersten Mal eventuell nach ([Microsoft](https://learn.microsoft.com/en-us/windows/win32/nativewifi/wi-fi-access-location-changes)). Ohne diese Erlaubnis zeigt die Prüfung „unbekannt“. Die Information bleibt auf deinem PC.
