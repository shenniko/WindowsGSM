# WindowsGSM Analytics

WindowsGSM includes optional anonymous analytics to help understand which parts of the app are used, which games and plugins need attention, and where common setup or runtime problems happen.

Analytics are disabled until the user accepts the consent prompt.

## User Consent

On first launch, WindowsGSM asks:

```text
Help improve WindowsGSM by sending anonymous usage and error analytics?
```

The user can choose **Yes** or **No**. The choice is stored locally in:

```text
<WindowsGSM folder>\configs\Analytics.json
```

The same consent dialog can be opened again from:

```text
Tools > Analytics Consent
```

If analytics are disabled, WindowsGSM does not send analytics events.

## What Is Sent

WindowsGSM sends small event records for app and server activity. These events are intended to show broad usage patterns and failure points, not identify individual users or server owners.

Current events:

```text
app_start
server_created
server_deleted
server_started
server_stopped
server_restarted
steamcmd_install
steamcmd_update
plugin_installed
plugin_load_failed
discord_command_used
server_crashed
backup_completed
restore_completed
addon_installed
readiness_check_completed
plugin_search_used
```

Possible event details:

```text
app_version
dotnet_version
os_version
analytics_schema_version
schema_version
game
plugin_name
plugin_version
steam_app_id
steam_branch
install_method
start_method
stop_method
restart_method
validate
result
error_code
source
command_name
exit_code
addon_name
pass_count
warning_count
fail_count
result_count
```

Not every event includes every detail. For example, `server_started` may include the game, plugin name, and start method, while `readiness_check_completed` includes pass, warning, and fail counts.

## What Is Not Sent

WindowsGSM must not send:

- Server names
- IP addresses or ports
- File paths
- Console commands
- Discord usernames or user IDs
- Webhook URLs
- Tokens, passwords, or config contents
- Steam usernames or account details

## How It Works

WindowsGSM sends analytics to a Cloudflare Worker proxy. The app does not contain the Google Analytics API secret.

The Worker:

- Accepts only known event names
- Accepts only known event parameters
- Removes unknown parameters
- Forwards accepted events to Google Analytics 4
- Keeps the Measurement Protocol API secret outside the WindowsGSM source code and released EXE

The local analytics config stores only consent, an anonymous generated client ID, the schema version, and the analytics proxy URL.

Example:

```json
{
  "SchemaVersion": 1,
  "AnalyticsPromptShown": true,
  "AnalyticsEnabled": true,
  "ClientId": "generated-guid",
  "AnalyticsProxyUrl": "https://tight-resonance-b44e.robbie-b6b.workers.dev/"
}
```

## Viewing Data

Google Analytics 4 shows recent events in:

```text
Reports > Realtime
```

Processed event totals can be viewed in:

```text
Reports > Engagement > Events
```

More detailed tables can be built in:

```text
Explore > Free form
```

Useful exploration setup:

```text
Rows: Event name, game, plugin_name, app_version
Values: Event count
Date range: Today or Last 7 days
```

GA4 custom dimensions can take 24-48 hours to appear in standard reports and explorations after they are created.

## Troubleshooting

Cloudflare Worker logs are the quickest way to confirm whether WindowsGSM is sending analytics successfully.

Useful Worker log messages:

```text
analytics_forwarded              Request was accepted and sent to GA4
analytics_method_not_allowed     Someone opened the Worker URL directly or used a non-POST request
analytics_proxy_not_configured   Missing GA4_MEASUREMENT_ID or GA4_API_SECRET in Cloudflare
analytics_invalid_json           Request body was not valid JSON
analytics_invalid_payload        Request did not include client_id/events
analytics_no_accepted_events     Event name is not in the Worker's allow-list
analytics_ga_rejected            GA4 rejected the forwarded event
```

Opening the Worker URL in a browser sends a `GET` request, so it is expected to create an `analytics_method_not_allowed` log entry and count as an error in Cloudflare metrics.

## Maintainer Notes

When adding a new analytics event:

- Add the event in WindowsGSM code.
- Add the event name to the Cloudflare Worker allow-list.
- Add any new parameter names to the Cloudflare Worker allow-list.
- Add matching GA4 event-scoped custom dimensions if the parameter should be reportable.
- Update this document so users can see what is collected.

Do not put the GA4 Measurement Protocol API secret in the WindowsGSM source code, `Analytics.json`, or released EXE.
