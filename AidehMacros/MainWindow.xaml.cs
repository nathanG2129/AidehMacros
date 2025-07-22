using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AidehMacros.Models;
using AidehMacros.Services;


namespace AidehMacros
{
    public partial class MainWindow : Window
    {
        private readonly ConfigurationService _configService = null!;
        private readonly MacroExecutionService _executionService = null!;
        private readonly RawInputKeyboardHook _rawInputHook = null!; // Primary hook for device detection and action execution
        private readonly LowLevelKeyboardHook _blockingHook = null!; // Secondary hook for input blocking only
        
        private Configuration _currentConfig = null!;
        private string? _detectedKeyboardId = null;
        private ObservableCollection<MacroAction> _actions = null!;
        private ObservableCollection<MacroMapping> _mappings = null!;
        private System.Threading.CancellationTokenSource? _testDisplayResetToken;
        
        // Device correlation tracking
        private readonly HashSet<string> _mappedKeys = new(); // Keys that should be blocked
        private readonly Dictionary<string, DateTime> _recentMacroKeyPresses = new();
        private readonly TimeSpan _keyCorrelationWindow = TimeSpan.FromMilliseconds(100); // Increased from 50ms to 100ms
        
        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow constructor started");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent completed");
                
                System.Diagnostics.Debug.WriteLine("Creating services...");
                
                System.Diagnostics.Debug.WriteLine("About to create ConfigurationService...");
                _configService = new ConfigurationService();
                System.Diagnostics.Debug.WriteLine("ConfigurationService created");
                
                System.Diagnostics.Debug.WriteLine("About to create MacroExecutionService...");
                _executionService = new MacroExecutionService();
                System.Diagnostics.Debug.WriteLine("MacroExecutionService created");
                
                System.Diagnostics.Debug.WriteLine("About to create RawInputKeyboardHook...");
                _rawInputHook = new RawInputKeyboardHook();
                System.Diagnostics.Debug.WriteLine("RawInputKeyboardHook created");
                
                System.Diagnostics.Debug.WriteLine("About to create LowLevelKeyboardHook for blocking...");
                _blockingHook = new LowLevelKeyboardHook();
                System.Diagnostics.Debug.WriteLine("LowLevelKeyboardHook created");
                
                System.Diagnostics.Debug.WriteLine("Creating collections...");
                _actions = new ObservableCollection<MacroAction>();
                _mappings = new ObservableCollection<MacroMapping>();
                System.Diagnostics.Debug.WriteLine("Collections created");
                
                System.Diagnostics.Debug.WriteLine("About to call InitializeApplication");
                InitializeApplication();
                System.Diagnostics.Debug.WriteLine("InitializeApplication completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in MainWindow constructor: {ex.Message}");
                
                // Ensure minimum initialization
                _currentConfig = new Services.Configuration();
                _actions = new ObservableCollection<MacroAction>();
                _mappings = new ObservableCollection<MacroMapping>();
                
                // Try to show the error to the user
                System.Windows.MessageBox.Show($"Application failed to initialize properly: {ex.Message}", 
                    "Initialization Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
            }
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the raw input hook for device detection and action execution
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            System.Diagnostics.Debug.WriteLine($"MainWindow.Window_Loaded: Initializing raw input hook with handle {windowHandle:X8}");
            
            if (_rawInputHook.Initialize(windowHandle))
            {
                _rawInputHook.KeyDown += OnRawInputKeyDown;
                
                // Set the target device if we have one
                if (!string.IsNullOrEmpty(_detectedKeyboardId))
                {
                    _rawInputHook.SetTargetDevice(_detectedKeyboardId);
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Set raw input target device to {_detectedKeyboardId}");
                }
                
                System.Diagnostics.Debug.WriteLine("MainWindow: Raw Input keyboard hook started successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Failed to start raw input hook");
            }
            
