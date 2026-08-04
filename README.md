# Emby Best Episodes

[![Build](https://github.com/Conroy1988/Emby-Best-Episodes/actions/workflows/build.yml/badge.svg)](https://github.com/Conroy1988/Emby-Best-Episodes/actions/workflows/build.yml)

An Emby Server plugin that creates rating-sorted playlists for TV seasons. The original season order and metadata are never changed.

Tested first with **Ancient Aliens: Origins** on Emby Server **4.9.5.0** running natively on Windows.

## What version 0.2 does

- Lets you select one or more shows from a dropdown populated directly from your Emby TV library.
- Can process every series visible to the playlist owner.
- Reads each episode's Emby `CommunityRating` and sorts highest rating first.
- Supports top 5, top 10, top 20, or all eligible episodes per season.
- Supports a minimum rating and optional unrated episodes or specials.
- Can exclude episodes already watched by the playlist owner.
- Creates playlists named like `Best Rated - Ancient Aliens: Origins - Season 1`.
- Refreshes automatically after a library scan, manually from Scheduled Tasks, or daily at 4:00 AM.
- Safely serializes concurrent refresh attempts so two jobs cannot edit the same playlist at once.

Emby server plugins cannot add a new sort button to every official client. Playlists are the cross-client version: the ranked results appear in Emby Web, TV apps, Roku, Fire TV, and mobile without patching those clients.

## Defaults

| Setting | Default |
|---|---|
| Series | Existing 0.1 installs continue using `Ancient Aliens: Origins` until a dropdown choice is saved |
| Process all series | No |
| Episodes per season | Top 10 |
| Minimum rating | 0 |
| Include unrated | No |
| Include specials | No |
| Exclude watched | No |
| Refresh after library scan | Yes |
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
2. Replace `Emby.BestEpisodes.dll` in `%AppData%\Emby-Server\programdata\plugins\`.
3. Start Emby Server.
4. Open Dashboard > Plugins > Best Episodes.
5. Select one or more shows from **TV series**, then save.
6. Open Dashboard > Scheduled Tasks > Best Episodes and run **Refresh best-rated episode playlists** once.
7. Open Playlists and inspect the generated season playlists.

If that plugin folder does not exist, use the Emby dashboard's server paths/logs to locate the active `programdata\plugins` directory before copying the DLL.

## Safety and rollback

The plugin never modifies seasons, episodes, ratings, or watched state. To roll back, stop Emby, remove `Emby.BestEpisodes.dll`, and restart. Generated playlists are normal Emby playlists and can then be deleted manually.
