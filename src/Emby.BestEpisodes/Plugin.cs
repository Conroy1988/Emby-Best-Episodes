using System;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace Emby.BestEpisodes
{
    public sealed class Plugin : BasePluginSimpleUI<PluginOptions>
    {
        private static readonly Guid PluginId = new Guid("15C9C024-9067-4AA1-B94A-A9FF35B7A767");
        private readonly ILogger _logger;

        public Plugin(IApplicationHost applicationHost, ILogManager logManager)
            : base(applicationHost)
        {
            _logger = logManager.GetLogger(Name);
            Options = GetOptions();
        }

        public const string PluginName = "Best Episodes";

        public static PluginOptions Options { get; private set; } = new PluginOptions();

        public override string Name => PluginName;

        public override string Description =>
            "Creates a highest-rated episode playlist for each season of a configured TV series.";

        public override Guid Id => PluginId;

        protected override void OnOptionsSaved(PluginOptions options)
        {
            Options = options ?? new PluginOptions();
            _logger.Info("{0} options saved. Run the scheduled task to refresh playlists.", Name);
        }
    }
}
