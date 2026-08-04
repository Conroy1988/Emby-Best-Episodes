namespace Emby.BestEpisodes.Ranking
{
    public sealed class EpisodeCandidate
    {
        public long ItemId { get; set; }

        public int SeasonNumber { get; set; }

        public int? EpisodeNumber { get; set; }

        public float? CommunityRating { get; set; }

        public string Name { get; set; }
    }
}

