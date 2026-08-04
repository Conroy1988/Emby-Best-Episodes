using System.Collections.Generic;
using System.Linq;
using Emby.BestEpisodes.Ranking;
using Xunit;

namespace Emby.BestEpisodes.Tests
{
    public sealed class EpisodeRankerTests
    {
        [Fact]
        public void SortsHighestRatingFirstWithinEachSeason()
        {
            var result = Rank(
                Episode(1, 1, 1, 6.2f),
                Episode(2, 1, 2, 9.1f),
                Episode(3, 1, 3, 8.4f));

            Assert.Equal(new long[] { 2, 3, 1 }, result[1].Select(item => item.ItemId));
        }

        [Fact]
        public void UsesEpisodeNumberAsStableTieBreaker()
        {
            var result = Rank(
                Episode(10, 2, 8, 8.5f),
                Episode(11, 2, 3, 8.5f));

            Assert.Equal(new long[] { 11, 10 }, result[2].Select(item => item.ItemId));
        }

        [Fact]
        public void AppliesLimitAndMinimumRating()
        {
            var result = EpisodeRanker.RankBySeason(
                new[]
                {
                    Episode(1, 1, 1, 9.0f),
                    Episode(2, 1, 2, 8.0f),
                    Episode(3, 1, 3, 7.0f)
                },
                2,
                7.5,
                false,
                false);

            Assert.Equal(new long[] { 1, 2 }, result[1].Select(item => item.ItemId));
        }

        [Fact]
        public void ExcludesUnratedEpisodesByDefault()
        {
            var result = Rank(
                Episode(1, 1, 1, null),
                Episode(2, 1, 2, 7.0f));

            Assert.Single(result[1]);
            Assert.Equal(2, result[1][0].ItemId);
        }

        [Fact]
        public void PlacesIncludedUnratedEpisodesLast()
        {
            var result = EpisodeRanker.RankBySeason(
                new[]
                {
                    Episode(1, 1, 1, null),
                    Episode(2, 1, 2, 7.0f)
                },
                10,
                0,
                true,
                false);

            Assert.Equal(new long[] { 2, 1 }, result[1].Select(item => item.ItemId));
        }

        [Fact]
        public void ExcludesSpecialsUnlessEnabled()
        {
            var episodes = new[]
            {
                Episode(1, 0, 1, 9.0f),
                Episode(2, 1, 1, 8.0f)
            };

            var withoutSpecials = EpisodeRanker.RankBySeason(episodes, 10, 0, false, false);
            var withSpecials = EpisodeRanker.RankBySeason(episodes, 10, 0, false, true);

            Assert.False(withoutSpecials.ContainsKey(0));
            Assert.True(withSpecials.ContainsKey(0));
        }

        private static IReadOnlyDictionary<int, IReadOnlyList<EpisodeCandidate>> Rank(
            params EpisodeCandidate[] episodes)
        {
            return EpisodeRanker.RankBySeason(episodes, 10, 0, false, false);
        }

        private static EpisodeCandidate Episode(
            long itemId,
            int season,
            int episode,
            float? rating)
        {
            return new EpisodeCandidate
            {
                ItemId = itemId,
                SeasonNumber = season,
                EpisodeNumber = episode,
                CommunityRating = rating,
                Name = "Episode " + episode
            };
        }
    }
}

