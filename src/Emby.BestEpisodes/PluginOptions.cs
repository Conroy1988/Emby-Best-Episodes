using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using MediaBrowser.Model.Attributes;

namespace Emby.BestEpisodes
{
    public enum EpisodeLimit
    {
        [Description("All eligible episodes")]
        All = 0,

        [Description("Top 5")]
        Top5 = 5,

        [Description("Top 10")]
        Top10 = 10,

        [Description("Top 20")]
        Top20 = 20
    }

    public sealed class PluginOptions : EditableOptionsBase
    {
        public PluginOptions()
        {
            SelectedSeriesIds = string.Empty;
            ProcessAllSeries = false;
            OwnerUserName = string.Empty;
            PlaylistPrefix = "Best Rated";
            EpisodesPerSeason = EpisodeLimit.Top10;
            MinimumRating = 0;
            IncludeUnratedEpisodes = false;
            IncludeSpecials = false;
            ExcludeWatchedEpisodes = false;
            AutoRefreshAfterLibraryScan = true;
            CreatePublicPlaylists = true;
            SeriesOptions = Array.Empty<EditorSelectOption>();

            // Retained so an existing 0.1 installation keeps working until a library
            // selection is saved in the new UI.
            TargetSeriesName = "Ancient Aliens: Origins";
        }

        public override string EditorTitle => "Best Episodes";

        public override string EditorDescription =>
            "Build per-season playlists with the highest community-rated episodes first.";

        [Browsable(false)]
        [XmlIgnore]
        public IEnumerable<EditorSelectOption> SeriesOptions { get; set; }

        [DisplayName("TV series")]
        [Description("Choose one or more shows from your Emby library. This is ignored when Process all series is enabled.")]
        [EditMultilSelect]
        [SelectItemsSource(nameof(SeriesOptions))]
        public string SelectedSeriesIds { get; set; }

        [DisplayName("Process all series")]
        [Description("Create best-rated season playlists for every TV series visible to the playlist owner.")]
        public bool ProcessAllSeries { get; set; }

        [DisplayName("Playlist owner username")]
        [Description("Leave blank to use the first Emby administrator account. Watched-state filtering uses this account.")]
        public string OwnerUserName { get; set; }

        [DisplayName("Playlist name prefix")]
        [Description("Only playlists beginning with this generated prefix are managed by the plugin.")]
        public string PlaylistPrefix { get; set; }

        [DisplayName("Episodes per season")]
        [Description("Choose the maximum number of highest-rated episodes in each season playlist.")]
        public EpisodeLimit EpisodesPerSeason { get; set; }

        [DisplayName("Minimum community rating")]
        [Description("Episodes below this value are omitted. Use 0 to accept every rated episode.")]
        public double MinimumRating { get; set; }

        [DisplayName("Include unrated episodes")]
        [Description("When enabled, unrated episodes appear after all rated episodes.")]
        public bool IncludeUnratedEpisodes { get; set; }

        [DisplayName("Include specials")]
        [Description("When enabled, season 0 is given its own best-rated playlist.")]
        public bool IncludeSpecials { get; set; }

        [DisplayName("Exclude watched episodes")]
        [Description("Remove episodes already marked played by the playlist owner.")]
        public bool ExcludeWatchedEpisodes { get; set; }

        [DisplayName("Refresh after every library scan")]
        [Description("Automatically update generated playlists after Emby finishes scanning the library.")]
        public bool AutoRefreshAfterLibraryScan { get; set; }

        [DisplayName("Make generated playlists public")]
        public bool CreatePublicPlaylists { get; set; }

        [Browsable(false)]
        public string TargetSeriesName { get; set; }
    }
}
