using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ViperaSecurityInstaller
{
    public partial class InstallerViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _currentStep = 1; // 1: Welcome, 2: Options, 3: Installing, 4: Finish

        [ObservableProperty]
        private string _installDirectory = string.Empty;

        [ObservableProperty]
        private bool _createDesktopShortcut = true;

        [ObservableProperty]
        private bool _createStartMenuShortcut = true;

        [ObservableProperty]
        private bool _launchOnStartup = true;

        [ObservableProperty]
        private bool _launchAppAfterInstall = true;

        [ObservableProperty]
        private bool _acceptedLicense = true;

        [ObservableProperty]
        private double _installProgress;

        [ObservableProperty]
        private string _statusMessage = "Ready to install Vipera Security.";

        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _isFinished;

        [ObservableProperty]
        private bool _showElevationButton;

        [ObservableProperty]
        private bool _isPerUserInstall;

        public InstallerViewModel()
        {
            try
            {
                if (IsAdministrator())
                {
                    string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    if (string.IsNullOrEmpty(pf)) pf = @"C:\Program Files";
                    InstallDirectory = Path.Combine(pf, "Vipera Security");
                    IsPerUserInstall = false;
                }
                else
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    if (string.IsNullOrEmpty(localAppData)) localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local");
                    InstallDirectory = Path.Combine(localAppData, "Programs", "Vipera Security");
                    IsPerUserInstall = true;
                }
            }
            catch
            {
                InstallDirectory = @"C:\ViperaSecurity";
                IsPerUserInstall = true;
            }
        }

        public static bool IsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        [RelayCommand]
        public void SwitchToPerUserInstall()
        {
            InstallDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Vipera Security");
            IsPerUserInstall = true;
            ShowElevationButton = false;
        }

        [RelayCommand]
        public void RestartAsAdmin()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch { }
        }

        [RelayCommand]
        public void NextStep()
        {
            if (CurrentStep == 1 && AcceptedLicense)
            {
                CurrentStep = 2;
            }
            else if (CurrentStep == 2)
            {
                CurrentStep = 3;
                _ = RunInstallationAsync();
            }
            else if (CurrentStep == 4)
            {
                if (LaunchAppAfterInstall)
                {
                    string targetExe = Path.Combine(InstallDirectory, "ViperaSecurity.exe");
                    if (File.Exists(targetExe))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = targetExe,
                            WorkingDirectory = InstallDirectory,
                            UseShellExecute = true
                        });
                    }
                }
                System.Windows.Application.Current.Shutdown();
            }
        }

        [RelayCommand]
        public void PreviousStep()
        {
            if (CurrentStep > 1 && CurrentStep < 3)
            {
                CurrentStep--;
            }
        }

        private async Task RunInstallationAsync()
        {
            IsInstalling = true;
            InstallProgress = 0;
            StatusMessage = "Initializing installation target...";

            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(InstallDirectory))
                    {
                        Directory.CreateDirectory(InstallDirectory);
                    }

                    // Test write permission
                    string testFile = Path.Combine(InstallDirectory, ".perm_test");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);

                    StatusMessage = "Preparing Vipera Security threat engine payload...";
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string localZip = Path.Combine(baseDir, "ViperaPayload.zip");
                    string tempZip = Path.Combine(Path.GetTempPath(), "ViperaPayload_Install.zip");

                    if (File.Exists(localZip))
                    {
                        tempZip = localZip;
                    }
                    else
                    {
                        using var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ViperaSecurityInstaller.ViperaPayload.zip");
                        if (resStream != null)
                        {
                            using var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write);
                            resStream.CopyTo(fs);
                        }
                        else
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusMessage = "Downloading threat engine payload from server...";
                            });

                            using var http = new System.Net.Http.HttpClient();
                            http.DefaultRequestHeaders.UserAgent.ParseAdd("ViperaInstaller/2.0");
                            string downloadUrl = "https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip";

                            using (var resp = http.GetAsync(downloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                            {
                                resp.EnsureSuccessStatusCode();
                                var totalBytes = resp.Content.Headers.ContentLength ?? 75000000;
                                using var netStream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                                using var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                                byte[] buffer = new byte[16384];
                                long totalRead = 0;
                                int bytesRead;
                                while ((bytesRead = netStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                    totalRead += bytesRead;
                                    double pct = ((double)totalRead / totalBytes) * 40.0;
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        InstallProgress = pct;
                                        StatusMessage = $"Downloading payload: {totalRead / 1048576} MB / {totalBytes / 1048576} MB";
                                    });
                                }
                            }
                        }
                    }

                    if (File.Exists(tempZip))
                    {
                        using var archive = ZipFile.OpenRead(tempZip);
                        int totalEntries = archive.Entries.Count;

                        for (int i = 0; i < totalEntries; i++)
                        {
                            var entry = archive.Entries[i];
                            if (string.IsNullOrEmpty(entry.Name)) continue;

                            string destFile = Path.Combine(InstallDirectory, entry.FullName);
                            string? destDir = Path.GetDirectoryName(destFile);

                            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                            }

                            entry.ExtractToFile(destFile, overwrite: true);

                            double progress = 40.0 + (((i + 1) / (double)totalEntries) * 45.0);
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                InstallProgress = progress;
                                StatusMessage = $"Extracting: {entry.Name}";
                            });
                        }
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        InstallProgress = 85;
                        StatusMessage = "Registering in Windows Control Panel...";
                    });
                    RegisterAddRemovePrograms();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        InstallProgress = 95;
                        StatusMessage = "Creating Desktop & Start Menu shortcuts...";
                    });
                    CreateShortcuts();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        InstallProgress = 100;
                        StatusMessage = "Installation completed successfully!";
                        IsInstalling = false;
                        IsFinished = true;
                        CurrentStep = 4;
                    });
                }
                catch (UnauthorizedAccessException)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Permission Restriction: Writing to '{InstallDirectory}' requires Administrator privileges.\n\nChoose an option below:";
                        ShowElevationButton = true;
                        IsInstalling = false;
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Installation Error: {ex.Message}";
                        IsInstalling = false;
                    });
                }
            });
        }

        private void RegisterAddRemovePrograms()
        {
            try
            {
                string targetExe = Path.Combine(InstallDirectory, "ViperaSecurity.exe");
                string uninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Vipera Security";
                var rootKey = (!IsPerUserInstall && IsAdministrator()) ? Registry.LocalMachine : Registry.CurrentUser;
                using var key = rootKey.CreateSubKey(uninstallKeyPath);
                if (key != null)
                {
                    key.SetValue("DisplayName", "Vipera Security");
                    key.SetValue("DisplayVersion", "2.0.0");
                    key.SetValue("Publisher", "Vipera Security");
                    key.SetValue("InstallLocation", InstallDirectory);
                    key.SetValue("DisplayIcon", targetExe);
                    key.SetValue("UninstallString", $"cmd.exe /c rmdir /s /q \"{InstallDirectory}\"");
                }
            }
            catch { }
        }

        private void CreateShortcuts()
        {
            try
            {
                string targetExe = Path.Combine(InstallDirectory, "ViperaSecurity.exe");

                if (CreateDesktopShortcut)
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string shortcutPath = Path.Combine(desktopPath, "Vipera Security.lnk");
                    CreateWshShortcut(shortcutPath, targetExe, InstallDirectory);
                }

                if (CreateStartMenuShortcut)
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Vipera Security");
                    if (!Directory.Exists(startMenu)) Directory.CreateDirectory(startMenu);
                    string shortcutPath = Path.Combine(startMenu, "Vipera Security.lnk");
                    CreateWshShortcut(shortcutPath, targetExe, InstallDirectory);
                }
            }
            catch { }
        }

        private static void CreateWshShortcut(string shortcutPath, string targetExe, string workingDir)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    if (shell != null)
                    {
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = targetExe;
                        shortcut.WorkingDirectory = workingDir;
                        shortcut.Description = "Vipera Security Antivirus & Real-Time Protection";
                        shortcut.Save();
                    }
                }
            }
            catch { }
        }
    }
}
