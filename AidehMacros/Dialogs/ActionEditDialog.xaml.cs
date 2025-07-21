using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AidehMacros.Models;

namespace AidehMacros
{
    public partial class ActionEditDialog : Window
    {
        public MacroAction? Result { get; private set; }
        
        private readonly ObservableCollection<string> _selectedKeys;
        private readonly List<string> _availableKeys;
        private readonly MacroAction? _editingAction;
        
        public ActionEditDialog(MacroAction? actionToEdit = null)
        {
            InitializeComponent();
            
            _editingAction = actionToEdit;
            _selectedKeys = new ObservableCollection<string>();
            _availableKeys = GetAvailableKeys();
            
            InitializeDialog();
        }
        
        private void InitializeDialog()
        {
            // Setup key selection
            KeySelectionComboBox.ItemsSource = _availableKeys;
            SelectedKeysListBox.ItemsSource = _selectedKeys;
            
            // Set default type
            TypeComboBox.SelectedIndex = 0;
            
            // If editing an existing action, populate fields
            if (_editingAction != null)
            {
                NameTextBox.Text = _editingAction.Name;
                EnabledCheckBox.IsChecked = _editingAction.IsEnabled;
                
                // Set type and data based on action type
                switch (_editingAction.Type)
                {
                    case ActionType.KeyCombination:
                        TypeComboBox.SelectedIndex = 0;
                        foreach (var key in _editingAction.KeyCombination)
                        {
                            _selectedKeys.Add(key);
                        }
                        break;
                        
                    case ActionType.SendText:
                        TypeComboBox.SelectedIndex = 1;
                        SendTextTextBox.Text = _editingAction.Data;
                        break;
                        
                    case ActionType.RunCommand:
                        TypeComboBox.SelectedIndex = 2;
                        RunCommandTextBox.Text = _editingAction.Data;
                        break;
                }
            }
            
            UpdatePanelVisibility();
        }
        
        private List<string> GetAvailableKeys()
        {
            return new List<string>
            {
                // Modifier keys
                "Ctrl", "Alt", "Shift", "Win",
                
                // Function keys
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                
                // Number keys
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
                
                // Letter keys
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                
                // Special keys
                "Space", "Enter", "Tab", "Escape", "Backspace", "Delete",
                "Home", "End", "PageUp", "PageDown",
                "Up", "Down", "Left", "Right",
                
                // Numpad
                "NumPad0", "NumPad1", "NumPad2", "NumPad3", "NumPad4",
                "NumPad5", "NumPad6", "NumPad7", "NumPad8", "NumPad9"
            };
        }
        
        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanelVisibility();
        }
        
        private void UpdatePanelVisibility()
        {
            if (TypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var type = selectedItem.Tag?.ToString();
                
                KeyCombinationPanel.Visibility = type == "KeyCombination" ? Visibility.Visible : Visibility.Collapsed;
                SendTextPanel.Visibility = type == "SendText" ? Visibility.Visible : Visibility.Collapsed;
                RunCommandPanel.Visibility = type == "RunCommand" ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        private void AddKey_Click(object sender, RoutedEventArgs e)
        {
            if (KeySelectionComboBox.SelectedItem is string selectedKey)
            {
                if (!_selectedKeys.Contains(selectedKey))
                {
                    _selectedKeys.Add(selectedKey);
                }
                KeySelectionComboBox.SelectedItem = null;
            }
        }
        
        private void RemoveKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string keyToRemove)
            {
                _selectedKeys.Remove(keyToRemove);
            }
        }
        
        private void ExampleCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string command)
            {
                RunCommandTextBox.Text = command;
            }
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                var action = _editingAction != null ? 
                    new MacroAction { Id = _editingAction.Id } : 
                    new MacroAction();
                
                action.Name = NameTextBox.Text.Trim();
                action.IsEnabled = EnabledCheckBox.IsChecked == true;
                
                if (TypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var typeString = selectedItem.Tag?.ToString();
                    action.Type = Enum.Parse<ActionType>(typeString!);
                    
                    switch (action.Type)
                    {
                        case ActionType.KeyCombination:
                            action.KeyCombination = _selectedKeys.ToList();
                            action.Data = string.Join("+", _selectedKeys);
                            break;
                            
                        case ActionType.SendText:
                            action.Data = SendTextTextBox.Text;
                            break;
                            
                        case ActionType.RunCommand:
                            action.Data = RunCommandTextBox.Text.Trim();
                            break;
                    }
                }
                
                Result = action;
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
                                    if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please enter a name for the action.", "Validation Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }
            
            if (TypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var type = selectedItem.Tag?.ToString();
                
                switch (type)
                {
                    case "KeyCombination":
                        if (_selectedKeys.Count == 0)
                        {
                            System.Windows.MessageBox.Show("Please add at least one key for the combination.", "Validation Error", 
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            return false;
                        }
                        break;
                        
                    case "SendText":
                        if (string.IsNullOrWhiteSpace(SendTextTextBox.Text))
                        {
                            System.Windows.MessageBox.Show("Please enter text to send.", "Validation Error", 
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            return false;
                        }
                        break;
                        
                    case "RunCommand":
                        if (string.IsNullOrWhiteSpace(RunCommandTextBox.Text))
                        {
                            System.Windows.MessageBox.Show("Please enter a command to run.", "Validation Error", 
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            return false;
                        }
                        break;
                }
            }
            
            return true;
        }
    }
} 