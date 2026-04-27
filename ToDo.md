# ToDo / Ideas

## Existing Ideas

- Minecraft / Java detection
  - Check Windows environment variables and common install locations for Java.
  - Stop relying on a static 32-bit Java path for Minecraft-related flows.
  - Consider validating the detected Java version against plugin or server requirements.
- Firewall rule improvements
  - Current firewall rule adds the server executable as an allowed app, which allows all TCP/UDP ports for that exe.
  - Replace with explicit inbound port rules where possible.
  - Start with WindowsGSM.cfg values:
    - Server port
    - Query port
    - RCON/admin port
  - Add protocol selection: TCP / UDP / Both.
  - Later, allow plugins/game definitions to declare extra firewall ports such as Steam, beacon, telnet, web admin, voice, or derived offset ports.
  - Keep broad executable allow as a fallback for games/plugins without explicit firewall metadata.
- Server health checks and recovery
  - Add query/RCON heartbeat checks so a hung server is not treated as healthy just because the process is still running.
  - Allow auto-restart thresholds based on repeated failed checks.
  - Surface the last successful health check and recent failure reason in the UI.
- Config validation
  - Validate ports, paths, cron syntax, tokens, duplicate server names, and required runtime locations before saving.
  - Warn about conflicting ports across configured servers.
  - Surface invalid plugin custom settings with field-level validation in the UI.
- In-app crash triage
  - Show a crash summary in the dashboard using the generated crash logs.
  - Include exit code, likely error line, last restart count, and a short recent-log excerpt.
  - Make it easy to jump from a server row to its latest crash log.
- Plugin capability metadata
  - Let plugins declare supported capabilities such as query method, RCON support, required runtime, firewall ports, backup exclusions, and update channels.
  - Use this metadata to remove hardcoded per-game assumptions from the core app.
- Large-library management
  - Add search/filter/sort for servers by game, status, tags, favorite, port, or machine role.
  - Consider saved views for admins running many instances.
- Secret handling
  - Improve storage for webhook URLs, bot tokens, GSLTs, and admin credentials.
  - Prefer protected storage or encryption for sensitive values instead of plain text config where practical.
- Import / export server profiles
  - Export server definitions and WindowsGSM-managed settings separately from full file backups.
  - Make migration and reproducible server setup easier across machines.
- Port collision detection
  - Detect overlapping game/query/RCON/admin ports across all configured servers.
  - Warn before save and offer quick navigation to the conflicting server entries.

## Review Findings To Fix

- SteamCMD config parsing and install diagnostics
  - `WindowsGSM/Installer/SteamCMD.cs:105` and `WindowsGSM/Installer/SteamCMD.cs:285` read `line[0]` / `line[1]` without checking the line length first.
  - Harden `userData.txt` parsing against blank lines, short lines, missing `=`, unquoted values, and malformed credentials.
  - `WindowsGSM/Installer/SteamCMD.cs:50` extracts SteamCMD without overwrite/recovery, so a partial previous extraction can break future installs.
  - Replace broad swallowed exceptions in SteamCMD install/update/download paths with logged failure reasons that can be shown in the UI.
  - Review `WindowsGSM/Installer/SteamCMD.cs:363` so `-overrideminos` and custom branch arguments are composed once and predictably.
- Scheduler / crontab reliability
  - `WindowsGSM/Functions/CrontabManager.cs:39` warns that the scheduler must run on the main thread or the thread can be killed without trace.
  - Split scheduler parsing/execution from UI work so scheduled actions can run safely on background tasks and marshal UI changes explicitly.
  - `WindowsGSM/Functions/CrontabManager.cs:105` uses raw `Split(';')`; replace with a small parser that validates fields and preserves semicolons in payloads.
  - `WindowsGSM/Functions/CrontabManager.cs:192` disposes tracked tasks instead of awaiting/cancelling them; add cancellation and completion handling.
  - `WindowsGSM/Functions/CrontabManager.cs:236` parses the RCON port without validation; report bad config instead of throwing during schedule execution.
  - `WindowsGSM/Functions/CrontabManager.cs:248` uses arbitrary scheduled command names in process/log handling; validate executable paths and sanitize log file names.
- RCON connection handling
  - `WindowsGSM/Functions/RconClient.cs:33` uses `IPEndPoint.Parse(ip)` for an IP-only value; switch to `IPAddress.TryParse` plus `new IPEndPoint(address, port)`.
  - `WindowsGSM/Functions/RconClient.cs:55` creates an RCON connection that is only disposed on success; wrap the connection in `using` / `finally`.
  - Return structured RCON results so callers can distinguish connection failures, authentication failures, command errors, and command output.
- Discord bot config validation
  - `WindowsGSM/DiscordBot/Configs.cs:26` lists `<SERVERIID>` in command help; fix to `<SERVERID>`.
  - `WindowsGSM/DiscordBot/Configs.cs:118` accepts any refresh-rate integer; clamp to a safe minimum/maximum and show invalid config clearly.
  - `WindowsGSM/DiscordBot/Configs.cs:161` can read `items[1]` from an admin ID line that does not contain a server list; validate the admin file format.
  - Move bot tokens and other Discord secrets toward protected storage instead of plain text where practical.
