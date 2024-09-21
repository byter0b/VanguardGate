using System.ServiceProcess;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace VanguardGate;

public class TrayApplicationContext : ApplicationContext
{
    private readonly IConfiguration _configuration;
    private readonly string _vgTrayExePath;
    private readonly int _checkIntervalSeconds;
    private readonly List<GameConfig> _games;

    private NotifyIcon trayIcon;
    private System.Windows.Forms.Timer timer;
    private int gameNotRunningCount = 0;

    private const string VGCServiceName = "vgc";

    // Current toggle mode
    private ToggleMode _currentToggleMode;

    // Context Menu Items
    private ToolStripMenuItem autoMenuItem;
    private ToolStripMenuItem onMenuItem;
    private ToolStripMenuItem offMenuItem;

    public TrayApplicationContext()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        _configuration = builder.Build();

        _vgTrayExePath = _configuration["VgTray:ExePath"];
        if (int.TryParse(_configuration["CheckIntervalSeconds"], out int interval))
        {
            _checkIntervalSeconds = interval;
        }
        else
        {
            _checkIntervalSeconds = 15; // Default to 15 seconds
        }

        // Read games configuration
        _games = _configuration.GetSection("Games").Get<List<GameConfig>>() ?? new List<GameConfig>();

        // Read toggle mode
        string toggleModeStr = _configuration["Toggle:Mode"];
        if (Enum.TryParse(toggleModeStr, true, out ToggleMode mode))
        {
            _currentToggleMode = mode;
        }
        else
        {
            _currentToggleMode = ToggleMode.Auto; // Default to Auto
        }

        // Initialize Context Menu
        var contextMenuStrip = new ContextMenuStrip();

        // Toggle Menu Items
        autoMenuItem = new ToolStripMenuItem("Auto");
        autoMenuItem.CheckOnClick = true;
        autoMenuItem.Checked = _currentToggleMode == ToggleMode.Auto;
        autoMenuItem.Click += (s, e) => SetToggleMode(ToggleMode.Auto);

        onMenuItem = new ToolStripMenuItem("Manual On");
        onMenuItem.CheckOnClick = true;
        onMenuItem.Checked = _currentToggleMode == ToggleMode.On;
        onMenuItem.Click += (s, e) => SetToggleMode(ToggleMode.On);

        offMenuItem = new ToolStripMenuItem("Manual Off");
        offMenuItem.CheckOnClick = true;
        offMenuItem.Checked = _currentToggleMode == ToggleMode.Off;
        offMenuItem.Click += (s, e) => SetToggleMode(ToggleMode.Off);

        // Ensure only one item is checked at a time
        contextMenuStrip.Items.AddRange(new ToolStripItem[] { autoMenuItem, onMenuItem, offMenuItem, new ToolStripSeparator() });
        contextMenuStrip.Items.Add("Exit", null, Exit);

