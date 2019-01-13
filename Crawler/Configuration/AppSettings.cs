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

        public bool ShowHelp
        {
            get { return _args.ToList().Any(s => _helpSwitches.Contains(s.ToLowerInvariant()) ); }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Url);
            }
        }
    }
}
