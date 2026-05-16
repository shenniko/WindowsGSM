using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    class GoogleAnalytics
    {
        public const string MeasurementId = "G-T46K9BQDJK";
        private const string AnalyticsSchemaVersion = "1";

        public Task SendAppStart()
        {
            var parameters = new Dictionary<string, object>
            {
                ["app_version"] = MainWindow.WGSM_VERSION,
                ["dotnet_version"] = Environment.Version.ToString(),
                ["analytics_schema_version"] = AnalyticsSchemaVersion
            };

            AddWindowsOsParameters(parameters);
            return SendEventAsync("app_start", parameters);
        }

        public Task SendServerCreated(string game, string pluginName, string installMethod, string steamAppId, string branch)
        {
            return SendEventAsync("server_created", ServerParameters(game, pluginName, steamAppId, branch, new Dictionary<string, object>
            {
                ["install_method"] = installMethod
            }));
        }

        public Task SendServerDeleted(string game, string pluginName)
        {
            return SendEventAsync("server_deleted", ServerParameters(game, pluginName));
        }

        public Task SendServerStarted(string game, string pluginName, string startMethod, string steamAppId, string branch)
        {
            return SendEventAsync("server_started", ServerParameters(game, pluginName, steamAppId, branch, new Dictionary<string, object>
            {
                ["start_method"] = startMethod
            }));
        }

        public Task SendServerStopped(string game, string pluginName, string stopMethod)
        {
            return SendEventAsync("server_stopped", ServerParameters(game, pluginName, extra: new Dictionary<string, object>
            {
                ["stop_method"] = stopMethod
            }));
        }

        public Task SendServerRestarted(string game, string pluginName, string restartMethod, string steamAppId, string branch)
        {
            return SendEventAsync("server_restarted", ServerParameters(game, pluginName, steamAppId, branch, new Dictionary<string, object>
            {
                ["restart_method"] = restartMethod
            }));
        }

        public Task SendSteamCmdInstall(string game, string pluginName, string steamAppId, string branch, string result, string errorCode = null)
        {
            return SendEventAsync("steamcmd_install", ServerParameters(game, pluginName, steamAppId, branch, new Dictionary<string, object>
            {
                ["result"] = result,
                ["error_code"] = errorCode
            }));
        }

        public Task SendSteamCmdUpdate(string game, string pluginName, string steamAppId, string branch, bool validate, string result, string errorCode = null)
        {
            return SendEventAsync("steamcmd_update", ServerParameters(game, pluginName, steamAppId, branch, new Dictionary<string, object>
            {
                ["validate"] = validate,
                ["result"] = result,
                ["error_code"] = errorCode
            }));
        }

        public Task SendPluginInstalled(string pluginName, string pluginVersion, string source)
        {
            return SendEventAsync("plugin_installed", new Dictionary<string, object>
            {
                ["plugin_name"] = pluginName,
                ["plugin_version"] = pluginVersion,
                ["source"] = source
            });
        }

        public Task SendPluginLoadFailed(string pluginName, string errorCode)
        {
            return SendEventAsync("plugin_load_failed", new Dictionary<string, object>
            {
                ["plugin_name"] = pluginName,
                ["error_code"] = errorCode
            });
        }

        public Task SendDiscordCommandUsed(string commandName)
        {
            return SendEventAsync("discord_command_used", new Dictionary<string, object>
            {
                ["command_name"] = commandName
            });
        }

        public Task SendServerCrashed(string game, string pluginName, string exitCode)
        {
            return SendEventAsync("server_crashed", ServerParameters(game, pluginName, extra: new Dictionary<string, object>
            {
                ["exit_code"] = exitCode
            }));
        }

        public Task SendBackupCompleted(string game, string pluginName, string result)
        {
            return SendEventAsync("backup_completed", ServerParameters(game, pluginName, extra: new Dictionary<string, object>
            {
                ["result"] = result
            }));
        }

        public Task SendRestoreCompleted(string game, string pluginName, string result)
        {
            return SendEventAsync("restore_completed", ServerParameters(game, pluginName, extra: new Dictionary<string, object>
            {
                ["result"] = result
            }));
        }

        public Task SendAddonInstalled(string addonName, string game, string result)
        {
            return SendEventAsync("addon_installed", new Dictionary<string, object>
            {
                ["addon_name"] = addonName,
                ["game"] = game,
                ["result"] = result
            });
        }

        public Task SendReadinessCheckCompleted(int passCount, int warningCount, int failCount)
        {
            return SendEventAsync("readiness_check_completed", new Dictionary<string, object>
            {
                ["pass_count"] = passCount,
                ["warning_count"] = warningCount,
                ["fail_count"] = failCount
            });
        }

        public Task SendPluginSearchUsed(int resultCount)
        {
            return SendEventAsync("plugin_search_used", new Dictionary<string, object>
            {
                ["result_count"] = resultCount
            });
        }

        private async Task SendEventAsync(string eventName, Dictionary<string, object> parameters)
        {
            if (!AnalyticsSettings.IsEnabled) { return; }

            try
            {
                var settings = AnalyticsSettings.Current;
                var eventParams = new JObject
                {
                    ["measurement_id"] = MeasurementId,
                    ["engagement_time_msec"] = 1,
                    ["schema_version"] = AnalyticsSchemaVersion
                };

                foreach (var parameter in SanitizeParameters(parameters))
                {
                    eventParams[parameter.Key] = JToken.FromObject(parameter.Value);
                }

                var payload = new JObject
                {
                    ["client_id"] = settings.ClientId,
                    ["events"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = eventName,
                            ["params"] = eventParams
                        }
                    }
                };

                using (var response = await Http.PostJsonAsync(settings.AnalyticsProxyUrl, payload.ToString(Formatting.None)))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine((int)response.StatusCode + " analytics proxy did not return success");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static Dictionary<string, object> ServerParameters(string game, string pluginName, string steamAppId = null, string branch = null, Dictionary<string, object> extra = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["game"] = game,
                ["plugin_name"] = pluginName,
                ["steam_app_id"] = steamAppId,
                ["steam_branch"] = string.IsNullOrWhiteSpace(branch) ? "public" : branch
            };

            if (extra != null)
            {
                foreach (var item in extra)
                {
                    parameters[item.Key] = item.Value;
                }
            }

            return parameters;
        }

        private static Dictionary<string, object> SanitizeParameters(Dictionary<string, object> parameters)
        {
            return (parameters ?? new Dictionary<string, object>())
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key) && parameter.Value != null)
                .ToDictionary(
                    parameter => parameter.Key,
                    parameter => parameter.Value is string value ? SanitizeValue(value) : parameter.Value);
        }

        private static string SanitizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }

            value = value.Trim();
            return value.Length <= 100 ? value : value.Substring(0, 100);
        }

        private static void AddWindowsOsParameters(Dictionary<string, object> parameters)
        {
            try
            {
                string osBit = string.Empty;
                using (var searcher = new ManagementObjectSearcher("SELECT OSArchitecture FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        osBit = obj["OSArchitecture"]?.ToString() ?? string.Empty;
                    }
                }

                string osName = new Microsoft.VisualBasic.Devices.ComputerInfo().OSFullName;
                parameters["os_version"] = string.IsNullOrWhiteSpace(osBit) ? osName : $"{osName} - {osBit}";
            }
            catch
            {
                parameters["os_version"] = Environment.OSVersion.VersionString;
            }
        }
    }
}
