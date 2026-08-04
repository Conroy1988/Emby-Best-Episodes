using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.BestEpisodes.Ranking;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace Emby.BestEpisodes
{
    internal sealed class BestEpisodesService
    {
        private static readonly SemaphoreSlim RefreshLock = new SemaphoreSlim(1, 1);

        private readonly ILibraryManager _libraryManager;
        private readonly IPlaylistManager _playlistManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public BestEpisodesService(
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager,
            IUserDataManager userDataManager,
            IUserManager userManager,
            ILogger logger)
        {
            _libraryManager = libraryManager;
            _playlistManager = playlistManager;
            _userDataManager = userDataManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task RefreshAsync(
            PluginOptions options,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            await RefreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var owner = ResolveOwner(options.OwnerUserName);
                var seriesItems = ResolveSeries(owner, options);
                var playlists = GetVisiblePlaylists(owner).ToList();
                var duplicateNames = new HashSet<string>(
                    seriesItems.GroupBy(item => item.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key),
                    StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < seriesItems.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var series = seriesItems[index];
                    var seriesLabel = duplicateNames.Contains(series.Name ?? string.Empty)
                        ? string.Format("{0} (library ID {1})", series.Name, series.InternalId)
                        : series.Name;
                    await RefreshSeriesAsync(
                            options,
                            owner,
                            series,
                            seriesLabel,
                            playlists,
                            cancellationToken)
                        .ConfigureAwait(false);

                    progress?.Report((index + 1) * 100d / seriesItems.Count);
                }
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        internal static string BuildPlaylistName(string prefix, string seriesName, int seasonNumber)
        {
            var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "Best Rated" : prefix.Trim();
            var seasonLabel = seasonNumber == 0 ? "Specials" : "Season " + seasonNumber;
            return string.Format("{0} - {1} - {2}", safePrefix, seriesName, seasonLabel);
        }

        internal static IReadOnlyCollection<long> ParseSeriesIds(string selectedSeriesIds)
        {
            if (string.IsNullOrWhiteSpace(selectedSeriesIds))
            {
                return Array.Empty<long>();
            }

            var ids = new HashSet<long>();
            foreach (var value in selectedSeriesIds.Split(','))
            {
                if (long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private async Task RefreshSeriesAsync(
            PluginOptions options,
            User owner,
            Series series,
            string seriesLabel,
            IList<Playlist> playlists,
            CancellationToken cancellationToken)
        {
            var episodes = GetEpisodes(owner, series);
            var eligibleEpisodes = options.ExcludeWatchedEpisodes
                ? episodes.Where(item => !IsPlayed(owner, item)).ToList()
                : episodes;

            var seasonNumbers = episodes
                .Select(item => item.ParentIndexNumber ?? 0)
                .Where(number => options.IncludeSpecials || number > 0)
                .Distinct()
                .OrderBy(number => number)
                .ToList();

            var rankedSeasons = EpisodeRanker.RankBySeason(
                eligibleEpisodes.Select(ToCandidate),
                (int)options.EpisodesPerSeason,
                options.MinimumRating,
                options.IncludeUnratedEpisodes,
                options.IncludeSpecials);

            foreach (var seasonNumber in seasonNumbers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var playlistName = BuildPlaylistName(options.PlaylistPrefix, seriesLabel, seasonNumber);
                var existing = playlists.FirstOrDefault(item =>
                    string.Equals(item.Name, playlistName, StringComparison.OrdinalIgnoreCase));
                var itemIds = rankedSeasons.TryGetValue(seasonNumber, out var ranked)
                    ? ranked.Select(item => item.ItemId).ToArray()
                    : Array.Empty<long>();

                if (existing == null && itemIds.Length == 0)
                {
                    continue;
                }

                if (existing == null)
                {
                    await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
                    {
                        Name = playlistName,
                        User = owner,
                        ItemIdList = itemIds,
                        MediaType = MediaType.Video,
                        IsPublic = options.CreatePublicPlaylists
                    }).ConfigureAwait(false);

                    _logger.Info("Created playlist {0} with {1} episodes.", playlistName, itemIds.Length);
                }
                else
                {
                    await ReplacePlaylistItems(existing, owner, itemIds, cancellationToken).ConfigureAwait(false);
                    _logger.Info("Updated playlist {0} with {1} episodes.", playlistName, itemIds.Length);
                }
            }

            if (rankedSeasons.Count == 0)
            {
                _logger.Warn("No eligible episodes were found for {0}.", series.Name);
            }
        }

        private User ResolveOwner(string configuredUserName)
        {
            if (!string.IsNullOrWhiteSpace(configuredUserName))
            {
                var configured = _userManager.GetUserByName(configuredUserName.Trim());
                if (configured == null)
                {
                    throw new InvalidOperationException(
                        "The configured playlist owner was not found: " + configuredUserName.Trim());
                }

                return configured;
            }

#pragma warning disable CS0618
            var administrator = _userManager.Users.FirstOrDefault(user =>
                _userManager.GetUserPolicy(user).IsAdministrator);
#pragma warning restore CS0618

            if (administrator == null)
            {
                throw new InvalidOperationException("No Emby administrator account was available as playlist owner.");
            }

            return administrator;
        }

        private IReadOnlyList<Series> ResolveSeries(User owner, PluginOptions options)
        {
            var allSeries = _libraryManager.GetItemList(new InternalItemsQuery(owner)
                {
                    IncludeItemTypes = new[] { "Series" },
                    Recursive = true
                })
                .OfType<Series>()
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.InternalId)
                .ToList();

            if (options.ProcessAllSeries)
            {
                if (allSeries.Count == 0)
                {
                    throw new InvalidOperationException("No TV series are visible to the playlist owner.");
                }

                return allSeries;
            }

            var selectedIds = new HashSet<long>(ParseSeriesIds(options.SelectedSeriesIds));
            if (selectedIds.Count > 0)
            {
                var selected = allSeries.Where(item => selectedIds.Contains(item.InternalId)).ToList();
                var missingCount = selectedIds.Count - selected.Count;
                if (missingCount > 0)
                {
                    _logger.Warn("Ignored {0} selected series that are no longer visible in the Emby library.", missingCount);
                }

                if (selected.Count == 0)
                {
                    throw new InvalidOperationException("None of the selected TV series are visible to the playlist owner.");
                }

                return selected;
            }

            if (!string.IsNullOrWhiteSpace(options.TargetSeriesName))
            {
                var legacyMatches = allSeries.Where(item => string.Equals(
                        item.Name,
                        options.TargetSeriesName.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (legacyMatches.Count == 1)
                {
                    return legacyMatches;
                }
            }

            throw new InvalidOperationException("Select at least one TV series in the Best Episodes plugin settings.");
        }

        private IReadOnlyList<Episode> GetEpisodes(User owner, Series series)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery(owner)
                {
                    IncludeItemTypes = new[] { "Episode" },
                    SeriesIds = new[] { series.InternalId },
                    Recursive = true
                })
                .OfType<Episode>()
                .ToList();
        }

        private IEnumerable<Playlist> GetVisiblePlaylists(User owner)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery(owner)
                {
                    IncludeItemTypes = new[] { "Playlist" },
                    Recursive = true,
                    AllowGlobalLists = true
                })
                .OfType<Playlist>();
        }

        private bool IsPlayed(User owner, Episode episode)
        {
            var userData = _userDataManager.GetUserData(owner, episode);
            return userData != null && userData.Played;
        }

        private static EpisodeCandidate ToCandidate(Episode episode)
        {
            return new EpisodeCandidate
            {
                ItemId = episode.InternalId,
                SeasonNumber = episode.ParentIndexNumber ?? 0,
                EpisodeNumber = episode.IndexNumber,
                CommunityRating = episode.CommunityRating,
                Name = episode.Name ?? string.Empty
            };
        }

        private async Task ReplacePlaylistItems(
            Playlist playlist,
            User owner,
            long[] itemIds,
            CancellationToken cancellationToken)
        {
            var currentItems = playlist.GetItemList(new InternalItemsQuery(owner)
            {
                Recursive = true
            });

            var entryIds = currentItems
                .Select(item => item.ListItemEntryId)
                .Where(entryId => entryId > 0)
                .ToArray();

            if (entryIds.Length > 0)
            {
                await _playlistManager.RemoveFromPlaylist(playlist, entryIds).ConfigureAwait(false);
            }

            if (itemIds.Length > 0)
            {
                await _playlistManager.AddToPlaylist(
                    playlist,
                    itemIds,
                    true,
                    owner,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
