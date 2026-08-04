using System;
using System.Collections.Generic;
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
        private readonly ILibraryManager _libraryManager;
        private readonly IPlaylistManager _playlistManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public BestEpisodesService(
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager,
            IUserManager userManager,
            ILogger logger)
        {
            _libraryManager = libraryManager;
            _playlistManager = playlistManager;
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

            if (string.IsNullOrWhiteSpace(options.TargetSeriesName))
            {
                throw new InvalidOperationException("A target series name must be configured.");
            }

            var owner = ResolveOwner(options.OwnerUserName);
            var series = ResolveSeries(owner, options.TargetSeriesName.Trim());
            var episodes = GetEpisodes(owner, series);

            var rankedSeasons = EpisodeRanker.RankBySeason(
                episodes.Select(ToCandidate),
                options.TopEpisodesPerSeason,
                options.MinimumRating,
                options.IncludeUnratedEpisodes,
                options.IncludeSpecials);

            if (rankedSeasons.Count == 0)
            {
                _logger.Warn(
                    "No eligible episodes with community ratings were found for {0}. Existing playlists were left unchanged.",
                    series.Name);
                progress?.Report(100);
                return;
            }

            var playlists = GetVisiblePlaylists(owner).ToList();
            var completed = 0;

            foreach (var season in rankedSeasons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var playlistName = BuildPlaylistName(options.PlaylistPrefix, series.Name, season.Key);
                var itemIds = season.Value.Select(item => item.ItemId).ToArray();
                var existing = playlists.FirstOrDefault(item =>
                    string.Equals(item.Name, playlistName, StringComparison.OrdinalIgnoreCase));

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

                completed++;
                progress?.Report(completed * 100d / rankedSeasons.Count);
            }
        }

        internal static string BuildPlaylistName(string prefix, string seriesName, int seasonNumber)
        {
            var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "Best Rated" : prefix.Trim();
            var seasonLabel = seasonNumber == 0 ? "Specials" : "Season " + seasonNumber;
            return string.Format("{0} - {1} - {2}", safePrefix, seriesName, seasonLabel);
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

        private Series ResolveSeries(User owner, string exactName)
        {
            var matches = _libraryManager.GetItemList(new InternalItemsQuery(owner)
                {
                    IncludeItemTypes = new[] { "Series" },
                    Recursive = true
                })
                .OfType<Series>()
                .Where(item => string.Equals(item.Name, exactName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                throw new InvalidOperationException("Series not found in Emby: " + exactName);
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    "More than one Emby series has the exact title '" + exactName + "'. Rename one or use a unique title.");
            }

            return matches[0];
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

