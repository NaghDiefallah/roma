using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roma.Models;
using Roma.Services;
using Windows.System;
using Windows.Storage.Pickers;

namespace Roma
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ServerDataService _serverDataService;
        private readonly RageConnectionService _rageConnectionService;
        private readonly SettingsService _settingsService;
        private readonly FavoritesService _favoritesService;
        private readonly RecentServersService _recentServersService;
        private readonly PingService _pingService;

        private ObservableCollection<ServerItem> _allServers;
        private ObservableCollection<ServerItem> _filteredServers;
        private string _serverCountText;
        private CancellationTokenSource? _pingCancellationTokenSource;

        // Sorting state
        private string _currentSortColumn = "players";
        private bool _sortAscending = false;

        public ObservableCollection<ServerItem> FilteredServers
        {
            get
            {
                if (_filteredServers == null)
                {
                    _filteredServers = new ObservableCollection<ServerItem>();
                }
                System.Diagnostics.Debug.WriteLine($"FilteredServers getter called, count: {_filteredServers.Count}");
                return _filteredServers;
            }
        }

        public string ServerCountText
        {
            get
            {
                if (_serverCountText == null)
                {
                    _serverCountText = "0 servers";
                }
                System.Diagnostics.Debug.WriteLine($"ServerCountText getter called: {_serverCountText}");
                return _serverCountText;
            }
            set
            {
                if (_serverCountText != value)
                {
                    _serverCountText = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isFirstActivation = true;

        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting MainWindow initialization...");

                // Initialize collections FIRST - before InitializeComponent (x:Bind might access them)
                _allServers = new ObservableCollection<ServerItem>();
                _filteredServers = new ObservableCollection<ServerItem>();
                _serverCountText = "0 servers";
                System.Diagnostics.Debug.WriteLine("Collections initialized");

                // Initialize component - this must complete successfully
                System.Diagnostics.Debug.WriteLine("Calling InitializeComponent...");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent completed successfully");

                // Verify Content is not null after InitializeComponent
                if (Content == null)
                {
                    throw new InvalidOperationException("Window Content is null after InitializeComponent");
                }

                System.Diagnostics.Debug.WriteLine("Initializing services...");

                // Initialize services with individual error handling
                try
                {
                    _serverDataService = new ServerDataService();
                    System.Diagnostics.Debug.WriteLine("ServerDataService initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize ServerDataService: {ex.Message}");
                    throw;
                }

                try
                {
                    _rageConnectionService = new RageConnectionService();
                    System.Diagnostics.Debug.WriteLine("RageConnectionService initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize RageConnectionService: {ex.Message}");
                    throw;
                }

                try
                {
                    _settingsService = new SettingsService();
                    System.Diagnostics.Debug.WriteLine("SettingsService initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize SettingsService: {ex.Message}");
                    throw;
                }

                try
                {
                    _favoritesService = new FavoritesService();
                    System.Diagnostics.Debug.WriteLine("FavoritesService initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize FavoritesService: {ex.Message}");
                    throw;
                }

                try
                {
                    _recentServersService = new RecentServersService();
                    System.Diagnostics.Debug.WriteLine("RecentServersService initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize RecentServersService: {ex.Message}");
                    throw;
                }

                try
                {
                    _pingService = new PingService();
                    System.Diagnostics.Debug.WriteLine("PingService initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize PingService: {ex.Message}");
                    throw;
                }

                // Handle window closing to cleanup resources
                this.Closed += MainWindow_Closed;

                // Subscribe to Activated event for post-activation setup
                System.Diagnostics.Debug.WriteLine("Subscribing to Activated event...");
                this.Activated += MainWindow_Activated;

                // Register keyboard accelerator for search (Ctrl+K)
                RegisterKeyboardAccelerators();

                System.Diagnostics.Debug.WriteLine("MainWindow initialization completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing MainWindow: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception type: {ex.InnerException.GetType().FullName}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Activated: State = {e.WindowActivationState}");

                // Only run initialization on first activation when window becomes active
                if (_isFirstActivation && e.WindowActivationState != WindowActivationState.Deactivated)
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow_Activated: First activation detected, initializing...");
                    InitializeAfterActivation();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Activated: Error: {ex.Message}");
            }
        }

        public void InitializeAfterActivation()
        {
            if (!_isFirstActivation)
            {
                System.Diagnostics.Debug.WriteLine("InitializeAfterActivation: Already initialized, skipping");
                return;
            }

            _isFirstActivation = false;
            System.Diagnostics.Debug.WriteLine("InitializeAfterActivation: Starting post-activation setup...");

            try
            {
                // Configure custom path if set
                try
                {
                    if (_settingsService != null && !string.IsNullOrEmpty(_settingsService.CustomRageMpPath))
                    {
                        _rageConnectionService?.SetCustomPath(_settingsService.CustomRageMpPath);
                        System.Diagnostics.Debug.WriteLine("Custom RAGE path configured");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set custom RAGE path: {ex.Message}");
                }

                // Configure connection method
                try
                {
                    if (_settingsService != null && _rageConnectionService != null)
                    {
                        _rageConnectionService.SetConnectionMethod(_settingsService.ConnectionMethod ?? "Rage");
                        System.Diagnostics.Debug.WriteLine("Connection method configured");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set connection method: {ex.Message}");
                }

                // Apply saved theme and animation settings
                try
                {
                    if (_settingsService != null)
                    {
                        ApplyTheme(_settingsService.ThemeMode ?? "Dark");
                        ApplyAnimationSettings();
                        System.Diagnostics.Debug.WriteLine("Theme and animations applied");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to apply theme/animation settings: {ex.Message}");
                }

                // Configure window AFTER activation (AppWindow is now available)
                System.Diagnostics.Debug.WriteLine("Configuring window...");
                ConfigureWindow();

                System.Diagnostics.Debug.WriteLine("Window configured - loading servers...");

                // Now it's safe to load servers
                _ = LoadServersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeAfterActivation: Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                // Cancel any ongoing ping operations
                _pingCancellationTokenSource?.Cancel();
                _pingCancellationTokenSource?.Dispose();
                _pingCancellationTokenSource = null;

                System.Diagnostics.Debug.WriteLine("MainWindow: Cleanup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closed: Error during cleanup: {ex.Message}");
            }
        }

        private void ConfigureWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ConfigureWindow: Starting window configuration...");

                // Extend into title bar for custom chrome
                try
                {
                    ExtendsContentIntoTitleBar = true;
                    System.Diagnostics.Debug.WriteLine("ConfigureWindow: ExtendsContentIntoTitleBar set to true");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Failed to set ExtendsContentIntoTitleBar: {ex.Message}");
                }

                // Set the title bar drag region to fix window controls overlap
                try
                {
                    if (AppTitleBar != null)
                    {
                        SetTitleBar(AppTitleBar);
                        System.Diagnostics.Debug.WriteLine("ConfigureWindow: Title bar set successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ConfigureWindow: AppTitleBar is null, skipping SetTitleBar");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Failed to set title bar: {ex.Message}");
                }

                // Set default window size and configure title bar appearance
                try
                {
                    var appWindow = this.AppWindow;
                    if (appWindow != null)
                    {
                        System.Diagnostics.Debug.WriteLine("ConfigureWindow: AppWindow is available");

                        // Resize window
                        try
                        {
                            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1400, Height = 900 });
                            System.Diagnostics.Debug.WriteLine("ConfigureWindow: Window resized to 1400x900");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Failed to resize window: {ex.Message}");
                        }

                        // Configure title bar appearance
                        var titleBar = appWindow.TitleBar;
                        if (titleBar != null)
                        {
                            try
                            {
                                titleBar.ExtendsContentIntoTitleBar = true;
                                titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                                titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                                System.Diagnostics.Debug.WriteLine("ConfigureWindow: Title bar colors configured");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Failed to configure title bar colors: {ex.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ConfigureWindow: TitleBar is null");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ConfigureWindow: AppWindow is null");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Failed to access AppWindow: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine("ConfigureWindow: Window configuration completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Unexpected error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ConfigureWindow: Stack trace: {ex.StackTrace}");
                // Continue execution - window configuration is not critical for basic functionality
            }
        }

        private void RegisterKeyboardAccelerators()
        {
            try
            {
                // Note: In WinUI 3, keyboard accelerators need to be registered on the root content element
                // rather than the Window itself. We'll attach this after InitializeComponent when Content is ready
                System.Diagnostics.Debug.WriteLine("RegisterKeyboardAccelerators: Keyboard accelerators ready for registration");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RegisterKeyboardAccelerators: Error: {ex.Message}");
            }
        }

        private async Task LoadServersAsync()
        {
            try
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                }

                // Cancel any ongoing ping operations
                _pingCancellationTokenSource?.Cancel();
                _pingCancellationTokenSource?.Dispose();
                _pingCancellationTokenSource = new CancellationTokenSource();

                var servers = await _serverDataService.FetchServersAsync(_settingsService.ServerListSource);

                _allServers.Clear();
                foreach (var server in servers)
                {
                    // Set favorite status
                    server.IsFavorite = _favoritesService.IsFavorite(server.Ip, server.Port);
                    _allServers.Add(server);
                }

                ApplyFilters();

                // Start lazy loading pings in background
                _ = LoadPingsAsync(_pingCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                // Only show error dialog if we have a valid XamlRoot
                if (Content?.XamlRoot != null)
                {
                    await ShowErrorDialogAsync("Failed to load servers", ex.Message);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load servers: {ex.Message}");
                }
            }
            finally
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async Task LoadPingsAsync(CancellationToken cancellationToken)
        {
            // Set to true to enable verbose ping logging during debugging
            const bool verbosePingLogging = false;

            foreach (var server in _allServers.ToList()) // Create a copy to avoid collection modification issues
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Load ping asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        var ping = await _pingService.GetPingAsync(server.Ip);

                        if (cancellationToken.IsCancellationRequested)
                            return;

                        // Update on UI thread safely
                        try
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                try
                                {
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        server.Ping = ping;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Silently ignore if server object is no longer valid
                                    if (verbosePingLogging)
                                        System.Diagnostics.Debug.WriteLine($"Failed to update ping for {server.Ip}: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Dispatcher queue might be shutdown - only log if verbose
                            if (verbosePingLogging)
                                System.Diagnostics.Debug.WriteLine($"Failed to enqueue ping update: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Only log unexpected errors in verbose mode
                        if (verbosePingLogging)
                            System.Diagnostics.Debug.WriteLine($"Ping task failed for {server.Ip}: {ex.Message}");
                    }
                }, cancellationToken);

                // Small delay to avoid overwhelming the network
                try
                {
                    await Task.Delay(50, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }
        }

        private void ApplyFilters()
        {
            try
            {
                // Guard against being called before UI elements are initialized
                if (SearchBox == null || LanguageFilter == null || GamemodeFilter == null || _allServers == null || _filteredServers == null)
                {
                    System.Diagnostics.Debug.WriteLine("ApplyFilters called before UI initialization complete");
                    return;
                }

                var query = SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

                // Extract language from ComboBoxItem content (handle emoji + text format)
                var languageItem = (LanguageFilter.SelectedItem as ComboBoxItem)?.Content;
                string selectedLanguage = "ALL";

                if (languageItem is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
                {
                    var text = tb.Text;
                    if (!string.IsNullOrEmpty(text) && text.Contains("(") && text.Contains(")"))
                    {
                        // Extract language code from "English (EN)" format
                        var start = text.IndexOf("(") + 1;
                        var end = text.IndexOf(")");
                        if (end > start)
                        {
                            selectedLanguage = text.Substring(start, end - start);
                        }
                    }
                    else if (text == "All Languages")
                    {
                        selectedLanguage = "ALL";
                    }
                }

                // Extract gamemode
                var gamemodeItem = (GamemodeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Gamemodes";

                var hideEmpty = HideEmptyToggle?.IsOn ?? false;
                var showFavoritesOnly = ShowFavoritesToggle?.IsOn ?? false;

                var filtered = _allServers.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrEmpty(query))
                {
                    filtered = filtered.Where(s =>
                        s.Name?.ToLowerInvariant().Contains(query) == true ||
                        s.Ip?.ToLowerInvariant().Contains(query) == true ||
                        s.Language?.ToLowerInvariant().Contains(query) == true ||
                        s.Gamemode?.ToLowerInvariant().Contains(query) == true ||
                        (s.Tags != null && s.Tags.Any(t => t?.ToLowerInvariant().Contains(query) == true)));
                }

                // Apply language filter
                if (selectedLanguage != "ALL")
                {
                    filtered = filtered.Where(s => 
                        s.Language?.Equals(selectedLanguage, StringComparison.OrdinalIgnoreCase) == true);
                }

                // Apply gamemode filter
                if (gamemodeItem != "All Gamemodes")
                {
                    filtered = filtered.Where(s => 
                        s.Gamemode?.Equals(gamemodeItem, StringComparison.OrdinalIgnoreCase) == true);
                }

                // Apply empty server filter
                if (hideEmpty)
                {
                    filtered = filtered.Where(s => !s.IsEmpty);
                }

                // Apply favorites filter
                if (showFavoritesOnly)
                {
                    filtered = filtered.Where(s => s.IsFavorite);
                }

                // Apply sorting
                filtered = ApplySorting(filtered);

                // Update filtered collection
                _filteredServers.Clear();
                foreach (var server in filtered)
                {
                    _filteredServers.Add(server);
                }

                // Update server count
                var totalServers = _allServers.Count;
                var filteredCount = _filteredServers.Count;
                ServerCountText = filteredCount == totalServers 
                    ? $"{totalServers} servers" 
                    : $"{filteredCount} / {totalServers} servers";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Error applying filters: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Stack trace: {ex.StackTrace}");
            }
        }

        private IEnumerable<ServerItem> ApplySorting(IEnumerable<ServerItem> servers)
        {
            try
            {
                if (servers == null)
                    return Enumerable.Empty<ServerItem>();

                return _currentSortColumn switch
                {
                    "flag" => _sortAscending 
                        ? servers.OrderBy(s => s.Language ?? string.Empty) 
                        : servers.OrderByDescending(s => s.Language ?? string.Empty),
                    "name" => _sortAscending 
                        ? servers.OrderBy(s => s.Name ?? string.Empty) 
                        : servers.OrderByDescending(s => s.Name ?? string.Empty),
                    "players" => _sortAscending 
                        ? servers.OrderBy(s => s.Players) 
                        : servers.OrderByDescending(s => s.Players),
                    "language" => _sortAscending 
                        ? servers.OrderBy(s => s.Language ?? string.Empty) 
                        : servers.OrderByDescending(s => s.Language ?? string.Empty),
                    _ => servers.OrderByDescending(s => s.Players)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplySorting: Error sorting servers: {ex.Message}");
                return servers ?? Enumerable.Empty<ServerItem>();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_allServers != null && _filteredServers != null)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchBox_TextChanged: Error: {ex.Message}");
            }
        }

        private void LanguageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_allServers != null && _filteredServers != null && GamemodeFilter != null)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LanguageFilter_SelectionChanged: Error: {ex.Message}");
            }
        }

        private void GamemodeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_allServers != null && _filteredServers != null && LanguageFilter != null)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GamemodeFilter_SelectionChanged: Error: {ex.Message}");
            }
        }

        private void HideEmptyToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allServers != null && _filteredServers != null)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HideEmptyToggle_Toggled: Error: {ex.Message}");
            }
        }

        private void ShowFavoritesToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allServers != null && _filteredServers != null)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowFavoritesToggle_Toggled: Error: {ex.Message}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = LoadServersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshButton_Click: Error: {ex.Message}");
            }
        }

        private void SortByFlag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleSort("flag");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SortByFlag_Click: Error: {ex.Message}");
            }
        }

        private void SortByName_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleSort("name");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SortByName_Click: Error: {ex.Message}");
            }
        }

        private void SortByPlayers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleSort("players");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SortByPlayers_Click: Error: {ex.Message}");
            }
        }

        private void SortByLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleSort("language");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SortByLanguage_Click: Error: {ex.Message}");
            }
        }

        private void ToggleSort(string column)
        {
            try
            {
                if (string.IsNullOrEmpty(column))
                    return;

                if (_currentSortColumn == column)
                {
                    _sortAscending = !_sortAscending;
                }
                else
                {
                    _currentSortColumn = column;
                    _sortAscending = column == "name" || column == "flag" || column == "language";
                }

                if (_allServers != null && _filteredServers != null)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleSort: Error: {ex.Message}");
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ServerItem server)
                {
                    if (_recentServersService != null && !string.IsNullOrEmpty(server.Ip) && !string.IsNullOrEmpty(server.Port))
                    {
                        // Add to recent servers
                        _recentServersService.AddRecent(server.Ip, server.Port, server.Name ?? string.Empty, server.DisplayName ?? server.Name ?? string.Empty);
                    }

                    if (_rageConnectionService != null && !string.IsNullOrEmpty(server.Ip) && !string.IsNullOrEmpty(server.Port))
                    {
                        // Connect to server using protocol handler
                        var success = _rageConnectionService.ConnectToServer(server.Ip, server.Port);

                        if (success)
                        {
                            // Auto-close if enabled
                            if (_settingsService?.AutoCloseOnConnect == true)
                            {
                                Application.Current.Exit();
                            }
                        }
                        else
                        {
                            await ShowErrorDialogAsync(
                                "Connection Failed",
                                "Failed to launch RAGE:MP. Make sure RAGE:MP is installed.");
                        }
                    }
                    else
                    {
                        await ShowErrorDialogAsync(
                            "Connection Error",
                            "Unable to connect: Invalid server information or service not available.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConnectButton_Click: Error: {ex.Message}");
                await ShowErrorDialogAsync("Connection Error", $"An error occurred: {ex.Message}");
            }
        }

        private async void ServerRow_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is ServerItem server)
            {
                await ShowServerDetailsAsync(server);
            }
        }

        private async Task ShowServerDetailsAsync(ServerItem server)
        {
            var dialog = new ContentDialog
            {
                Title = "Server Details",
                CloseButtonText = "Close",
                XamlRoot = Content.XamlRoot
            };

            var scrollViewer = new ScrollViewer { MaxHeight = 600 };
            var detailsPanel = new StackPanel { Spacing = 20, Padding = new Thickness(0, 16, 0, 0) };

            // Server Name
            var nameHeader = new TextBlock { Text = "Server Name", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 175, 175, 180)), FontWeight = Microsoft.UI.Text.FontWeights.Medium };
            detailsPanel.Children.Add(nameHeader);
            var nameText = new TextBlock { Text = server.DisplayName, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            detailsPanel.Children.Add(nameText);

            // Tags
            if (server.Tags.Count > 0)
            {
                var tagsHeader = new TextBlock { Text = "Tags", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 175, 175, 180)), FontWeight = Microsoft.UI.Text.FontWeights.Medium };
                detailsPanel.Children.Add(tagsHeader);
                var tagsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                foreach (var tag in server.Tags)
                {
                    var tagBorder = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 42, 42, 48)),
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 58, 58, 64)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    var tagText = new TextBlock { Text = tag, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 175, 175, 180)) };
                    tagBorder.Child = tagText;
                    tagsPanel.Children.Add(tagBorder);
                }
                detailsPanel.Children.Add(tagsPanel);
            }

            // Information
            var infoHeader = new TextBlock { Text = "Information", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) };
            detailsPanel.Children.Add(infoHeader);

            var infoGrid = new Grid { ColumnSpacing = 12, RowSpacing = 4 };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddInfoRow(infoGrid, 0, "Address:", server.AddressText);
            AddInfoRow(infoGrid, 1, "Players:", server.CapacityText);
            AddInfoRow(infoGrid, 2, "Language:", server.LanguageDisplay);
            AddInfoRow(infoGrid, 3, "Gamemode:", server.Gamemode);
            AddInfoRow(infoGrid, 4, "Ping:", server.PingText);

            detailsPanel.Children.Add(infoGrid);

            scrollViewer.Content = detailsPanel;
            dialog.Content = scrollViewer;
            await dialog.ShowAsync();
        }

        private void AddInfoRow(Grid grid, int row, string label, string value)
        {
            var labelText = new TextBlock
            {
                Text = label,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 175, 175, 180)),
                Margin = new Thickness(0, 4, 0, 4)
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value,
                Margin = new Thickness(0, 4, 0, 4),
                FontFamily = label == "Address:" ? new Microsoft.UI.Xaml.Media.FontFamily("Consolas") : null
            };
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);
        }

        private void ServerRow_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is ServerItem server)
            {
                var flyout = new MenuFlyout();

                // Connect
                var connectItem = new MenuFlyoutItem
                {
                    Text = "Connect",
                    Icon = new SymbolIcon(Symbol.Play)
                };
                connectItem.Click += async (s, args) =>
                {
                    _recentServersService.AddRecent(server.Ip, server.Port, server.Name, server.DisplayName);
                    var success = _rageConnectionService.ConnectToServer(server.Ip, server.Port);
                    if (!success)
                    {
                        await ShowErrorDialogAsync("Connection Failed", "Failed to launch RAGE:MP.");
                    }
                };
                flyout.Items.Add(connectItem);

                // Favorite
                var favoriteItem = new MenuFlyoutItem
                {
                    Text = server.IsFavorite ? "Unfavorite" : "Favorite",
                    Icon = new SymbolIcon(server.IsFavorite ? Symbol.UnFavorite : Symbol.Favorite)
                };
                favoriteItem.Click += (s, args) =>
                {
                    _favoritesService.ToggleFavorite(server.Ip, server.Port);
                    server.IsFavorite = !server.IsFavorite;
                };
                flyout.Items.Add(favoriteItem);

                // View More
                var viewMoreItem = new MenuFlyoutItem
                {
                    Text = "View Details",
                    Icon = new SymbolIcon(Symbol.More)
                };
                viewMoreItem.Click += async (s, args) =>
                {
                    await ShowServerDetailsAsync(server);
                };
                flyout.Items.Add(viewMoreItem);

                flyout.ShowAt(grid, e.GetPosition(grid));
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService == null)
            {
                await ShowErrorDialogAsync("Error", "Settings service is not initialized.");
                return;
            }

            var localization = LocalizationService.Instance;

            var dialog = new ContentDialog
            {
                Title = localization.Settings,
                CloseButtonText = localization.Close,
                XamlRoot = Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
            };

            var scrollViewer = new ScrollViewer { MaxHeight = 500 };
            var settingsPanel = new StackPanel { Spacing = 16, Padding = new Thickness(0, 16, 0, 0) };

            // Language selector
            var languageHeader = new TextBlock
            {
                Text = localization.AppLanguage,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
            };
            settingsPanel.Children.Add(languageHeader);

            var languageCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SelectedIndex = 0
            };

            var languages = LocalizationService.GetAvailableLanguages();
            var currentLangIndex = 0;

            for (int i = 0; i < languages.Count; i++)
            {
                var lang = languages[i];
                var item = new ComboBoxItem();
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

                panel.Children.Add(new TextBlock 
                { 
                    Text = lang.Flag, 
                    FontSize = 16,
                    IsTextScaleFactorEnabled = false
                });
                panel.Children.Add(new TextBlock { Text = lang.Name });

                item.Content = panel;
                item.Tag = lang.Code;
                languageCombo.Items.Add(item);

                if (lang.Code == _settingsService.AppLanguage)
                {
                    currentLangIndex = i;
                }
            }

            languageCombo.SelectedIndex = currentLangIndex;
            languageCombo.SelectionChanged += (s, args) =>
            {
                if (languageCombo.SelectedItem is ComboBoxItem item && item.Tag is string code)
                {
                    _settingsService.AppLanguage = code;
                    LocalizationService.Instance.CurrentCulture = new System.Globalization.CultureInfo(code);
                }
            };
            settingsPanel.Children.Add(languageCombo);

            // Auto-close toggle
            var autoCloseToggle = new ToggleSwitch
            {
                Header = localization.AutoCloseOnConnect,
                IsOn = _settingsService.AutoCloseOnConnect
            };
            autoCloseToggle.Toggled += (s, args) =>
            {
                _settingsService.AutoCloseOnConnect = autoCloseToggle.IsOn;
            };
            settingsPanel.Children.Add(autoCloseToggle);

            // Reduce animations toggle
            var reduceAnimToggle = new ToggleSwitch
            {
                Header = "Reduce animations",
                IsOn = _settingsService.ReduceAnimations
            };
            reduceAnimToggle.Toggled += (s, args) =>
            {
                _settingsService.ReduceAnimations = reduceAnimToggle.IsOn;
                ApplyAnimationSettings();
            };
            settingsPanel.Children.Add(reduceAnimToggle);

            // Connection Method section
            var connectionMethodHeader = new TextBlock
            {
                Text = "Connection Method",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 8, 0, 0)
            };
            settingsPanel.Children.Add(connectionMethodHeader);

            var connectionMethodCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            connectionMethodCombo.Items.Add(new ComboBoxItem { Content = "Rage (Updater.exe - Recommended)", Tag = "Rage" });
            connectionMethodCombo.Items.Add(new ComboBoxItem { Content = "Protocol (rage:// URL)", Tag = "Protocol" });
            connectionMethodCombo.SelectedIndex = _settingsService.ConnectionMethod == "Protocol" ? 1 : 0;
            connectionMethodCombo.SelectionChanged += (s, args) =>
            {
                if (connectionMethodCombo.SelectedItem is ComboBoxItem item && item.Tag is string method)
                {
                    _settingsService.ConnectionMethod = method;
                    _rageConnectionService.SetConnectionMethod(method);
                }
            };
            settingsPanel.Children.Add(connectionMethodCombo);

            // Server List Source section
            var serverListSourceHeader = new TextBlock
            {
                Text = "Server List Source",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 8, 0, 0)
            };
            settingsPanel.Children.Add(serverListSourceHeader);

            var serverListSourceCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            serverListSourceCombo.Items.Add(new ComboBoxItem { Content = "Community (Current Source)", Tag = "Community" });
            serverListSourceCombo.Items.Add(new ComboBoxItem { Content = "RageMP Official", Tag = "Official" });
            serverListSourceCombo.SelectedIndex = _settingsService.ServerListSource == "Official" ? 1 : 0;
            serverListSourceCombo.SelectionChanged += (s, args) =>
            {
                if (serverListSourceCombo.SelectedItem is ComboBoxItem item && item.Tag is string source)
                {
                    _settingsService.ServerListSource = source;
                }
            };
            settingsPanel.Children.Add(serverListSourceCombo);

            // Theme mode section
            var themeHeader = new TextBlock
            {
                Text = "Theme",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 8, 0, 0)
            };
            settingsPanel.Children.Add(themeHeader);

            var themeCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            themeCombo.Items.Add(new ComboBoxItem { Content = "Dark", Tag = "Dark" });
            themeCombo.Items.Add(new ComboBoxItem { Content = "Light", Tag = "Light" });
            themeCombo.SelectedIndex = _settingsService.ThemeMode == "Light" ? 1 : 0;
            themeCombo.SelectionChanged += (s, args) =>
            {
                if (themeCombo.SelectedItem is ComboBoxItem item && item.Tag is string mode)
                {
                    _settingsService.ThemeMode = mode;
                    ApplyTheme(mode);
                }
            };
            settingsPanel.Children.Add(themeCombo);

            // Accent color picker
            var accentHeader = new TextBlock
            {
                Text = "Accent Color",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 8, 0, 0)
            };
            settingsPanel.Children.Add(accentHeader);

            var accentPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

            var presetColors = new[] 
            { 
                "#FFFFFF", "#FFD700", "#FF6B6B", "#4ECDC4", 
                "#45B7D1", "#96CEB4", "#DDA15E", "#BC6C25" 
            };

            foreach (var colorHex in presetColors)
            {
                var colorBtn = new Button
                {
                    Width = 40,
                    Height = 40,
                    CornerRadius = new CornerRadius(20),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseHexColor(colorHex)),
                    BorderThickness = new Thickness(_settingsService.AccentColor == colorHex ? 3 : 1),
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        _settingsService.AccentColor == colorHex 
                            ? Microsoft.UI.Colors.White 
                            : Microsoft.UI.ColorHelper.FromArgb(255, 42, 42, 48)),
                    Tag = colorHex
                };
                colorBtn.Click += (s, args) =>
                {
                    if (s is Button btn && btn.Tag is string color)
                    {
                        _settingsService.AccentColor = color;
                        // Update all borders
                        foreach (var child in accentPanel.Children)
                        {
                            if (child is Button b)
                            {
                                b.BorderThickness = new Thickness(b.Tag?.ToString() == color ? 3 : 1);
                                b.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                    b.Tag?.ToString() == color 
                                        ? Microsoft.UI.Colors.White 
                                        : Microsoft.UI.ColorHelper.FromArgb(255, 42, 42, 48));
                            }
                        }
                    }
                };
                accentPanel.Children.Add(colorBtn);
            }
            settingsPanel.Children.Add(accentPanel);

            // RAGE-MP path
            var pathHeader = new TextBlock
            {
                Text = "RAGE:MP Installation Path",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 8, 0, 0)
            };
            settingsPanel.Children.Add(pathHeader);

            var pathPanel = new StackPanel { Spacing = 8 };
            var pathBox = new TextBox
            {
                Text = string.IsNullOrEmpty(_settingsService.CustomRageMpPath) 
                    ? _rageConnectionService.GetRageMpPath() 
                    : _settingsService.CustomRageMpPath,
                IsReadOnly = true,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12
            };
            pathPanel.Children.Add(pathBox);

            var browseButton = new Button
            {
                Content = "Browse...",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            browseButton.Click += async (s, args) =>
            {
                var picker = new FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    _settingsService.CustomRageMpPath = folder.Path;
                    _rageConnectionService.SetCustomPath(folder.Path);
                    pathBox.Text = folder.Path;
                }
            };
            pathPanel.Children.Add(browseButton);
            settingsPanel.Children.Add(pathPanel);

            // Refresh button
            var refreshButton = new Button
            {
                Content = localization.RefreshServerList,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            refreshButton.Click += async (s, args) =>
            {
                dialog.Hide();
                await LoadServersAsync();
            };
            settingsPanel.Children.Add(refreshButton);

            scrollViewer.Content = settingsPanel;
            dialog.Content = scrollViewer;
            await dialog.ShowAsync();
        }

        private async void DirectConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Direct Connect",
                PrimaryButtonText = "Connect",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var panel = new StackPanel { Spacing = 16, Padding = new Thickness(0, 16, 0, 0), MinWidth = 400 };

            var descText = new TextBlock
            {
                Text = "Enter the IP address and port of the server you want to connect to:",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 175, 175, 180))
            };
            panel.Children.Add(descText);

            // IP Address
            var ipHeader = new TextBlock { Text = "IP Address", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium };
            panel.Children.Add(ipHeader);
            var ipBox = new TextBox
            {
                PlaceholderText = "e.g., 192.168.1.1 or play.example.com",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
            };
            panel.Children.Add(ipBox);

            // Port
            var portHeader = new TextBlock { Text = "Port", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium };
            panel.Children.Add(portHeader);
            var portBox = new TextBox
            {
                Text = "22005",
                PlaceholderText = "22005",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
            };
            panel.Children.Add(portBox);

            var errorText = new TextBlock
            {
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(errorText);

            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var serverIp = ipBox.Text?.Trim() ?? string.Empty;
                var serverPort = portBox.Text?.Trim() ?? "22005";

                if (!string.IsNullOrEmpty(serverIp))
                {
                    _recentServersService.AddRecent(serverIp, serverPort, $"{serverIp}:{serverPort}", $"{serverIp}:{serverPort}");
                    var success = _rageConnectionService.ConnectToServer(serverIp, serverPort);
                    if (!success)
                    {
                        await ShowErrorDialogAsync("Connection Failed", "Failed to launch RAGE:MP.");
                    }
                }
            }
        }

        private async void RecentServersButton_Click(object sender, RoutedEventArgs e)
        {
            var recentServers = _recentServersService.GetRecent();

            // Clear previous content
            RecentServersPanel.Children.Clear();

            if (recentServers.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No recent servers. Connect to a server to see it here.",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 40),
                    FontSize = 14
                };
                RecentServersPanel.Children.Add(emptyText);
                ClearRecentButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                ClearRecentButton.Visibility = Visibility.Visible;

                foreach (var recent in recentServers)
                {
                    var serverCard = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 22, 25)),
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 42, 42, 48)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(16, 12, 16, 12)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var infoPanel = new StackPanel { Spacing = 4 };

                    var nameText = new TextBlock
                    {
                        Text = recent.DisplayName,
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                    };
                    infoPanel.Children.Add(nameText);

                    var addressPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

                    var addressText = new TextBlock
                    {
                        Text = recent.AddressText,
                        FontSize = 12,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 110, 110, 120))
                    };
                    addressPanel.Children.Add(addressText);

                    var timeText = new TextBlock
                    {
                        Text = $"• {recent.TimeAgo}",
                        FontSize = 12,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 110, 110, 120))
                    };
                    addressPanel.Children.Add(timeText);

                    infoPanel.Children.Add(addressPanel);
                    grid.Children.Add(infoPanel);

                    var connectBtn = new Button
                    {
                        Content = "Connect",
                        Height = 36,
                        Padding = new Thickness(20, 0, 20, 0),
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                        BorderThickness = new Thickness(0),
                        CornerRadius = new CornerRadius(6),
                        FontSize = 13,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Tag = recent
                    };
                    connectBtn.Click += async (s, args) =>
                    {
                        if (s is Button btn && btn.Tag is RecentServer server)
                        {
                            RecentServersOverlay.Visibility = Visibility.Collapsed;
                            var success = _rageConnectionService.ConnectToServer(server.Ip, server.Port);
                            if (!success)
                            {
                                await ShowErrorDialogAsync("Connection Failed", "Failed to launch RAGE:MP.");
                            }
                        }
                    };
                    Grid.SetColumn(connectBtn, 1);
                    grid.Children.Add(connectBtn);

                    serverCard.Child = grid;
                    RecentServersPanel.Children.Add(serverCard);
                }
            }

            // Show overlay
            RecentServersOverlay.Visibility = Visibility.Visible;
        }

        private void CloseRecentServersOverlay_Click(object sender, RoutedEventArgs e)
        {
            RecentServersOverlay.Visibility = Visibility.Collapsed;
        }

        private void ClearRecentServers_Click(object sender, RoutedEventArgs e)
        {
            _recentServersService.ClearRecent();
            RecentServersOverlay.Visibility = Visibility.Collapsed;
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            if (Content?.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine($"Error - {title}: {message}");
                return;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void ApplyTheme(string themeMode)
        {
            try
            {
                if (string.IsNullOrEmpty(themeMode))
                {
                    themeMode = "Dark";
                }

                var isDark = themeMode == "Dark";

                // Update main grid background
                if (Content is Grid mainGrid)
                {
                    mainGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        isDark 
                            ? Microsoft.UI.ColorHelper.FromArgb(255, 13, 13, 15)  // #0D0D0F
                            : Microsoft.UI.ColorHelper.FromArgb(255, 245, 245, 247)  // #F5F5F7
                    );
                }

                // Update window title bar (only if window is activated)
                try
                {
                    var appWindow = this.AppWindow;
                    if (appWindow?.TitleBar != null)
                    {
                        appWindow.TitleBar.ButtonForegroundColor = isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;
                        appWindow.TitleBar.ButtonInactiveForegroundColor = isDark ? Microsoft.UI.Colors.Gray : Microsoft.UI.Colors.DarkGray;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to update title bar theme: {ex.Message}");
                }

                // Note: Full theme switching would require updating all UI elements
                // This is a basic implementation - consider using ThemeResource for production
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyTheme: Error applying theme: {ex.Message}");
            }
        }

        private void ApplyAnimationSettings()
        {
            try
            {
                // Wire up reduce animations to UI
                // This would typically involve:
                // 1. Setting transition durations to 0 or very short
                // 2. Disabling entrance/exit animations
                // 3. Using VisualStateManager to apply simplified states

                var duration = (_settingsService?.ReduceAnimations ?? false)
                    ? TimeSpan.Zero 
                    : TimeSpan.FromMilliseconds(250);

                // Apply to any animated transitions in the app
                // For now, we'll set a flag that can be checked when creating animations
                Application.Current.Resources["AnimationDuration"] = duration;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyAnimationSettings: Error applying animation settings: {ex.Message}");
            }
        }

        private Windows.UI.Color ParseHexColor(string hex)
        {
            // Remove # if present
            hex = hex.TrimStart('#');

            if (hex.Length == 6)
            {
                // RGB format
                return Microsoft.UI.ColorHelper.FromArgb(
                    255,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16)
                );
            }
            else if (hex.Length == 8)
            {
                // ARGB format
                return Microsoft.UI.ColorHelper.FromArgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16)
                );
            }

            // Default to white
            return Microsoft.UI.Colors.White;
        }
    }
}
