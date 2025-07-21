using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AidehMacros.Models;
using AidehMacros.Services;


namespace AidehMacros
{
    public partial class MainWindow : Window
    {
        private readonly KeyboardDeviceService _keyboardService = null!;
        private readonly ConfigurationService _configService = null!;
        private readonly MacroExecutionService _executionService = null!;
        private readonly LowLevelKeyboardHook _keyboardHook = null!;
        
        private Configuration _currentConfig = null!;
        private List<Models.KeyboardDevice> _availableKeyboards = null!;
        private ObservableCollection<MacroAction> _actions = null!;
        private ObservableCollection<MacroMapping> _mappings = null!;
        
        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow constructor started");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent completed");
                
                System.Diagnostics.Debug.WriteLine("Creating services...");
                System.Diagnostics.Debug.WriteLine("About to create KeyboardDeviceService...");
                _keyboardService = new KeyboardDeviceService();
                System.Diagnostics.Debug.WriteLine("KeyboardDeviceService created");
                
                System.Diagnostics.Debug.WriteLine("About to create ConfigurationService...");
                _configService = new ConfigurationService();
                System.Diagnostics.Debug.WriteLine("ConfigurationService created");
                
                System.Diagnostics.Debug.WriteLine("About to create MacroExecutionService...");
                _executionService = new MacroExecutionService();
                System.Diagnostics.Debug.WriteLine("MacroExecutionService created");
                
                System.Diagnostics.Debug.WriteLine("About to create LowLevelKeyboardHook...");
                _keyboardHook = new LowLevelKeyboardHook();
                System.Diagnostics.Debug.WriteLine("LowLevelKeyboardHook created");
                
                System.Diagnostics.Debug.WriteLine("Creating collections...");
                _availableKeyboards = new List<Models.KeyboardDevice>();
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
                _availableKeyboards = new List<Models.KeyboardDevice>();
                _actions = new ObservableCollection<MacroAction>();
                _mappings = new ObservableCollection<MacroMapping>();
                
                // Try to show the error to the user
                System.Windows.MessageBox.Show($"Application failed to initialize properly: {ex.Message}", 
                    "Initialization Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
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
            
                            // Setup keyboard hook
                _keyboardHook.KeyDown += OnKeyboardHookKeyDown;
                try
                {
                    _keyboardHook.StartHook();
                    System.Diagnostics.Debug.WriteLine("Keyboard hook started successfully");
                }
                catch (Exception hookEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start keyboard hook: {hookEx.Message}");
                    // Continue without the hook for now
                }
            
            // Load keyboards
            RefreshKeyboards();
            
                // Set initial state
                EnabledToggle.IsChecked = _currentConfig?.IsEnabled ?? true;
                UpdateStatus("Application loaded successfully");
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
        
        private void RefreshKeyboards()
        {
            try
            {
                _availableKeyboards = _keyboardService.GetConnectedKeyboards();
                KeyboardComboBox.ItemsSource = _availableKeyboards;
                
                // Select the previously configured keyboard if it exists
                if (!string.IsNullOrEmpty(_currentConfig.MacroKeyboardDeviceId))
                {
                    var selectedKeyboard = _availableKeyboards.FirstOrDefault(k => k.DeviceId == _currentConfig.MacroKeyboardDeviceId);
                    if (selectedKeyboard != null)
                    {
                        KeyboardComboBox.SelectedItem = selectedKeyboard;
                    }
                }
                
                UpdateStatus($"Found {_availableKeyboards.Count} keyboard(s)");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading keyboards: {ex.Message}");
            }
        }
        
        private void OnKeyboardHookKeyDown(object? sender, KeyboardHookEventArgs e)
        {
            if (!_currentConfig.IsEnabled) return;
            
            Dispatcher.Invoke(() =>
            {
                // Update test display
                TestKeyDisplay.Text = $"Last key pressed: {e.Key} (VK: {e.VirtualKeyCode})";
                
                // Check if this key has a mapping
                var keyName = e.Key.ToString();
                var mapping = _currentConfig.GetMappingForKey(keyName);
                
                if (mapping?.Action != null)
                {
                    // Execute the mapped action
                    _ = _executionService.ExecuteActionAsync(mapping.Action);
                    UpdateStatus($"Executed: {mapping.Action.Name}");
                    
                    // Suppress the key if it's from our macro keyboard
                    e.Handled = true;
                }
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
        
        private void RefreshKeyboards_Click(object sender, RoutedEventArgs e)
        {
            RefreshKeyboards();
        }
        
        private void KeyboardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentConfig == null || _configService == null) return; // Skip during initialization
            
            if (KeyboardComboBox.SelectedItem is Models.KeyboardDevice selectedKeyboard)
            {
                _configService.SetMacroKeyboard(_currentConfig, selectedKeyboard.DeviceId);
                SelectedKeyboardInfo.Text = $"Selected: {selectedKeyboard.Name} (ID: {selectedKeyboard.DeviceId})";
                UpdateStatus($"Macro keyboard set to: {selectedKeyboard.Name}");
            }
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
            _keyboardHook.StopHook();
            base.OnClosed(e);
        }
    }
}