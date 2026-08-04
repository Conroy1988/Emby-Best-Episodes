using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Logging;

namespace Emby.BestEpisodes
{
    public sealed class RefreshBestEpisodesPostScanTask : ILibraryPostScanTask
    {
        private readonly BestEpisodesService _service;
        private readonly ILogger _logger;

        public RefreshBestEpisodesPostScanTask(
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager,
            IUserDataManager userDataManager,
            IUserManager userManager,
            ILogManager logManager)
        {
            _logger = logManager.GetLogger(Plugin.PluginName);
            _service = new BestEpisodesService(
                libraryManager,
                playlistManager,
                userDataManager,
                userManager,
                _logger);
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (!Plugin.Options.AutoRefreshAfterLibraryScan)
            {
                progress?.Report(100);
                return;
            }

            try
            {
                await _service.RefreshAsync(Plugin.Options, cancellationToken, progress).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.Warn(
                    "Automatic Best Episodes refresh was skipped: {0}",
                    exception.Message);
                progress?.Report(100);
            }
        }
    }
}
