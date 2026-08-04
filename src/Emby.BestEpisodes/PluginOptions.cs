using System.ComponentModel;
using Emby.Web.GenericEdit;

namespace Emby.BestEpisodes
{
    public sealed class PluginOptions : EditableOptionsBase
    {
        public PluginOptions()
        {
            TargetSeriesName = "Ancient Aliens: Origins";
            OwnerUserName = string.Empty;
            PlaylistPrefix = "Best Rated";
            TopEpisodesPerSeason = 10;
            MinimumRating = 0;
            IncludeUnratedEpisodes = false;
            IncludeSpecials = false;
            CreatePublicPlaylists = true;
        }

        public override string EditorTitle => "Best Episodes";

        public override string EditorDescription =>
            "Build per-season playlists with the highest community-rated episodes first.";

        [DisplayName("Series name")]
        [Description("Exact Emby library title. The first test target is Ancient Aliens: Origins.")]
        public string TargetSeriesName { get; set; }

        [DisplayName("Playlist owner username")]
        [Description("Leave blank to use the first Emby administrator account.")]
        public string OwnerUserName { get; set; }

        [DisplayName("Playlist name prefix")]
        [Description("Only playlists beginning with this generated prefix are managed by the plugin.")]
        public string PlaylistPrefix { get; set; }

        [DisplayName("Episodes per season")]
        [Description("Maximum number of highest-rated episodes placed in each season playlist.")]
        public int TopEpisodesPerSeason { get; set; }

        [DisplayName("Minimum community rating")]
        [Description("Episodes below this value are omitted. Use 0 to accept every rated episode.")]
        public double MinimumRating { get; set; }

        [DisplayName("Include unrated episodes")]
        [Description("When enabled, unrated episodes appear after all rated episodes.")]
        public bool IncludeUnratedEpisodes { get; set; }

        [DisplayName("Include specials")]
        [Description("When enabled, season 0 is given its own best-rated playlist.")]
        public bool IncludeSpecials { get; set; }

        [DisplayName("Make generated playlists public")]
        public bool CreatePublicPlaylists { get; set; }
    }
}

