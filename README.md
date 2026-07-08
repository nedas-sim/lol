# lol-bot

![lol](https://media1.tenor.com/m/aLCR52_vKIkAAAAd/lol.gif)

A Windows utility that types `:lol:` wherever your cursor is, triggered by a keyboard shortcut, and logs every use to a SQLite database so you can obsess over your emoji statistics.

## How it works

PowerToys keyboard shortcut → `run.cmd` → `lol-bot.cs` types `:lol:` via `SendInput` and inserts a UTC timestamp into `lol.db`.

## Files

| File | Purpose |
|---|---|
| `lol-bot.cs` | Main app — types `:lol:`, logs timestamp to SQLite |
| `run.cmd` | Entry point for PowerToys shortcut |
| `migrate.cs` | Full historical import — wipes `lols` table, reloads from a dump file |
| `sync.cs` | Partial sync — deletes rows from a given date onward, reloads from a dump file |
| `stats.html` | Browser stats viewer (Chrome/Edge only — uses File System Access API) |
| `lol.db` | SQLite database, gitignored |

## Database schema

```sql
CREATE TABLE lols (
    id        INTEGER PRIMARY KEY,
    lolled_at TEXT NOT NULL  -- ISO 8601 UTC
);
```

One row per `:lol:` typed or imported.

## Setup

1. Point a PowerToys Run shortcut at `run.cmd`
2. That's it

## Syncing from Slack

Since most `:lol:` usage happens in Slack (sometimes 132 at a time), there's a Claude Code skill to pull the ground-truth data directly from Slack and sync it into the database.

In Claude Code (from this directory):

```
/sync-lols
```

It will ask how many days back to sync, search Slack, show a summary, write a dated dump file, and call `sync.cs` to update the database.

### Manual import

To rebuild the entire database from a dump file:

```
dotnet run migrate.cs
```

Edit `migrate.cs` to point at the desired dump file first.

### Running sync.cs directly

```
dotnet run sync.cs -- <from-date> <dump-file>
```

Example:

```
dotnet run sync.cs -- 2026-07-05 "dump 07-05 - 07-08.txt"
```

Deletes all rows `>= from-date` and inserts from the dump file.

## Dump file format

Tab-separated, no header. Third column is the count of `:lol:` in that Slack message — each gets expanded into N individual rows in the database.

```
1	2026-07-08T06:38:24.4685890Z	1
2	2026-07-08T15:23:28.3385490Z	26
```
