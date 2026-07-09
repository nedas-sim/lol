---
name: sync-lols
description: Fetches :lol: Slack messages for the last N days and syncs them as source of truth into lol.db. Use when user says "sync lols", "update lol db", "fetch lol data", or invokes /sync-lols.
---

# sync-lols

## Overview

Fetches all `:lol:` Slack messages sent by the user for the last N days, writes a dated dump file, then calls `sync.cs` to do a partial wipe + reinsert on `lol.db`.

## Step-by-step

### 1. Get Slack user ID
Load `mcp__claude_ai_Slack__slack_search_public_and_private` via ToolSearch. Its description contains the line "Current logged in user's user_id is XXXXXXX" — extract that ID as `$SLACK_USER_ID`.

If the tool is unavailable or returns no user ID, tell the user: "Slack MCP isn't connected. Open claude.ai in your browser, connect Slack under Settings → Integrations, then re-run this skill." Stop here.

### 2. Ask for N
Ask the user: "How many days back should I sync?" Wait for their answer.

### 3. Compute window
- `window_start` = today (UTC) minus N days, formatted as `YYYY-MM-DD`
- `search_after` = window_start minus 1 day, formatted as `YYYY-MM-DD` (Slack's `after:` is exclusive, so subtract 1 day to include messages on window_start itself)
- `today_str` = today formatted as `MM-DD`
- `from_str` = window_start formatted as `MM-DD`
- Dump file name: `dump <from_str> - <today_str>.txt` in the project root

### 4. Search Slack (paginated)
Use `mcp__claude_ai_Slack__slack_search_public_and_private` with:
- `query`: `from:<@$SLACK_USER_ID> :lol: after:<search_after>`
- `sort`: `timestamp`, `sort_dir`: `asc`
- `limit`: 20, `include_context`: false, `response_format`: detailed

Paginate using the cursor until `End of results`. Collect every result's `Message_ts` and `Text`.

### 5. Build rows
For each message:
- Convert `Message_ts` (Unix float, e.g. `1783522353.009439`) to UTC ISO 8601:
  - Integer part → UTC datetime (times shown as EEST = UTC+3, so subtract 3h)
  - Decimal part (6 digits) → append `0` to get 7 fractional digits
  - Format: `YYYY-MM-DDTHH:MM:SS.fffffffZ`
- **Filter**: drop any row whose UTC date is before `window_start` (Slack may return messages from `search_after` day that are outside the delete window — inserting them would cause duplicates)
- Count `:lol:` occurrences in the message text (each `:lol:` is 5 chars, they may be space-separated or concatenated)
- Row number = sequential 1-based index across kept rows only

### 6. Show summary and confirm
Report:
- Total messages found
- Total :lol: count (sum of all counts)
- Window: `<window_start>` → today

Ask: "Proceed with sync?" — wait for confirmation before continuing.

### 7. Write dump file
Format (tab-separated, no header):
```
1	2026-07-05T06:38:24.4685890Z	1
2	2026-07-05T07:01:48.4756990Z	3
```

Write to the project root: `dump <from_str> - <today_str>.txt`

### 8. Run sync.cs
```
dotnet run sync.cs -- <window_start> "dump <from_str> - <today_str>.txt"
```

Report the output (deleted rows + inserted rows).

## Notes
- Slack user ID is read dynamically from the tool description at runtime — works for any logged-in user
- `lol.db` lives in the project root
- `sync.cs` deletes all rows where `lolled_at >= window_start` then inserts from dump
- migrate.cs still exists for full historical reloads from a manual dump file
