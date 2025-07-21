using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using AidehMacros.Models;

namespace AidehMacros.Services
{
    public class KeyboardDeviceService
    {
        public List<KeyboardDevice> GetConnectedKeyboards()
        {
            var keyboards = new List<KeyboardDevice>();
            
            try
            {
                // Query Win32_Keyboard for basic keyboard info
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Keyboard");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    var keyboard = new KeyboardDevice
                    {
                        DeviceId = obj["DeviceID"]?.ToString() ?? string.Empty,
                        Name = obj["Name"]?.ToString() ?? "Unknown Keyboard",
                        Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown",
                        IsConnected = true
                    };
                    
                    keyboards.Add(keyboard);
                }
                
                // Also query Win32_PnPEntity for more detailed HID information
                using var hidSearcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE ClassGuid='{4d36e96b-e325-11ce-bfc1-08002be10318}' AND PNPClass='Keyboard'");
                using var hidCollection = hidSearcher.Get();
                
                foreach (ManagementObject obj in hidCollection)
                {
                    var deviceId = obj["DeviceID"]?.ToString() ?? string.Empty;
                    var name = obj["Name"]?.ToString() ?? "Unknown Keyboard";
                    var manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                    
                    // Extract VID and PID from DeviceID (format: USB\VID_046D&PID_C52B\...)
                    var (vendorId, productId) = ExtractVidPid(deviceId);
                    
                    // Check if we already have this keyboard from the previous query
                    var existing = keyboards.FirstOrDefault(k => k.DeviceId == deviceId);
                    if (existing != null)
                    {
                        existing.VendorId = vendorId;
                        existing.ProductId = productId;
                        if (existing.Name == "Unknown Keyboard")
                            existing.Name = name;
                        if (existing.Manufacturer == "Unknown")
                            existing.Manufacturer = manufacturer;
                    }
                    else
                    {
                        keyboards.Add(new KeyboardDevice
                        {
                            DeviceId = deviceId,
                            Name = name,
                            Manufacturer = manufacturer,
                            VendorId = vendorId,
                            ProductId = productId,
                            IsConnected = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error detecting keyboards: {ex.Message}");
            }
            
            return keyboards.Distinct().ToList();
        }
        
        private (string vendorId, string productId) ExtractVidPid(string deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId)) return (string.Empty, string.Empty);
                
                var parts = deviceId.Split('\\');
                if (parts.Length < 2) return (string.Empty, string.Empty);
                
                var vidPidPart = parts[1];
                var vidIndex = vidPidPart.IndexOf("VID_");
                var pidIndex = vidPidPart.IndexOf("PID_");
                
                string vendorId = string.Empty;
                string productId = string.Empty;
                
                if (vidIndex >= 0)
                {
                    vendorId = vidPidPart.Substring(vidIndex + 4, 4);
                }
                
                if (pidIndex >= 0)
                {
                    productId = vidPidPart.Substring(pidIndex + 4, 4);
                }
                
                return (vendorId, productId);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }
    }
} 