using System;
using System.Collections.Generic;

namespace AidehMacros.Models
{
    public enum ActionType
    {
        KeyCombination,
        SendText,
        RunCommand
    }
    
    public class MacroAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ActionType Type { get; set; }
        public string Data { get; set; } = string.Empty; // JSON or simple string depending on type
        public bool IsEnabled { get; set; } = true;
        
        // For key combinations like "Ctrl+Shift+F3"
        public List<string> KeyCombination { get; set; } = new List<string>();
        
        public override string ToString()
        {
            return Name;
        }
    }
} 