using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AidehMacros.Models;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace AidehMacros.Dialogs
{
    public partial class KeyAssignmentDialog : Window
    {
        public MacroAction? ResultAction { get; private set; }
        public MacroMapping? ResultMapping { get; private set; }
        public bool ShouldRemove { get; private set; } = false;
        
        private readonly string _keyName;
        private readonly MacroMapping? _existingMapping;
        private readonly List<string> _availableKeys;
        
        public KeyAssignmentDialog(string keyName, MacroMapping? existingMapping = null)
        {
            InitializeComponent();
            
            _keyName = keyName;
            _existingMapping = existingMapping;
            _availableKeys = GetAvailableKeys();
            
            InitializeDialog();
        }
        
        private void InitializeDialog()
        {
            // Set key display
            if (KeyDisplayText != null)
                KeyDisplayText.Text = _keyName;
            Title = $"Assign Macro to {_keyName}";
            
            // Setup key selection for combinations
            if (KeySelectionComboBox != null)
            {
                KeySelectionComboBox.ItemsSource = _availableKeys;
                KeySelectionComboBox.SelectionChanged += (s, e) => UpdateKeyCombinationPreview();
            }
            
            // Handle existing assignment
            if (_existingMapping?.Action != null)
            {
                if (ExistingAssignmentPanel != null)
                    ExistingAssignmentPanel.Visibility = Visibility.Visible;
                if (ExistingAssignmentText != null)
                    ExistingAssignmentText.Text = $"{_existingMapping.Action.Name} ({_existingMapping.Action.Type}: {_existingMapping.Action.Data})";
                if (RemoveButton != null)
                    RemoveButton.Visibility = Visibility.Visible;
                
                // Pre-populate with existing data
                if (ActionNameTextBox != null)
                    ActionNameTextBox.Text = _existingMapping.Action.Name;
                
                if (ActionTypeComboBox != null)
                {
                    switch (_existingMapping.Action.Type)
                    {
                        case ActionType.KeyCombination:
                            ActionTypeComboBox.SelectedIndex = 0;
                            LoadExistingKeyCombination(_existingMapping.Action.KeyCombination);
                            break;
                        case ActionType.SendText:
                            ActionTypeComboBox.SelectedIndex = 1;
                            if (SendTextTextBox != null)
                                SendTextTextBox.Text = _existingMapping.Action.Data;
                            break;
                        case ActionType.RunCommand:
                            ActionTypeComboBox.SelectedIndex = 2;
                            if (RunCommandTextBox != null)
                                RunCommandTextBox.Text = _existingMapping.Action.Data;
                            break;
                    }
                }
            }
            else
            {
                // Set default name for new assignments
                if (ActionNameTextBox != null)
                    ActionNameTextBox.Text = $"{_keyName} Macro";
                    
                // Set default key selection for new key combinations
                if (KeySelectionComboBox != null && _availableKeys.Contains(_keyName))
                {
                    KeySelectionComboBox.SelectedItem = _keyName;
                    System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.InitializeDialog: Set default key selection to '{_keyName}'");
                }
            }
            
            UpdatePanelVisibility();
            UpdateKeyCombinationPreview();
        }
        
        private void LoadExistingKeyCombination(List<string> keys)
        {
            // Set modifier toggles (only if UI elements exist)
            if (CtrlToggle != null)
                CtrlToggle.IsChecked = keys.Any(k => k.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || k.Equals("Control", StringComparison.OrdinalIgnoreCase));
            if (AltToggle != null)
                AltToggle.IsChecked = keys.Any(k => k.Equals("Alt", StringComparison.OrdinalIgnoreCase));
            if (ShiftToggle != null)
                ShiftToggle.IsChecked = keys.Any(k => k.Equals("Shift", StringComparison.OrdinalIgnoreCase));
            if (WinToggle != null)
                WinToggle.IsChecked = keys.Any(k => k.Equals("Win", StringComparison.OrdinalIgnoreCase) || k.Equals("Windows", StringComparison.OrdinalIgnoreCase));
            
            // Set main key
            var mainKey = keys.FirstOrDefault(k => !IsModifierKey(k));
            if (mainKey != null && KeySelectionComboBox != null)
            {
                KeySelectionComboBox.SelectedItem = mainKey;
            }
        }
        
        private bool IsModifierKey(string key)
        {
            return key.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("Control", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("Alt", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("Shift", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("Windows", StringComparison.OrdinalIgnoreCase);
        }
        
        private List<string> GetAvailableKeys()
        {
            return new List<string>
            {
                // Function keys (most popular for macros)
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                
                // Letters
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                
                // Numbers (using the same Tag values as in KeyboardVisual.xaml)
                "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0",
                
                // Special keys (using the same Tag values as in KeyboardVisual.xaml)
                "Space", "Return", "Tab", "Escape", "Back", "Delete",
                "Home", "End", "PageUp", "PageDown",
                "Up", "Down", "Left", "Right",
                "Insert", "PrintScreen",
                
                // Additional keys from the visual
                "OemTilde", "OemMinus", "OemPlus", "OemOpenBrackets", "OemCloseBrackets", 
                "OemBackslash", "CapsLock", "OemSemicolon", "OemQuotes", "LeftShift", "RightShift",
                "OemComma", "OemPeriod", "OemQuestion", "LeftCtrl", "RightCtrl", "LeftAlt", "RightAlt",
                "LWin", "RWin", "Apps",
                
                // Numpad
                "NumPad0", "NumPad1", "NumPad2", "NumPad3", "NumPad4", "NumPad5", 
                "NumPad6", "NumPad7", "NumPad8", "NumPad9", "NumLock", "Divide", 
                "Multiply", "Subtract", "Add", "Decimal"
            };
        }
        
        private void ActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanelVisibility();
        }
        
        private void UpdatePanelVisibility()
        {
            if (ActionTypeComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                var type = selectedItem.Tag?.ToString();
                
                // Only update panels if they exist (XAML is loaded)
                if (KeyCombinationPanel != null)
                    KeyCombinationPanel.Visibility = type == "KeyCombination" ? Visibility.Visible : Visibility.Collapsed;
                if (SendTextPanel != null)
                    SendTextPanel.Visibility = type == "SendText" ? Visibility.Visible : Visibility.Collapsed;
                if (RunCommandPanel != null)
                    RunCommandPanel.Visibility = type == "RunCommand" ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        private void ModifierToggle_Click(object sender, RoutedEventArgs e)
        {
            UpdateKeyCombinationPreview();
        }
        
        private void UpdateKeyCombinationPreview()
        {
            // Only update if UI elements are loaded
            if (KeyCombinationPreview == null || CtrlToggle == null || AltToggle == null || 
                ShiftToggle == null || WinToggle == null || KeySelectionComboBox == null)
                return;
                
            var combination = new List<string>();
            
            if (CtrlToggle.IsChecked == true) combination.Add("Ctrl");
            if (AltToggle.IsChecked == true) combination.Add("Alt");
            if (ShiftToggle.IsChecked == true) combination.Add("Shift");
            if (WinToggle.IsChecked == true) combination.Add("Win");
            
            if (KeySelectionComboBox.SelectedItem is string selectedKey)
            {
                combination.Add(selectedKey);
            }
            
            KeyCombinationPreview.Text = combination.Count > 0 ? string.Join(" + ", combination) : "(No combination selected)";
        }
        
        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string quickAction)
            {
                ActionTypeComboBox.SelectedIndex = 0; // Key Combination
                
                // Parse quick action and set up the combination
                var parts = quickAction.Split('+');
                
                CtrlToggle.IsChecked = parts.Any(p => p.Trim().Equals("Ctrl", StringComparison.OrdinalIgnoreCase));
                AltToggle.IsChecked = parts.Any(p => p.Trim().Equals("Alt", StringComparison.OrdinalIgnoreCase));
                ShiftToggle.IsChecked = parts.Any(p => p.Trim().Equals("Shift", StringComparison.OrdinalIgnoreCase));
                WinToggle.IsChecked = parts.Any(p => p.Trim().Equals("Win", StringComparison.OrdinalIgnoreCase));
                
                var mainKey = parts.LastOrDefault()?.Trim();
                if (mainKey != null && _availableKeys.Contains(mainKey))
                {
                    KeySelectionComboBox.SelectedItem = mainKey;
                }
                
                // Set a default name based on the quick action
                if (string.IsNullOrEmpty(ActionNameTextBox.Text) || ActionNameTextBox.Text == $"{_keyName} Macro")
                {
                    ActionNameTextBox.Text = button.Content.ToString()?.Split('(')[0]?.Trim() ?? $"{_keyName} Macro";
                }
                
                UpdateKeyCombinationPreview();
            }
        }
        
        private void CommandExample_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string command)
            {
                RunCommandTextBox.Text = command;
            }
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Starting save process for key '{_keyName}'");
            
            if (ValidateInput())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Validation passed, creating action and mapping");
                    
                    // Create the action
                    var action = _existingMapping?.Action != null ? 
                        new MacroAction { Id = _existingMapping.Action.Id } : 
                        new MacroAction();
                    
                    action.Name = ActionNameTextBox.Text.Trim();
                    action.IsEnabled = true;
                    
                    if (ActionTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        var typeString = selectedItem.Tag?.ToString();
                        action.Type = Enum.Parse<ActionType>(typeString!);
                        
                        switch (action.Type)
                        {
                            case ActionType.KeyCombination:
                                var combination = new List<string>();
                                if (CtrlToggle?.IsChecked == true) combination.Add("Ctrl");
                                if (AltToggle?.IsChecked == true) combination.Add("Alt");
                                if (ShiftToggle?.IsChecked == true) combination.Add("Shift");
                                if (WinToggle?.IsChecked == true) combination.Add("Win");
                                if (KeySelectionComboBox?.SelectedItem is string selectedKey)
                                    combination.Add(selectedKey);
                                    
                                action.KeyCombination = combination;
                                action.Data = string.Join("+", combination);
                                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Created key combination: {action.Data}");
                                break;
                                
                            case ActionType.SendText:
                                action.Data = SendTextTextBox?.Text ?? "";
                                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Created send text action: '{action.Data}'");
                                break;
                                
                            case ActionType.RunCommand:
                                action.Data = RunCommandTextBox?.Text?.Trim() ?? "";
                                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Created run command action: '{action.Data}'");
                                break;
                        }
                    }
                    
                    // Create the mapping
                    var mapping = _existingMapping != null ? 
                        new MacroMapping { Id = _existingMapping.Id } : 
                        new MacroMapping();
                    
                    mapping.TriggerKey = _keyName;
                    mapping.ActionId = action.Id;
                    mapping.IsEnabled = true;
                    
                    System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Created mapping - Key: '{mapping.TriggerKey}', ActionId: '{mapping.ActionId}'");
                    
                    ResultAction = action;
                    ResultMapping = mapping;
                    
                    System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Setting DialogResult = true and closing");
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Exception occurred: {ex.Message}");
                    MessageBox.Show($"Error saving macro: {ex.Message}", "Save Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.SaveButton_Click: Validation failed");
            }
        }
        
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to remove the macro assignment from the {_keyName} key?",
                "Remove Assignment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ShouldRemove = true;
                DialogResult = true;
                Close();
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private bool ValidateInput()
        {
            System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Starting validation");
            
            // Check if UI elements are available
            if (ActionNameTextBox == null || ActionTypeComboBox == null)
            {
                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: UI elements not initialized");
                MessageBox.Show("Dialog not properly initialized.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(ActionNameTextBox.Text))
            {
                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Action name is empty");
                MessageBox.Show("Please enter a name for this macro.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (ActionTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var type = selectedItem.Tag?.ToString();
                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Selected type: {type}");
                
                switch (type)
                {
                    case "KeyCombination":
                        if (KeySelectionComboBox?.SelectedItem == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: No key selected for combination");
                            MessageBox.Show("Please select a key for the combination.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                        System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Key combination validation passed");
                        break;
                        
                    case "SendText":
                        if (SendTextTextBox == null || string.IsNullOrWhiteSpace(SendTextTextBox.Text))
                        {
                            System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Send text is empty");
                            MessageBox.Show("Please enter text to send.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                        System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Send text validation passed");
                        break;
                        
                    case "RunCommand":
                        if (RunCommandTextBox == null || string.IsNullOrWhiteSpace(RunCommandTextBox.Text))
                        {
                            System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Run command is empty");
                            MessageBox.Show("Please enter a command to run.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                        System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: Run command validation passed");
                        break;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: No macro type selected");
                MessageBox.Show("Please select a macro type.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine($"KeyAssignmentDialog.ValidateInput: All validation passed");
            return true;
        }
    }
} 