            // Initialize the low-level hook for input blocking
            System.Diagnostics.Debug.WriteLine("MainWindow: Starting low-level keyboard hook for input blocking");
            _blockingHook.KeyDown += OnBlockingHookKeyDown;
            _blockingHook.StartHook();
            System.Diagnostics.Debug.WriteLine("MainWindow: Low-level keyboard hook started");
            
            UpdateStatus("Keyboard hooks initialized - ready for macro detection and blocking");
        }
        
        private void InitializeApplication()
        {
            try
            {
                // Load configuration
                _currentConfig = _configService.LoadConfiguration();
                System.Diagnostics.Debug.WriteLine($"Configuration loaded: {_currentConfig != null}");
            
                // Setup data bindings
                ActionsListView.ItemsSource = _actions;
                MappingsListView.ItemsSource = _mappings;
                
                // Load data from config
                LoadActionsFromConfig();
                LoadMappingsFromConfig();
                
                // Load the detected keyboard from config
                _detectedKeyboardId = _currentConfig?.MacroKeyboardDeviceId;
                
                // Set initial state
                EnabledToggle.IsChecked = _currentConfig?.IsEnabled ?? true;
                
                // Update UI based on whether we have a keyboard configured
                if (string.IsNullOrEmpty(_detectedKeyboardId))
                {
                    KeyboardStatusText.Text = "No macro keyboard configured";
                    TestKeyDisplay.Text = "No keyboard configured. Use 'Setup Macro Keyboard' first.";
                    TestKeyDisplay.Background = System.Windows.Media.Brushes.LightGray;
                    UpdateStatus("No macro keyboard configured. Use 'Setup Keyboard' to get started.");
                }
                else
                {
                    KeyboardStatusText.Text = $"Configured: {_detectedKeyboardId}";
                    TestKeyDisplay.Text = "Waiting for key press from your macro keyboard...";
                    TestKeyDisplay.Background = System.Windows.Media.Brushes.LightBlue;
                    UpdateStatus($"Ready - Macro keyboard: {_detectedKeyboardId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeApplication: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Ensure we have a valid config even if initialization fails
                if (_currentConfig == null)
                {
                    _currentConfig = new Services.Configuration();
                }
                
                UpdateStatus($"Error during initialization: {ex.Message}");
            }
        }
        
        private void LoadActionsFromConfig()
        {
            _actions.Clear();
            foreach (var action in _currentConfig.Actions)
            {
                _actions.Add(action);
            }
        }
        
        private void LoadMappingsFromConfig()
        {
            _mappings.Clear();
            _mappedKeys.Clear(); // Clear the blocking set
            
            foreach (var mapping in _currentConfig.Mappings)
            {
                mapping.Action = _currentConfig.Actions.FirstOrDefault(a => a.Id == mapping.ActionId);
                _mappings.Add(mapping);
                _mappedKeys.Add(mapping.TriggerKey); // Add to preemptive blocking set
            }
        }
        
        private void SetupKeyboard()
        {
            // Temporarily dispose the main window's hooks to avoid conflicts
            _rawInputHook?.Dispose();
            _blockingHook?.StopHook();
            System.Diagnostics.Debug.WriteLine("MainWindow.SetupKeyboard: Stopped both hooks before detection");
            
            var dialog = new KeyboardDetectionDialog();
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true && dialog.WasDetected && !string.IsNullOrEmpty(dialog.DetectedKeyboardId))
            {
                _detectedKeyboardId = dialog.DetectedKeyboardId;
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Detected keyboard ID: {_detectedKeyboardId}");
                
                // Re-initialize the raw input hook
                var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (_rawInputHook.Initialize(windowHandle))
                {
                    _rawInputHook.KeyDown += OnRawInputKeyDown;
                    _rawInputHook.SetTargetDevice(_detectedKeyboardId);
                    System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Re-initialized raw input hook with target device");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Failed to re-initialize raw input hook");
                }
                
                // Re-start the blocking hook
                _blockingHook.StartHook();
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Re-started blocking hook");
                
                // Save to configuration
                _configService.SetMacroKeyboard(_currentConfig, _detectedKeyboardId ?? string.Empty);
                _configService.SaveConfiguration(_currentConfig);
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Saved configuration");
                
                // Update UI
                KeyboardStatusText.Text = $"Configured: {_detectedKeyboardId}";
                UpdateStatus($"Macro keyboard configured: {_detectedKeyboardId} - Ready for testing with input blocking!");
                
                // Clear the test display to show it's ready for new input
                TestKeyDisplay.Text = "Keyboard configured! Press any key on your macro keyboard to test blocking...";
                TestKeyDisplay.Background = System.Windows.Media.Brushes.LightGreen;
                
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: UI updated, ready for testing");
            }
            else
            {
                // If detection was cancelled, re-initialize the hooks anyway
                var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (_rawInputHook.Initialize(windowHandle))
                {
                    _rawInputHook.KeyDown += OnRawInputKeyDown;
                    if (!string.IsNullOrEmpty(_detectedKeyboardId))
                    {
                        _rawInputHook.SetTargetDevice(_detectedKeyboardId);
                    }
                    System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Re-initialized raw input hook after cancelled detection");
                }
                
                _blockingHook.StartHook();
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Re-started blocking hook after cancelled detection");
            }
        }
        

        
        private void OnRawInputKeyDown(object? sender, RawKeyboardEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow.OnRawInputKeyDown: Key={e.Key}, Device={e.DeviceId}, IsTarget={e.IsFromTargetDevice}");
            
            var keyName = e.Key.ToString();
            
            // Only process and potentially block keys from our target macro keyboard
            if (e.IsFromTargetDevice)
            {
                if (!_currentConfig.IsEnabled) 
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow: Macro system disabled, ignoring key from macro keyboard");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"MainWindow: Processing key '{keyName}' from macro keyboard");
                
                // Mark this key for potential blocking by the Low-Level Hook
                _recentMacroKeyPresses[keyName] = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"MainWindow: Marked key '{keyName}' for blocking correlation at {DateTime.Now:HH:mm:ss.fff}");
                
                // Check if this key has a mapping and execute the action
                var mapping = _currentConfig.GetMappingForKey(keyName);
                
                if (mapping?.Action != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Found mapping for key {keyName}, executing action '{mapping.Action.Name}'");
                    
                    // Execute the mapped action immediately
                    _ = _executionService.ExecuteActionAsync(mapping.Action);
                    
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Action '{mapping.Action.Name}' execution started for key {keyName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow: No mapping found for key {keyName}");
                }
                
                // Update UI
                Dispatcher.Invoke(() =>
                {
                    // Cancel any pending reset operations
                    _testDisplayResetToken?.Cancel();
                    _testDisplayResetToken = new System.Threading.CancellationTokenSource();
                    
                    // Update test display with visual feedback
                    TestKeyDisplay.Text = $"✅ Key Detected: {keyName} (VK: {e.VirtualKeyCode})";
                    TestKeyDisplay.Background = System.Windows.Media.Brushes.LightGreen;
                    
                    if (mapping?.Action != null)
                    {
                        TestKeyDisplay.Text = $"✅ Key Detected: {keyName}\n🎯 Executed Action: {mapping.Action.Name}\n🚫 Input should be blocked";
                        UpdateStatus($"🎯 Key {keyName} → Executed: {mapping.Action.Name} (input blocking in progress)");
                    }
                    else
                    {
                        TestKeyDisplay.Text = $"✅ Key Detected: {keyName}\n⚠️ No action mapped to this key";
                        UpdateStatus($"🔑 Key detected: {keyName} (no mapping configured)");
                    }
                    
                    // Reset test display after 3 seconds
                    var currentToken = _testDisplayResetToken.Token;
                    Task.Delay(3000, currentToken).ContinueWith(task =>
                    {
                        if (!task.IsCanceled)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                TestKeyDisplay.Text = "Waiting for key press from your macro keyboard...";
                                TestKeyDisplay.Background = System.Windows.Media.Brushes.LightBlue;
                            });
                        }
                    }, TaskScheduler.Default);
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Key '{keyName}' from non-target device {e.DeviceId}, ignoring");
            }
        }
        
        private async void OnBlockingHookKeyDown(object? sender, KeyboardHookEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow.OnBlockingHookKeyDown: Key={e.Key}, VK={e.VirtualKeyCode}, Enabled={_currentConfig.IsEnabled}");
            
            if (!_currentConfig.IsEnabled) 
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Macro system disabled, allowing key through");
                return;
            }
            
            var keyName = e.Key.ToString();
            var now = DateTime.Now;
            
            // Check if this key was recently pressed on our macro keyboard
            bool shouldBlock = false;
            
            if (_recentMacroKeyPresses.ContainsKey(keyName))
            {
                var pressTime = _recentMacroKeyPresses[keyName];
                var timeDiff = now - pressTime;
                
                if (timeDiff <= _keyCorrelationWindow)
                {
                    shouldBlock = true;
                    _recentMacroKeyPresses.Remove(keyName); // Consume the correlation
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Found immediate correlation for {keyName} (time diff: {timeDiff.TotalMilliseconds}ms)");
                }
            }
            
            // If no correlation found, wait briefly for delayed Raw Input message
            // This handles the timing race condition where Hook arrives before Raw Input
            if (!shouldBlock && _mappedKeys.Contains(keyName))
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Key {keyName} is mapped but no correlation found, waiting for delayed Raw Input...");
                
                // Brief wait for potential delayed Raw Input message
                await Task.Delay(15); // Small delay to allow Raw Input to catch up
                
                // Check again after waiting
                if (_recentMacroKeyPresses.ContainsKey(keyName))
                {
                    var pressTime = _recentMacroKeyPresses[keyName];
                    var timeDiff = DateTime.Now - pressTime;
                    
                    if (timeDiff <= _keyCorrelationWindow)
                    {
                        shouldBlock = true;
                        _recentMacroKeyPresses.Remove(keyName);
                        System.Diagnostics.Debug.WriteLine($"MainWindow: Found delayed correlation for {keyName} after waiting (time diff: {timeDiff.TotalMilliseconds}ms)");
                    }
                }
            }
            
            if (shouldBlock)
            {
                // Check if this key has a mapping (double-check for safety)
                var mapping = _currentConfig.GetMappingForKey(keyName);
                
                if (mapping?.Action != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow: BLOCKING key {keyName} (correlated with macro keyboard press, mapped to '{mapping.Action.Name}')");
                    e.Handled = true;
                    
                    Dispatcher.BeginInvoke(() =>
                    {
                        UpdateStatus($"🚫 Key {keyName} BLOCKED from macro keyboard → Action: {mapping.Action.Name}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Key {keyName} was pressed on macro keyboard but has no mapping, allowing through");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Key {keyName} not correlated with recent macro keyboard press, allowing through");
            }
            
            // Clean up old correlation entries (do this less frequently to improve performance)
            if (_recentMacroKeyPresses.Count > 10) // Only clean up when we have many entries
            {
                var cutoff = now - _keyCorrelationWindow;
                var keysToRemove = _recentMacroKeyPresses.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    _recentMacroKeyPresses.Remove(key);
                }
                
                if (keysToRemove.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Cleaned up {keysToRemove.Count} old correlation entries");
                }
            }
        }
        
        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
            FooterText.Text = message;
        }
        
        // Event Handlers
        private void EnabledToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentConfig == null || _configService == null) return; // Skip during initialization
            
            _currentConfig.IsEnabled = true;
            _configService.SaveConfiguration(_currentConfig);
            UpdateStatus("Macro system enabled");
        }
        
        private void EnabledToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_currentConfig == null || _configService == null) return; // Skip during initialization
            
            _currentConfig.IsEnabled = false;
            _configService.SaveConfiguration(_currentConfig);
            UpdateStatus("Macro system disabled");
        }
        
        private void SetupKeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            SetupKeyboard();
        }
        
        private void ActionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = ActionsListView.SelectedItem != null;
            EditActionButton.IsEnabled = hasSelection;
            DeleteActionButton.IsEnabled = hasSelection;
        }
        
        private void MappingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = MappingsListView.SelectedItem != null;
            EditMappingButton.IsEnabled = hasSelection;
            DeleteMappingButton.IsEnabled = hasSelection;
        }
        
        private void NewAction_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ActionEditDialog();
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _actions.Add(dialog.Result);
                _configService.AddOrUpdateAction(_currentConfig, dialog.Result);
                UpdateStatus($"Added action: {dialog.Result.Name}");
            }
        }
        
        private void EditAction_Click(object sender, RoutedEventArgs e)
        {
            if (ActionsListView.SelectedItem is MacroAction selectedAction)
            {
                var dialog = new ActionEditDialog(selectedAction);
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    var index = _actions.IndexOf(selectedAction);
                    _actions[index] = dialog.Result;
                    _configService.AddOrUpdateAction(_currentConfig, dialog.Result);
                    UpdateStatus($"Updated action: {dialog.Result.Name}");
                }
            }
        }
        
        private void DeleteAction_Click(object sender, RoutedEventArgs e)
        {
            if (ActionsListView.SelectedItem is MacroAction selectedAction)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the action '{selectedAction.Name}'?\nThis will also remove any mappings that use this action.",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _actions.Remove(selectedAction);
                    _configService.RemoveAction(_currentConfig, selectedAction.Id);
                    LoadMappingsFromConfig(); // Refresh mappings as some may have been removed
                    UpdateStatus($"Deleted action: {selectedAction.Name}");
                }
            }
        }
        
        private void NewMapping_Click(object sender, RoutedEventArgs e)
        {
            if (_actions.Count == 0)
            {
                System.Windows.MessageBox.Show("Please create at least one action before adding mappings.", "No Actions Available", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            
            var dialog = new MappingEditDialog(_actions.ToList());
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                dialog.Result.KeyboardDeviceId = _currentConfig.MacroKeyboardDeviceId;
                dialog.Result.Action = _actions.FirstOrDefault(a => a.Id == dialog.Result.ActionId);
                
                _mappings.Add(dialog.Result);
                _configService.AddOrUpdateMapping(_currentConfig, dialog.Result);
                _mappedKeys.Add(dialog.Result.TriggerKey); // Add to preemptive blocking set
                UpdateStatus($"Added mapping: {dialog.Result.TriggerKey} → {dialog.Result.Action?.Name}");
            }
        }
        
        private void EditMapping_Click(object sender, RoutedEventArgs e)
        {
            if (MappingsListView.SelectedItem is MacroMapping selectedMapping)
            {
                var dialog = new MappingEditDialog(_actions.ToList(), selectedMapping);
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    dialog.Result.Action = _actions.FirstOrDefault(a => a.Id == dialog.Result.ActionId);
                    
                    var index = _mappings.IndexOf(selectedMapping);
                    _mappings[index] = dialog.Result;
                    _configService.AddOrUpdateMapping(_currentConfig, dialog.Result);
                    _mappedKeys.Add(dialog.Result.TriggerKey); // Add to preemptive blocking set
                    UpdateStatus($"Updated mapping: {dialog.Result.TriggerKey} → {dialog.Result.Action?.Name}");
                }
            }
        }
        
        private void DeleteMapping_Click(object sender, RoutedEventArgs e)
        {
            if (MappingsListView.SelectedItem is MacroMapping selectedMapping)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the mapping '{selectedMapping.TriggerKey} → {selectedMapping.Action?.Name}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _mappings.Remove(selectedMapping);
                    _configService.RemoveMapping(_currentConfig, selectedMapping.Id);
                    _mappedKeys.Remove(selectedMapping.TriggerKey); // Remove from preemptive blocking set
                    UpdateStatus($"Deleted mapping: {selectedMapping.TriggerKey}");
                }
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _testDisplayResetToken?.Cancel();
            _testDisplayResetToken?.Dispose();
            _blockingHook?.StopHook();
            _rawInputHook?.Dispose();
            base.OnClosed(e);
        }
    }
}