- Backup and restore safety
  - `WindowsGSM/Functions/BackupConfig.cs:220` rewrites backup config while loading; separate read, migration, and save so opening config does not unexpectedly mutate user paths.
  - `WindowsGSM/Functions/BackupConfig.cs:250` can rewrite paths based on matching directory tails; make migration explicit or show the before/after path before saving.
  - Add validation for missing save/file locations, inaccessible backup destinations, invalid maximum backup counts, and paths outside expected server folders.
  - Keep confirmation prompts around restore, delete, force stop, and destructive backup cleanup.
- Plugin loading and diagnostics
  - `WindowsGSM/Functions/PluginManagement.cs:45` / `WindowsGSM/Functions/PluginManagement.cs:97` log plugin load failures, but the UI should expose the compiler/runtime error directly from the plugin row.
  - `WindowsGSM/Functions/RoslynCompiler.cs:59` reports only the first compiler error in the exception message; include all diagnostics and line numbers in the plugin failure UI.
  - `WindowsGSM/Functions/RoslynCompiler.cs:69` loads plugin assemblies into the default `AssemblyLoadContext`; investigate collectible contexts or process isolation for plugin reload/unload.
  - Keep unsafe plugin support intentional: `WindowsGSM/Functions/RoslynCompiler.cs:43` sets `allowUnsafe: true`, so plugin trust boundaries should be documented in-app.
- Firewall API behavior
  - `WindowsGSM/WindowsFirewall.cs:34` and `WindowsGSM/WindowsFirewall.cs:63` use legacy authorized-app rules instead of explicit inbound port rules.
  - `WindowsGSM/WindowsFirewall.cs:91` removes rules by path substring; replace with deterministic rule names/IDs to avoid removing unrelated rules.
  - Log firewall failures instead of returning `false` silently so admin/elevation problems are obvious.
- Java runtime handling
  - `WindowsGSM/Functions/JavaHelper.cs:10` hardcodes JDK 22 installer metadata while `DownloadJREToServer` accepts a version argument that is not actually used.
  - `WindowsGSM/Functions/JavaHelper.cs:60` waits forever for `java.exe`; add timeout, process exit checks, and a useful error message.
  - `WindowsGSM/Functions/JavaHelper.cs:177` compares Java versions as strings; parse semantic/version components so `17` / `21` / `8` sort correctly.
- Destructive operations and recovery
  - Review direct `Directory.Delete`, `File.Delete`, and `Process.Kill` paths in `MainWindow.xaml.cs` and game server classes.
  - Standardize confirmation prompts and recovery logging for delete, restore, force stop, import overwrite, backup rotation cleanup, and failed addon extraction.
  - Add operation state tracking so install, update, start, stop, restart, backup, restore, and addon actions cannot overlap in unsafe ways.

## Implementation Order

1. Critical crash and data-safety fixes - first pass complete
   - [x] Harden SteamCMD credential/config parsing.
   - [x] Fix RCON endpoint parsing and disposal.
   - [x] Fix Discord command typo, admin ID parsing, and refresh-rate validation.
   - [x] Add timeouts and failure messages to Java installation.
2. Operation state tracking and UI locking - first pass complete
   - [x] Add a shared operation-state model for install, update, start, stop, restart, backup, restore, delete, force stop, and addon actions.
   - [x] Disable conflicting toolbar/menu actions while an operation is running.
   - [x] Surface active operation, elapsed time, and last error in the UI/log panel.
3. Validation layer
   - Centralize validation for paths, ports, duplicate names, missing executables, bad cron expressions, bad backup settings, invalid Discord settings, and invalid plugin custom settings.
   - Block saves for dangerous invalid values and show field-level messages where possible.
4. Scheduler rewrite
   - Replace raw crontab string splitting with a validated schedule model.
   - Add cancellation, awaited task completion, safe command execution, sanitized logs, and RCON config validation.
5. Backup / restore hardening
   - Separate backup config migration from normal loading.
   - Validate backup sources/destinations before archive work begins.
   - Confirm destructive restore/delete/rotation actions and log exactly what changed.
6. Firewall rule modernization
   - Add explicit inbound port rules from server config.
   - Use stable rule names and keep broad executable allow only as fallback.
7. Plugin diagnostics and capability metadata
   - Show compiler/runtime plugin errors in the UI.
   - Add metadata for query method, RCON support, required runtime, firewall ports, backup exclusions, and update channels.
   - Investigate safer plugin reload/unload behavior.
8. Server health checks and recovery
   - Add query/RCON heartbeat monitoring.
   - Auto-restart on repeated unhealthy checks.
   - Show last success/failure reason in the dashboard.
9. Secret handling
   - Move webhook URLs, bot tokens, GSLTs, and credentials to protected storage where possible.
   - Add migration from existing plain text files/config entries.
10. Admin quality-of-life features
   - Add search/filter/sort and saved views for large server libraries.
   - Add import/export of server profiles.
   - Add in-app crash triage and quick links to crash logs.
