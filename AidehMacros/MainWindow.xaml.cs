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
        private readonly RawInputKeyboardHook _keyboardHook = null!;
        
        private Configuration _currentConfig = null!;
        private string? _detectedKeyboardId = null;
        private ObservableCollection<MacroAction> _actions = null!;
        private ObservableCollection<MacroMapping> _mappings = null!;
        private System.Threading.CancellationTokenSource? _testDisplayResetToken;
        
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
                _keyboardHook = new RawInputKeyboardHook();
                System.Diagnostics.Debug.WriteLine("RawInputKeyboardHook created");
                
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
            // Initialize the keyboard hook when the window is loaded
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            System.Diagnostics.Debug.WriteLine($"MainWindow.Window_Loaded: Initializing keyboard hook with handle {windowHandle:X8}");
            
            if (_keyboardHook.Initialize(windowHandle))
            {
                _keyboardHook.KeyDown += OnKeyboardHookKeyDown;
                
                // Set the target device if we have one
                if (!string.IsNullOrEmpty(_detectedKeyboardId))
                {
                    _keyboardHook.SetTargetDevice(_detectedKeyboardId);
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Set target device to {_detectedKeyboardId}");
                }
                
                System.Diagnostics.Debug.WriteLine("MainWindow: Raw Input keyboard hook started successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Failed to start keyboard hook");
                UpdateStatus("Warning: Keyboard detection failed to initialize");
            }
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
            foreach (var mapping in _currentConfig.Mappings)
            {
                mapping.Action = _currentConfig.Actions.FirstOrDefault(a => a.Id == mapping.ActionId);
                _mappings.Add(mapping);
            }
        }
        
        private void SetupKeyboard()
        {
            // Temporarily dispose the main window's hook to avoid conflicts
            _keyboardHook?.Dispose();
            System.Diagnostics.Debug.WriteLine("MainWindow.SetupKeyboard: Disposed main hook before detection");
            
            var dialog = new KeyboardDetectionDialog();
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true && dialog.WasDetected && !string.IsNullOrEmpty(dialog.DetectedKeyboardId))
            {
                _detectedKeyboardId = dialog.DetectedKeyboardId;
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Detected keyboard ID: {_detectedKeyboardId}");
                
                // Re-initialize the main window's keyboard hook
                var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (_keyboardHook.Initialize(windowHandle))
                {
                    _keyboardHook.KeyDown += OnKeyboardHookKeyDown;
                    _keyboardHook.SetTargetDevice(_detectedKeyboardId);
                    System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Re-initialized main hook with target device");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Failed to re-initialize main hook");
                }
                
                // Save to configuration
                _configService.SetMacroKeyboard(_currentConfig, _detectedKeyboardId ?? string.Empty);
                _configService.SaveConfiguration(_currentConfig);
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Saved configuration");
                
                // Update UI
                KeyboardStatusText.Text = $"Configured: {_detectedKeyboardId}";
                UpdateStatus($"Macro keyboard configured: {_detectedKeyboardId} - Ready for testing!");
                
                // Clear the test display to show it's ready for new input
                TestKeyDisplay.Text = "Keyboard configured! Press any key on your macro keyboard to test...";
                TestKeyDisplay.Background = System.Windows.Media.Brushes.LightGreen;
                
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: UI updated, ready for testing");
            }
            else
            {
                // If detection was cancelled, re-initialize the hook anyway
                var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (_keyboardHook.Initialize(windowHandle))
                {
                    _keyboardHook.KeyDown += OnKeyboardHookKeyDown;
                    if (!string.IsNullOrEmpty(_detectedKeyboardId))
                    {
                        _keyboardHook.SetTargetDevice(_detectedKeyboardId);
                    }
                    System.Diagnostics.Debug.WriteLine($"MainWindow.SetupKeyboard: Re-initialized main hook after cancelled detection");
                }
            }
        }
        
        private void OnKeyboardHookKeyDown(object? sender, RawKeyboardEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow.OnKeyboardHookKeyDown called: Key={e.Key}, Device={e.DeviceId}, IsTarget={e.IsFromTargetDevice}, Enabled={_currentConfig.IsEnabled}");
            
            if (!_currentConfig.IsEnabled) 
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Macro system disabled, ignoring key");
                return;
            }
            
            // Only process keys from our target keyboard
            if (!e.IsFromTargetDevice) 
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Key not from target device, ignoring");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("MainWindow: Processing key from target device");
            
            Dispatcher.Invoke(() =>
            {
                // Cancel any pending reset operations
                _testDisplayResetToken?.Cancel();
                _testDisplayResetToken = new System.Threading.CancellationTokenSource();
                
                // Update test display with visual feedback
                var keyName = e.Key.ToString();
                TestKeyDisplay.Text = $"✅ Key Detected: {keyName} (VK: {e.VirtualKeyCode})";
                TestKeyDisplay.Background = System.Windows.Media.Brushes.LightGreen;
                
                // Check if this key has a mapping
                var mapping = _currentConfig.GetMappingForKey(keyName);
                
                if (mapping?.Action != null)
                {
                    // Execute the mapped action
                    _ = _executionService.ExecuteActionAsync(mapping.Action);
                    UpdateStatus($"✅ Key {keyName} pressed → Executed: {mapping.Action.Name}");
                    
                    // Show mapping feedback immediately (no delay)
                    TestKeyDisplay.Text = $"✅ Key Detected: {keyName}\n🎯 Mapped Action: {mapping.Action.Name}";
                }
                else
                {
                    UpdateStatus($"🔑 Key pressed: {keyName} (no mapping configured)");
                    
                    // Show no mapping feedback immediately (no delay)
                    TestKeyDisplay.Text = $"✅ Key Detected: {keyName}\n⚠️ No action mapped to this key";
                }
                
                // Reset test display after 3 seconds (with cancellation support)
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
                    UpdateStatus($"Deleted mapping: {selectedMapping.TriggerKey}");
                }
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _testDisplayResetToken?.Cancel();
            _testDisplayResetToken?.Dispose();
            _keyboardHook?.Dispose();
            base.OnClosed(e);
        }
    }
}