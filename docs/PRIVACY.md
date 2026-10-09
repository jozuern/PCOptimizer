# Privacy

[Deutsch](#datenschutz)

PCOptimizer has no telemetry, no accounts, no ads and no analytics. Scan results, backups, settings and logs stay on your PC in `%ProgramData%\PCOptimizer`. A Markdown report (`--report`) is written only to the file you name.

The app contacts the internet only for the features below, and only when you start them or turn them on.

| Feature | When | What is sent, and to whom |
|---|---|---|
| Update check (Settings > Updates) | Off by default. At start when you turn it on, or when you click "Check now" | One HTTPS request to `api.github.com` for the latest release. GitHub sees your IP address and the app version (`PCOptimizer/0.3.0`). Nothing is downloaded or installed; a newer version is only linked. |
| VirusTotal lookup (Startup page) | Only with your own API key, when you start a lookup | The SHA-256 hash of each checked file and your API key go to `virustotal.com`. Files are never uploaded. The key is stored DPAPI-encrypted for your Windows account. |
| DNS benchmark (Graphics & network) | When you start it | DNS queries for ten popular domains (Google, YouTube, Steam, Twitch, Discord, Microsoft, Reddit, Amazon, Wikipedia, Epic Games) to your current DNS servers and to Cloudflare, Google and Quad9. |
| DNS presets (Graphics & network) | When you apply one | Nothing is sent by the app. Your PC then uses that provider for DNS, so the provider sees the names your PC looks up. |
| App installs and the PawnIO sensor driver | When you install something | winget downloads the package from its source (Microsoft's winget repository and the vendor's servers). |
| Repair with DISM (Health) | When you start it | Windows downloads repair files from Windows Update. |
| Links (Store pages, driver pages, sources, GitHub) | When you click one | Opens in your browser. |

The frame time benchmark uses PresentMon, which is built into the exe and runs locally. Sensor readings stay local.

---

# Datenschutz

PCOptimizer hat keine Telemetrie, keine Konten, keine Werbung und keine Analyse. Scan-Ergebnisse, Sicherungen, Einstellungen und Protokolle bleiben auf deinem PC in `%ProgramData%\PCOptimizer`. Ein Markdown-Bericht (`--report`) wird nur in die Datei geschrieben, die du angibst.

Die App geht nur für die folgenden Funktionen ins Internet, und nur wenn du sie startest oder einschaltest.

| Funktion | Wann | Was gesendet wird, und an wen |
|---|---|---|
| Update-Prüfung (Einstellungen > Updates) | Standardmäßig aus. Beim Start, wenn du sie einschaltest, oder bei "Jetzt prüfen" | Eine HTTPS-Anfrage an `api.github.com` nach der neuesten Version. GitHub sieht deine IP-Adresse und die App-Version (`PCOptimizer/0.3.0`). Es wird nichts heruntergeladen oder installiert; eine neuere Version wird nur verlinkt. |
| VirusTotal-Abfrage (Autostart) | Nur mit deinem eigenen API-Schlüssel, wenn du eine Abfrage startest | Der SHA-256-Hash jeder geprüften Datei und dein API-Schlüssel gehen an `virustotal.com`. Dateien werden nie hochgeladen. Der Schlüssel wird mit DPAPI für dein Windows-Konto verschlüsselt gespeichert. |
| DNS-Benchmark (Grafik & Netzwerk) | Wenn du ihn startest | DNS-Anfragen nach zehn bekannten Domains (Google, YouTube, Steam, Twitch, Discord, Microsoft, Reddit, Amazon, Wikipedia, Epic Games) an deine aktuellen DNS-Server und an Cloudflare, Google und Quad9. |
| DNS-Vorgaben (Grafik & Netzwerk) | Wenn du eine anwendest | Die App sendet nichts. Dein PC nutzt danach diesen Anbieter für DNS, der Anbieter sieht also die Namen, die dein PC nachschlägt. |
| App-Installationen und der PawnIO-Sensortreiber | Wenn du etwas installierst | winget lädt das Paket aus seiner Quelle (Microsofts winget-Verzeichnis und die Server des Herstellers). |
| Reparatur mit DISM (Zustand) | Wenn du sie startest | Windows lädt Reparaturdateien von Windows Update. |
| Links (Store-Seiten, Treiberseiten, Quellen, GitHub) | Wenn du einen anklickst | Öffnet sich in deinem Browser. |

Der Frametime-Benchmark nutzt PresentMon, das in der exe enthalten ist und lokal läuft. Sensorwerte bleiben lokal.