        // Initialize Tray Icon
        trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application, // Replace with your custom icon
            ContextMenuStrip = contextMenuStrip,
            Visible = true
        };

        // Initialize Timer
        timer = new System.Windows.Forms.Timer();
        timer.Interval = _checkIntervalSeconds * 1000; // Convert to milliseconds
        timer.Tick += Timer_Tick;
        timer.Start();

        // Initial Status Update
        UpdateTrayIcon();

        // Handle Application Exit
        Application.ApplicationExit += new EventHandler(OnApplicationExit);
    }

    /// <summary>
    /// Sets the toggle mode and updates the UI and configuration accordingly.
    /// </summary>
    /// <param name="mode">The desired toggle mode.</param>
    private void SetToggleMode(ToggleMode mode)
    {
        if (_currentToggleMode == mode)
            return; // No change

        _currentToggleMode = mode;

        // Update menu check states
        autoMenuItem.Checked = mode == ToggleMode.Auto;
        onMenuItem.Checked = mode == ToggleMode.On;
        offMenuItem.Checked = mode == ToggleMode.Off;

        // Persist the new toggle mode
        UpdateConfiguration();

        // Immediately apply the new mode
        ApplyToggleMode();
    }
    /// <summary>
    /// Updates the `appsettings.json` with the current toggle mode.
    /// </summary>
    private void UpdateConfiguration()
    {
        try
        {
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string json = File.ReadAllText(configFilePath);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
            var jsonElement = jsonDoc.RootElement.Clone();

            using (var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json, jsonOptions);
                if (config != null)
                {
                    if (config.ContainsKey("Toggle"))
                    {
                        var toggle = (System.Text.Json.JsonElement)config["Toggle"];
                        // Update the Mode
                        config["Toggle"] = new Dictionary<string, string>
                            {
                                { "Mode", _currentToggleMode.ToString() }
                            };
                    }
                    else
                    {
                        // Add Toggle section
                        config.Add("Toggle", new Dictionary<string, string>
                            {
                                { "Mode", _currentToggleMode.ToString() }
                            });
                    }

                    // Serialize back to JSON
                    string updatedJson = System.Text.Json.JsonSerializer.Serialize(config, jsonOptions);
                    stream.SetLength(0); // Clear the file
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(updatedJson);
                    writer.Flush();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating configuration: {ex.Message}");
            // Optionally notify the user
        }
    }
    /// <summary>
    /// Applies the current toggle mode by enabling/disabling services and processes accordingly.
    /// </summary>
    private void ApplyToggleMode()
    {
        switch (_currentToggleMode)
        {
            case ToggleMode.Auto:
                // Restore auto behavior based on game status
                break;
            case ToggleMode.On:
                // Ensure services and processes are running
                Task.Run(async () =>
                {
                    await EnsureServicesRunning();
                    UpdateTrayIcon();
                });
                break;
            case ToggleMode.Off:
                // Ensure services and processes are stopped
                Task.Run(async () =>
                {
                    await EnsureServicesStopped();
                    UpdateTrayIcon();
                });
                break;
        }
    }

    private async void Timer_Tick(object sender, EventArgs e)
    {
        try
        {
            // If in Auto mode, perform the usual checks
            if (_currentToggleMode == ToggleMode.Auto)
            {
                bool isAnyGameRunning = IsAnyGameRunning();

                if (isAnyGameRunning)
                {
                    gameNotRunningCount = 0; // Reset counter
                                             // Ensure services are running
                    await EnsureServicesRunning();
                }
                else
                {
                    gameNotRunningCount++;
                    if (gameNotRunningCount >= 2) // Two consecutive checks
                    {
                        // Ensure services are stopped
                        await EnsureServicesStopped();
                        // Reset counter to prevent repeated stop attempts
                        gameNotRunningCount = 0;
                    }
                }

                UpdateTrayIcon();
            }
            // If in On or Off mode, skip automatic management
            else
            {
                // Optionally, ensure services/processes are in the desired state
                // For example, if in On mode, ensure services are running
                // if in Off mode, ensure services are stopped
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in Timer_Tick: {ex.Message}");
            // Optionally log the error or notify the user
        }
    }

    private bool IsAnyGameRunning()
    {
        foreach (var game in _games)
        {
            foreach (var processName in game.ProcessNames)
            {
                if (Process.GetProcessesByName(processName).Length > 0)
                {
                    Debug.WriteLine($"{game.Name} is running.");
                    return true;
                }
            }
        }
        Debug.WriteLine("No monitored games are running.");
        return false;
    }

    private ServiceController? GetService(string serviceName)
    {
        try
        {
            var sc = new ServiceController(serviceName);
            // Accessing Status property to confirm existence
            var status = sc.Status;
            return sc;
        }
        catch (InvalidOperationException)
        {
            Debug.WriteLine($"Service '{serviceName}' does not exist.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving service '{serviceName}': {ex.Message}");
            return null;
        }
    }

    private void StartVgTray()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_vgTrayExePath))
            {
                Debug.WriteLine("vgtray.exe path is not configured.");
                return;
            }

            if (!File.Exists(_vgTrayExePath))
            {
                Debug.WriteLine($"vgtray.exe not found at {_vgTrayExePath}");
                return;
            }

            // Check if vgtray.exe is already running
            var existingProcesses = Process.GetProcessesByName("vgtray");
            if (existingProcesses.Length > 0)
            {
                Debug.WriteLine("vgtray.exe is already running.");
                return;
            }

            Process.Start(_vgTrayExePath);
            Debug.WriteLine("Started vgtray.exe.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error starting vgtray.exe: {ex.Message}");
        }
    }

    private void StopVgTray()
    {
        try
        {
            var processes = Process.GetProcessesByName("vgtray");
            if (processes.Length == 0)
            {
                Debug.WriteLine("vgtray.exe is not running.");
                return;
            }

            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit(5000); // Wait up to 5 seconds for the process to exit
                Debug.WriteLine("Stopped vgtray.exe.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping vgtray.exe: {ex.Message}");
        }
    }

    private bool IsVgTrayRunning()
    {
        return Process.GetProcessesByName("vgtray").Length > 0;
    }

    private async Task EnsureServicesRunning()
    {
        await Task.Run(() =>
        {
            var service = GetService(VGCServiceName);

            if (service != null && service.Status != ServiceControllerStatus.Running)
            {
                try
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    Debug.WriteLine("VGC Service started.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting VGC Service: {ex.Message}");
                }
            }

            StartVgTray();
        });
    }

    private async Task EnsureServicesStopped()
    {
        await Task.Run(() =>
        {
            var service = GetService(VGCServiceName);

            if (service != null && service.Status == ServiceControllerStatus.Running)
            {
                try
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    Debug.WriteLine("VGC Service stopped.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping VGC Service: {ex.Message}");
                }
            }

            StopVgTray();
        });
    }

    private void UpdateTrayIcon()
    {
        try
        {
            bool isAnyGameRunning = IsAnyGameRunning();
            bool isVgTrayRunning = IsVgTrayRunning();
            bool isVgcRunning = false;
            var service = GetService(VGCServiceName);

            if (service != null)
            {
                isVgcRunning = service.Status == ServiceControllerStatus.Running;
            }

            string runningGames = string.Join(", ", GetRunningGames());

            string tooltip = $"Running Games: {(string.IsNullOrEmpty(runningGames) ? "None" : runningGames)}\n" +
                             $"VGC Service: {(isVgcRunning ? "Running" : "Stopped")}\n" +
                             $"VgTray.exe: {(isVgTrayRunning ? "Running" : "Stopped")}\n" +
                             $"Mode: {_currentToggleMode}";

            trayIcon.Text = tooltip;

            // Optionally, change the icon based on status
            if (_currentToggleMode == ToggleMode.On)
            {
                trayIcon.Icon = SystemIcons.Shield; // Example: Different icon for On
            }
            else if (_currentToggleMode == ToggleMode.Off)
            {
                trayIcon.Icon = SystemIcons.Error; // Example: Different icon for Off
            }
            else
            {
                // Auto mode icons based on game running status
                trayIcon.Icon = isAnyGameRunning ? SystemIcons.Application : SystemIcons.Warning;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in UpdateTrayIcon: {ex.Message}");
        }
    }

    private List<string> GetRunningGames()
    {
        List<string> runningGames = new List<string>();

        foreach (var game in _games)
        {
            foreach (var processName in game.ProcessNames)
            {
                if (Process.GetProcessesByName(processName).Length > 0)
                {
                    runningGames.Add(game.Name);
                    break;
                }
            }
        }

        return runningGames;
    }

    private void RefreshStatus(object sender, EventArgs e)
    {
        UpdateTrayIcon();
    }

    private void Exit(object sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }

    private void OnApplicationExit(object sender, EventArgs e)
    {
        trayIcon.Visible = false;
        timer?.Stop();
        timer?.Dispose();
        trayIcon?.Dispose();
    }
}
