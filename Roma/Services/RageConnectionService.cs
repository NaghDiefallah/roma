using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace Roma.Services
{
    public class RageConnectionService
    {
        private const string RegistryPath = @"Software\RAGE-MP";
        private const string DefaultRageMpPath = @"C:\RAGEMP";
        private const string UpdaterExecutable = "updater.exe";
        private const string RageMpExecutable = "RAGE Multiplayer.exe";
        private string _customPath = string.Empty;
        private string _connectionMethod = "Rage";

        public void SetCustomPath(string path)
        {
            _customPath = path;
        }

        public void SetConnectionMethod(string method)
        {
            _connectionMethod = method;
        }

        public bool ConnectToServer(string ip, string port)
        {
            if (_connectionMethod == "Protocol")
            {
                return ConnectViaProtocol(ip, port);
            }
            else
            {
                return ConnectViaUpdater(ip, port);
            }
        }

        private bool ConnectViaProtocol(string ip, string port)
        {
            try
            {
                var connectionUrl = $"rage://v/connect?ip={ip}:{port}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = connectionUrl,
                    UseShellExecute = true
                };

                var process = Process.Start(startInfo);
                return process != null;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.WriteLine($"Protocol failed: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection failed: {ex.Message}");
                return false;
            }
        }

        private bool ConnectViaUpdater(string ip, string port)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                Debug.WriteLine($"[~] Connecting: {ip}:{port}");

                // Get RAGE-MP path
                string rageMpPath = "";
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                    {
                        if (key != null)
                        {
                            rageMpPath = key.GetValue("rage_mp_path") as string;
                        }
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(rageMpPath) || !Directory.Exists(rageMpPath))
                {
                    rageMpPath = !string.IsNullOrEmpty(_customPath) && Directory.Exists(_customPath) 
                        ? _customPath 
                        : DefaultRageMpPath;
                }

                if (!Directory.Exists(rageMpPath))
                {
                    Debug.WriteLine($"[!] RAGE-MP not found");
                    return false;
                }

                string updaterPath = Path.Combine(rageMpPath, UpdaterExecutable);
                if (!File.Exists(updaterPath))
                {
                    Debug.WriteLine($"[!] updater.exe not found");
                    return false;
                }

                // OPTIMIZED PowerShell (this was working at ~300-400ms)
                string psCommand = $"Set-ItemProperty -Path 'HKCU:\\Software\\RAGE-MP' -Name 'launch2.ip' -Value '{ip}';" +
                                   $"Set-ItemProperty -Path 'HKCU:\\Software\\RAGE-MP' -Name 'launch2.port' -Value '{port}';" +
                                   $"Start-Process '{updaterPath}' -WorkingDirectory '{rageMpPath}'";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);

                sw.Stop();
                Debug.WriteLine($"[+] Done in {sw.ElapsedMilliseconds}ms");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[!] Error: {ex.Message}");
                return false;
            }
        }

        public string GetRageMpPath()
        {
            // Priority: Custom path > Registry > Default
            if (!string.IsNullOrEmpty(_customPath) && Directory.Exists(_customPath))
            {
                return _customPath;
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                var path = key?.GetValue("rage_mp_path") as string;
                return string.IsNullOrEmpty(path) ? DefaultRageMpPath : path;
            }
            catch
            {
                return DefaultRageMpPath;
            }
        }

        public bool IsRageMpInstalled()
        {
            var path = GetRageMpPath();
            var updaterPath = Path.Combine(path, UpdaterExecutable);
            return File.Exists(updaterPath);
        }
    }
}
