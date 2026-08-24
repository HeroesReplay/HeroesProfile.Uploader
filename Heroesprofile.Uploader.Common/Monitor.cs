using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroesprofile.Uploader.Common
{
    public class Monitor : IMonitor
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        protected string ProfilePath { get { return ReplayLocation.Current; } }
        protected FileSystemWatcher _watcher;
        private string _watchedPath;

        /// <summary>
        /// Fires when a new replay file is found
        /// </summary>
        public event EventHandler<EventArgs<string>> ReplayAdded;
        protected virtual void OnReplayAdded(object source, FileSystemEventArgs e)
        {
            _log.Debug($"Detected new replay: {e.FullPath}");
            ReplayAdded?.Invoke(this, new EventArgs<string>(e.FullPath));
        }

        /// <summary>
        /// Starts watching filesystem for new replays. When found raises <see cref="ReplayAdded"/> event.
        /// </summary>
        public void Start()
        {
            if (_watcher != null && _watchedPath != ProfilePath) {
                // replay folder was changed in settings, rebuild the watcher around the new one
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnReplayAdded;
                _watcher.Dispose();
                _watcher = null;
            }
            if (_watcher == null) {
                try {
                    _watcher = new FileSystemWatcher() {
                        Path = ProfilePath,
                        Filter = "*.StormReplay",
                        IncludeSubdirectories = true
                    };
                    _watcher.Created += OnReplayAdded;
                    _watchedPath = ProfilePath;
                }
                catch (Exception ex) {
                    _log.Error(ex, $"Failed to watch replay directory: {ProfilePath}");
                    _watcher = null;
                    return;
                }
            }
            _watcher.EnableRaisingEvents = true;
            _log.Debug($"Started watching for new replays in {ProfilePath}");
        }

        /// <summary>
        /// Stops watching filesystem for new replays
        /// </summary>
        public void Stop()
        {
            if (_watcher != null) {
                _watcher.EnableRaisingEvents = false;
            }
            _log.Debug($"Stopped watching for new replays");
        }

        /// <summary>
        /// Finds all available replay files
        /// </summary>
        public IEnumerable<string> ScanReplays()
        {
            try {
                return Directory.GetFiles(ProfilePath, "*.StormReplay", SearchOption.AllDirectories);
            }
            catch (Exception ex) {
                // an unreadable folder must not take the app down, the user can point us at another one
                _log.Error(ex, $"Failed to scan replays in directory: {ProfilePath}");
                return new string[0];
            }
        }
    }
}
