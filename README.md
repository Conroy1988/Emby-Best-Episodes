# Emby Best Episodes

[![Build](https://github.com/Conroy1988/Emby-Best-Episodes/actions/workflows/build.yml/badge.svg)](https://github.com/Conroy1988/Emby-Best-Episodes/actions/workflows/build.yml)

An Emby Server plugin that creates a rating-sorted playlist for every season of a configured TV series. The original season order and metadata are never changed.

The initial test target is **Ancient Aliens: Origins** on Emby Server **4.9.5.0** running natively on Windows.

## What version 0.1 does

- Reads each episode's Emby `CommunityRating`.
- Groups episodes by season.
- Sorts highest rating first, using episode number to break ties.
- Creates playlists named like `Best Rated - Ancient Aliens: Origins - Season 1`.
- Refreshes only playlists whose exact generated names match the configured prefix, series, and season.
- Leaves existing generated playlists unchanged if no eligible rated episodes are found.
- Runs manually from Emby's Scheduled Tasks page or daily at 4:00 AM by default.

Emby server plugins cannot add a new sort button to every official client. Playlists are the cross-client version: the ranked results appear in Emby Web, TV apps, Roku, Fire TV, and mobile without patching those clients.

## First-test defaults

| Setting | Default |
|---|---|
| Series | `Ancient Aliens: Origins` |
| Episodes per season | `10` |
| Minimum rating | `0` |
| Include unrated | No |
| Include specials | No |
| Public playlists | Yes |
| Playlist prefix | `Best Rated` |

## Build

Install the .NET 8 SDK, open PowerShell in this folder, and run:

```powershell
.\build.ps1
```

The output is `dist\Emby.BestEpisodes.dll`.

## Install on native Windows Emby

1. Stop Emby Server.
2. Copy `Emby.BestEpisodes.dll` to:

   `%AppData%\Emby-Server\programdata\plugins\`

3. Start Emby Server.
4. Open Dashboard > Plugins > Best Episodes and confirm the series title.
5. Open Dashboard > Scheduled Tasks > Best Episodes.
6. Run **Refresh best-rated episode playlists**.
7. Open Playlists and inspect the generated Ancient Aliens: Origins season playlists.

If that folder does not exist, use the Emby dashboard's server paths/logs to locate the active `programdata\plugins` directory before copying the DLL.

## Safety and rollback

The plugin never modifies seasons or episode ratings. To roll back, stop Emby, remove `Emby.BestEpisodes.dll`, and restart. Generated playlists are normal Emby playlists and can then be deleted manually.
