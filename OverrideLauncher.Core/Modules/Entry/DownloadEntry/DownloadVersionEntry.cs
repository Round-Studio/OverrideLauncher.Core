using System;
using System.Collections.Generic;

namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry
{
    public class DownloadVersionEntry
    {
        public LatestVersionInfo Latest { get; set; }
        public List<GameVersion> Versions { get; set; }
    }

    public class LatestVersionInfo
    {
        public string Release { get; set; }
        public string Snapshot { get; set; }
    }

    public class GameVersion
    {
        public string Id { get; set; }
        public string Type { get; set; } // "release" or "snapshot"
        public string Url { get; set; }
        public DateTime Time { get; set; }
        public DateTime ReleaseTime { get; set; }
    }
}