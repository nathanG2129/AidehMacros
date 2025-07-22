using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using AidehMacros.Models;
using AidehMacros.Services;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace AidehMacros.Controls
{
    public partial class KeyboardVisual : UserControl
    {
        private readonly Dictionary<string, Button> _keyButtons = new();
        private Configuration? _currentConfig;
        private ConfigurationService? _configService;
        
        public event EventHandler<KeyAssignmentEventArgs>? KeyAssignmentRequested;
        
        public KeyboardVisual()
        {
            InitializeComponent();
            Loaded += KeyboardVisual_Loaded;
            IsVisibleChanged += KeyboardVisual_IsVisibleChanged;
        }
        
        private void KeyboardVisual_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("KeyboardVisual.KeyboardVisual_Loaded: Control loaded, initializing key mappings");
            InitializeKeyMappings();
        }
        
        private void KeyboardVisual_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && _currentConfig != null)
            {
                System.Diagnostics.Debug.WriteLine("KeyboardVisual.KeyboardVisual_IsVisibleChanged: Control became visible, refreshing visuals");
                
                // Ensure key mappings are initialized
                if (_keyButtons.Count == 0)
                {
                    InitializeKeyMappings();
                }
                
                // Refresh the visuals
                UpdateKeyVisuals();
            }
        }
        
        private void InitializeKeyMappings()
        {
            // Find all buttons with tags and map them to their key names
            FindKeyButtons(this);
            System.Diagnostics.Debug.WriteLine($"KeyboardVisual.InitializeKeyMappings: Found {_keyButtons.Count} key buttons");
            
            if (_keyButtons.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.InitializeKeyMappings: Sample keys: {string.Join(", ", _keyButtons.Keys.Take(10))}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.InitializeKeyMappings: WARNING - No key buttons found!");
            }
        }
        
        private void FindKeyButtons(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button && button.Tag is string keyName)
                {
                    _keyButtons[keyName] = button;
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisual.FindKeyButtons: Found button for key '{keyName}'");
                }
                
                FindKeyButtons(child);
            }
        }
        
        public void SetConfiguration(Configuration config, ConfigurationService configService)
        {
            System.Diagnostics.Debug.WriteLine($"KeyboardVisual.SetConfiguration: Setting configuration (config: {config != null}, service: {configService != null})");
            _currentConfig = config;
            _configService = configService;
            
            // If key buttons haven't been found yet, the control may not be loaded
            if (_keyButtons.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("KeyboardVisual.SetConfiguration: Key buttons not found yet, trying to initialize");
                InitializeKeyMappings();
            }
            
            UpdateKeyVisuals();
        }
        
        private void UpdateKeyVisuals()
        {
            if (_currentConfig?.Mappings == null) 
            {
                System.Diagnostics.Debug.WriteLine("KeyboardVisual.UpdateKeyVisuals: No config or mappings available");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Processing {_currentConfig.Mappings.Count} mappings");
            
            // Reset all keys to default style
            try
            {
                var defaultStyle = (Style?)FindResource("KeyButtonStyle");
                if (defaultStyle == null)
                {
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: ERROR - KeyButtonStyle not found!");
                    return;
                }
                
                foreach (var button in _keyButtons.Values)
                {
                    button.Style = defaultStyle;
                    button.ToolTip = null;
                }
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Reset {_keyButtons.Count} buttons to default style");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: ERROR resetting styles: {ex.Message}");
                return;
            }
            
            // Update keys that have assignments
            try
            {
                var assignedStyle = (Style?)FindResource("AssignedKeyStyle");
                if (assignedStyle == null)
                {
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: ERROR - AssignedKeyStyle not found!");
                    return;
                }
                
                foreach (var mapping in _currentConfig.Mappings.Where(m => m.IsEnabled))
                {
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Processing mapping for key '{mapping.TriggerKey}', ActionId: '{mapping.ActionId}'");
                    
                    if (_keyButtons.TryGetValue(mapping.TriggerKey, out var button))
                    {
                        System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Found button for key '{mapping.TriggerKey}', setting to assigned style");
                        button.Style = assignedStyle;
                        
                        // Verify the style was applied
                        var appliedBackground = button.Background;
                        System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Button background after style applied: {appliedBackground}");
                        
                        if (mapping.Action != null)
                        {
                            var tooltip = $"Key: {mapping.TriggerKey}\n" +
                                         $"Action: {mapping.Action.Name}\n" +
                                         $"Type: {mapping.Action.Type}\n" +
                                         $"Details: {mapping.Action.Data}";
                            button.ToolTip = tooltip;
                            System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Set tooltip for key '{mapping.TriggerKey}'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Warning - mapping for key '{mapping.TriggerKey}' has no action reference");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Warning - No button found for key '{mapping.TriggerKey}'. Available keys: {string.Join(", ", _keyButtons.Keys.Take(10))}...");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: ERROR applying assigned styles: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine($"KeyboardVisual.UpdateKeyVisuals: Update complete");
        }
        
        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string keyName)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.KeyButton_Click: Key '{keyName}' clicked");
                
                // Find existing mapping for this key
                var existingMapping = _currentConfig?.Mappings?.FirstOrDefault(m => m.TriggerKey == keyName);
                if (existingMapping != null)
                {
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisual.KeyButton_Click: Found existing mapping for key '{keyName}' with action '{existingMapping.Action?.Name ?? "NULL"}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"KeyboardVisual.KeyButton_Click: No existing mapping found for key '{keyName}'");
                }
                
                // Raise event to request key assignment
                System.Diagnostics.Debug.WriteLine($"KeyboardVisual.KeyButton_Click: Raising KeyAssignmentRequested event for key '{keyName}'");
                KeyAssignmentRequested?.Invoke(this, new KeyAssignmentEventArgs 
                { 
                    KeyName = keyName, 
                    ExistingMapping = existingMapping 
                });
            }
        }
        
        public void RefreshVisuals()
        {
            UpdateKeyVisuals();
        }
        
        public void HighlightKey(string keyName, bool highlight = true)
        {
            if (_keyButtons.TryGetValue(keyName, out var button))
            {
                if (highlight)
                {
                    button.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 59)); // Yellow highlight
                }
                else
                {
                    UpdateKeyVisuals(); // Reset to normal state
                }
            }
        }
        
        public List<string> GetAssignedKeys()
        {
            if (_currentConfig?.Mappings == null)
                return new List<string>();
                
            return _currentConfig.Mappings
                .Where(m => m.IsEnabled)
                .Select(m => m.TriggerKey)
                .ToList();
        }
        
        public int GetAssignedKeyCount()
        {
            return GetAssignedKeys().Count;
        }
    }
    
    public class KeyAssignmentEventArgs : EventArgs
    {
        public string KeyName { get; set; } = string.Empty;
        public MacroMapping? ExistingMapping { get; set; }
    }
} 