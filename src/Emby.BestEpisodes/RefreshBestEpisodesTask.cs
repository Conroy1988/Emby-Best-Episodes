using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace Emby.BestEpisodes
{
    public sealed class RefreshBestEpisodesTask : IScheduledTask
    {
        private readonly BestEpisodesService _service;

        public RefreshBestEpisodesTask(
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager,
            IUserDataManager userDataManager,
            IUserManager userManager,
            ILogManager logManager)
        {
            _service = new BestEpisodesService(
                libraryManager,
                playlistManager,
                userDataManager,
                userManager,
                logManager.GetLogger(Plugin.PluginName));
        }

        public string Name => "Refresh best-rated episode playlists";

        public string Description =>
            "Creates or refreshes community-rating-sorted season playlists for the selected TV series.";

        public string Category => Plugin.PluginName;

        public string Key => "BestEpisodes.RefreshPlaylists";

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return _service.RefreshAsync(Plugin.Options, cancellationToken, progress);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
                }
            };
        }
    }
}
