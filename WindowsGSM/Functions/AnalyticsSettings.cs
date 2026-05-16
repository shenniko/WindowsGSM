using Newtonsoft.Json;
using System;
using System.IO;

namespace WindowsGSM.Functions
{
    public sealed class AnalyticsSettings
    {
        private const int CurrentSchemaVersion = 1;
        private const string DefaultAnalyticsProxyUrl = "https://tight-resonance-b44e.robbie-b6b.workers.dev/";
        private static readonly object LockObject = new object();
        private static AnalyticsSettings _current;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public bool AnalyticsPromptShown { get; set; }
        public bool AnalyticsEnabled { get; set; }
        public string ClientId { get; set; }
        public string AnalyticsProxyUrl { get; set; } = DefaultAnalyticsProxyUrl;

        public static string ConfigPath => Path.Combine(MainWindow.WGSM_PATH, "configs", "Analytics.json");

        public static AnalyticsSettings Current
        {
            get
            {
                lock (LockObject)
                {
                    return _current ?? (_current = Load());
                }
            }
        }

        public static bool IsEnabled => Current.AnalyticsPromptShown
            && Current.AnalyticsEnabled
            && !string.IsNullOrWhiteSpace(Current.AnalyticsProxyUrl);

        public static AnalyticsSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var settings = JsonConvert.DeserializeObject<AnalyticsSettings>(File.ReadAllText(ConfigPath)) ?? new AnalyticsSettings();
                    settings.EnsureDefaults();
                    return settings;
                }
            }
            catch
            {
                // Fall through to a fresh config if the existing file is unreadable.
            }

            var fresh = new AnalyticsSettings();
            fresh.EnsureDefaults();
            return fresh;
        }

        public static void Save(AnalyticsSettings settings)
        {
            if (settings == null) { return; }

            settings.EnsureDefaults();
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(settings, Formatting.Indented));

            lock (LockObject)
            {
                _current = settings;
            }
        }

        public static void SetConsent(bool enabled)
        {
            var settings = Current;
            settings.AnalyticsPromptShown = true;
            settings.AnalyticsEnabled = enabled;
            Save(settings);
        }

        private void EnsureDefaults()
        {
            SchemaVersion = CurrentSchemaVersion;
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                ClientId = Guid.NewGuid().ToString("D");
            }

            AnalyticsProxyUrl = string.IsNullOrWhiteSpace(AnalyticsProxyUrl) ? DefaultAnalyticsProxyUrl : AnalyticsProxyUrl.Trim();
        }
    }
}
