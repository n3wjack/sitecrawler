using System.Linq;

namespace Crawler.Configuration
{
    public class AppSettings
    {
        private string[] _args;
        private string[]_helpSwitches = new string[] { "-h", "/?", "--help", "-help", "-?" };

        public AppSettings(string[] args)
        {
            _args = args;
        }

        public string OutputFile { get; set; }
        public string Url { get; set; }
        public int Minutes { get; set; }
        public int ParallelTasks { get; set; } = 10;
        public int RequestDelay { get; set; }
        public bool ShowHelp => _args.ToList().Any(s => _helpSwitches.Contains(s.ToLowerInvariant()));
        public bool IsValid => !string.IsNullOrWhiteSpace(Url);
    }
}
