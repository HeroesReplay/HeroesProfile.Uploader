namespace Heroesprofile.Uploader.Windows
{
    public class AppConfig
    {
        public bool UpgradeRequired { get; set; }
        public bool AutoUpdate { get; set; }
        public string UpdateRepository { get; set; }
        public int WindowTop { get; set; }
        public int WindowLeft { get; set; }
        public bool MinimizeToTray { get; set; }
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }
        public string DeleteAfterUpload { get; set; }
        public string Theme { get; set; }
        public bool AllowPreReleases { get; set; }
        public string ApplicationVersion { get; set; }
        public bool PreMatchPage { get; set; }
        public bool PostMatchPage { get; set; }

    }
}
