using System.Collections.Generic;
using System.Linq;

namespace Emby.BestEpisodes.Ranking
{
    public static class EpisodeRanker
    {
        public static IReadOnlyDictionary<int, IReadOnlyList<EpisodeCandidate>> RankBySeason(
            IEnumerable<EpisodeCandidate> episodes,
            int maximumPerSeason,
            double minimumRating,
            bool includeUnrated,
            bool includeSpecials)
        {
            var limit = maximumPerSeason == 0
                ? int.MaxValue
                : maximumPerSeason < 0 ? 1 : maximumPerSeason;
            var floor = minimumRating < 0 ? 0 : minimumRating > 10 ? 10 : minimumRating;

            return episodes
                .Where(item => includeSpecials || item.SeasonNumber > 0)
                .Where(item => includeUnrated || item.CommunityRating.HasValue)
                .Where(item => !item.CommunityRating.HasValue || item.CommunityRating.Value >= floor)
                .GroupBy(item => item.SeasonNumber)
                .OrderBy(group => group.Key)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<EpisodeCandidate>)group
                        .OrderByDescending(item => item.CommunityRating.HasValue)
                        .ThenByDescending(item => item.CommunityRating ?? float.MinValue)
                        .ThenBy(item => item.EpisodeNumber ?? int.MaxValue)
                        .ThenBy(item => item.Name)
                        .Take(limit)
                        .ToList());
        }
    }
}
