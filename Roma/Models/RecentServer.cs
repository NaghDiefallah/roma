using System;

namespace Roma.Models
{
    public class RecentServer
    {
        public string Ip { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime LastConnected { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public string AddressText => string.IsNullOrEmpty(Port) ? Ip : $"{Ip}:{Port}";
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - LastConnected;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
                return LastConnected.ToShortDateString();
            }
        }
    }
}
