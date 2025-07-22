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
            InitializeKeyMappings();
        }
        
        private void InitializeKeyMappings()
        {
            // Find all buttons with tags and map them to their key names
            FindKeyButtons(this);
        }
        
        private void FindKeyButtons(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button && button.Tag is string keyName)
                {
                    _keyButtons[keyName] = button;
                }
                
                FindKeyButtons(child);
            }
        }
        
        public void SetConfiguration(Configuration config, ConfigurationService configService)
        {
            _currentConfig = config;
            _configService = configService;
            UpdateKeyVisuals();
        }
        
        private void UpdateKeyVisuals()
        {
            if (_currentConfig?.Mappings == null) return;
            
            // Reset all keys to default style
            foreach (var button in _keyButtons.Values)
            {
                button.Style = (Style)FindResource("KeyButtonStyle");
                button.ToolTip = null;
            }
            
            // Update keys that have assignments
            foreach (var mapping in _currentConfig.Mappings.Where(m => m.IsEnabled))
            {
                if (_keyButtons.TryGetValue(mapping.TriggerKey, out var button))
                {
                    button.Style = (Style)FindResource("AssignedKeyStyle");
                    
                    if (mapping.Action != null)
                    {
                        var tooltip = $"Key: {mapping.TriggerKey}\n" +
                                     $"Action: {mapping.Action.Name}\n" +
                                     $"Type: {mapping.Action.Type}\n" +
                                     $"Details: {mapping.Action.Data}";
                        button.ToolTip = tooltip;
                    }
                }
            }
        }
        
        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string keyName)
            {
                // Find existing mapping for this key
                var existingMapping = _currentConfig?.Mappings?.FirstOrDefault(m => m.TriggerKey == keyName);
                
                // Raise event to request key assignment
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