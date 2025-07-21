using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AidehMacros.Models;
using AidehMacros.Services;
using ModernWpf.Controls;

namespace AidehMacros
{
    public partial class MainWindow : Window
    {
        private readonly KeyboardDeviceService _keyboardService;
        private readonly ConfigurationService _configService;
        private readonly MacroExecutionService _executionService;
        private readonly LowLevelKeyboardHook _keyboardHook;
        
        private Configuration _currentConfig = null!;
        private List<Models.KeyboardDevice> _availableKeyboards;
        private ObservableCollection<MacroAction> _actions;
        private ObservableCollection<MacroMapping> _mappings;
        
        public MainWindow()
        {
            InitializeComponent();
            
            _keyboardService = new KeyboardDeviceService();
            _configService = new ConfigurationService();
            _executionService = new MacroExecutionService();
            _keyboardHook = new LowLevelKeyboardHook();
            
            _availableKeyboards = new List<Models.KeyboardDevice>();
            _actions = new ObservableCollection<MacroAction>();
            _mappings = new ObservableCollection<MacroMapping>();
            
            InitializeApplication();
        }
        
        private void InitializeApplication()
        {
            // Load configuration
            _currentConfig = _configService.LoadConfiguration();
            
            // Setup data bindings
            ActionsListView.ItemsSource = _actions;
            MappingsListView.ItemsSource = _mappings;
            
            // Load data from config
            LoadActionsFromConfig();
            LoadMappingsFromConfig();
            
            // Setup keyboard hook
            _keyboardHook.KeyDown += OnKeyboardHookKeyDown;
            _keyboardHook.StartHook();
            
            // Load keyboards
            RefreshKeyboards();
            
            // Set initial state
            EnabledToggle.IsChecked = _currentConfig.IsEnabled;
            UpdateStatus("Application loaded successfully");
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
            _currentConfig.IsEnabled = true;
            _configService.SaveConfiguration(_currentConfig);
            UpdateStatus("Macro system enabled");
        }
        
        private void EnabledToggle_Unchecked(object sender, RoutedEventArgs e)
        {
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