using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AidehMacros.Models;

namespace AidehMacros
{
    public partial class MappingEditDialog : Window
    {
        public MacroMapping? Result { get; private set; }
        
        private readonly List<MacroAction> _availableActions;
        private readonly List<string> _availableKeys;
        private readonly MacroMapping? _editingMapping;
        
        public MappingEditDialog(List<MacroAction> availableActions, MacroMapping? mappingToEdit = null)
        {
            InitializeComponent();
            
            _availableActions = availableActions;
            _editingMapping = mappingToEdit;
            _availableKeys = GetAvailableKeys();
            
            InitializeDialog();
        }
        
        private void InitializeDialog()
        {
            // Setup trigger key selection
            TriggerKeyComboBox.ItemsSource = _availableKeys;
            TriggerKeyComboBox.SelectionChanged += TriggerKeyComboBox_SelectionChanged;
            
            // Setup action selection
            ActionComboBox.ItemsSource = _availableActions;
            ActionComboBox.SelectionChanged += ActionComboBox_SelectionChanged;
            
            // If editing an existing mapping, populate fields
            if (_editingMapping != null)
            {
                TriggerKeyComboBox.SelectedItem = _editingMapping.TriggerKey;
                
                var action = _availableActions.FirstOrDefault(a => a.Id == _editingMapping.ActionId);
                if (action != null)
                {
                    ActionComboBox.SelectedItem = action;
                }
                
                EnabledCheckBox.IsChecked = _editingMapping.IsEnabled;
            }
            
            UpdatePreview();
        }
        
        private List<string> GetAvailableKeys()
        {
            return new List<string>
            {
                // Function keys (most common for macro keyboards)
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                
                // Number keys
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                
                // Letter keys
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                
                // Numpad keys
                "NumPad0", "NumPad1", "NumPad2", "NumPad3", "NumPad4",
                "NumPad5", "NumPad6", "NumPad7", "NumPad8", "NumPad9",
                "NumPadAdd", "NumPadSubtract", "NumPadMultiply", "NumPadDivide",
                "NumPadDecimal", "NumPadEnter",
                
                // Special keys
                "Space", "Enter", "Tab", "Escape", "Backspace", "Delete",
                "Home", "End", "PageUp", "PageDown",
                "Up", "Down", "Left", "Right",
                "Insert", "PrintScreen", "ScrollLock", "Pause"
            };
        }
        
        private void TriggerKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }
        
        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActionPreview();
            UpdatePreview();
        }
        
        private void UpdateActionPreview()
        {
            if (ActionComboBox.SelectedItem is MacroAction selectedAction)
            {
                ActionPreviewBorder.Visibility = Visibility.Visible;
                
                var previewText = selectedAction.Type switch
                {
                    ActionType.KeyCombination => $"Key Combination: {selectedAction.Data}",
                    ActionType.SendText => $"Send Text: \"{selectedAction.Data}\"",
                    ActionType.RunCommand => $"Run Command: {selectedAction.Data}",
                    _ => "Unknown action type"
                };
                
                ActionPreviewText.Text = previewText;
            }
            else
            {
                ActionPreviewBorder.Visibility = Visibility.Collapsed;
            }
        }
        
        private void UpdatePreview()
        {
            var triggerKey = TriggerKeyComboBox.SelectedItem as string ?? "?";
            var actionName = (ActionComboBox.SelectedItem as MacroAction)?.Name ?? "?";
            
            PreviewTriggerKey.Text = triggerKey;
            PreviewActionName.Text = actionName;
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                var mapping = _editingMapping != null ? 
                    new MacroMapping { Id = _editingMapping.Id } : 
                    new MacroMapping();
                
                mapping.TriggerKey = TriggerKeyComboBox.SelectedItem as string ?? string.Empty;
                mapping.ActionId = (ActionComboBox.SelectedItem as MacroAction)?.Id ?? string.Empty;
                mapping.IsEnabled = EnabledCheckBox.IsChecked == true;
                
                Result = mapping;
                DialogResult = true;
                Close();
            }
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private bool ValidateInput()
        {
            if (TriggerKeyComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Please select a trigger key.", "Validation Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }
            
            if (ActionComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Please select an action to execute.", "Validation Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }
    }
} 