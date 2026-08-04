using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Emby.Web.GenericEdit.Common;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace Emby.BestEpisodes
{
    public sealed class Plugin : BasePluginSimpleUI<PluginOptions>
    {
        private static readonly Guid PluginId = new Guid("15C9C024-9067-4AA1-B94A-A9FF35B7A767");
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public Plugin(
            IApplicationHost applicationHost,
            ILogManager logManager,
            ILibraryManager libraryManager)
            : base(applicationHost)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(Name);
            Options = GetOptions();
        }

        public const string PluginName = "Best Episodes";

        public static PluginOptions Options { get; private set; } = new PluginOptions();

        public override string Name => PluginName;

        public override string Description =>
            "Creates highest-rated episode playlists for each season of selected TV series.";

        public override Guid Id => PluginId;

        protected override PluginOptions OnBeforeShowUI(PluginOptions options)
        {
            var current = options ?? new PluginOptions();
            current.SeriesOptions = GetSeriesOptions();
            return current;
        }

        protected override void OnOptionsSaved(PluginOptions options)
        {
            Options = options ?? new PluginOptions();
            _logger.Info("{0} options saved. Run the scheduled task to refresh playlists.", Name);
        }

        private IReadOnlyList<EditorSelectOption> GetSeriesOptions()
        {
            var series = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { "Series" },
                    Recursive = true
                })
                .OfType<Series>()
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.InternalId)
                .ToList();

            var duplicateNames = new HashSet<string>(
                series.GroupBy(item => item.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.OrdinalIgnoreCase);

            return series.Select(item => new EditorSelectOption
                {
                    Value = item.InternalId.ToString(CultureInfo.InvariantCulture),
                    Name = duplicateNames.Contains(item.Name ?? string.Empty)
                        ? string.Format("{0} (library ID {1})", item.Name, item.InternalId)
                        : item.Name,
                    IsEnabled = true
                })
                .ToList();
        }
    }
}
