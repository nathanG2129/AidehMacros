using System;

namespace AidehMacros.Models
{
    public class MacroMapping
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TriggerKey { get; set; } = string.Empty; // e.g., "F1", "1", "NumPad1"
        public string ActionId { get; set; } = string.Empty;
        public string KeyboardDeviceId { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        
        public MacroAction? Action { get; set; } // For easy access, not persisted
        
        public override string ToString()
        {
            return $"{TriggerKey} → {Action?.Name ?? "Unknown Action"}";
        }
    }
} 