using System;

namespace AidehMacros.Models
{
    public class KeyboardDevice
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsMacroKeyboard { get; set; }
        
        public override string ToString()
        {
            return $"{Name} ({Manufacturer})";
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is KeyboardDevice other)
            {
                return DeviceId == other.DeviceId;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return DeviceId.GetHashCode();
        }
    }
} 