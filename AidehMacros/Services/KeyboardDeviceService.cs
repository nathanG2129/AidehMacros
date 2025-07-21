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
                System.Diagnostics.Debug.WriteLine("Starting keyboard detection...");
                
                // Query Win32_Keyboard for basic keyboard info
                System.Diagnostics.Debug.WriteLine("Querying Win32_Keyboard...");
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Keyboard");
                using var collection = searcher.Get();
                
                System.Diagnostics.Debug.WriteLine($"Found {collection.Count} Win32_Keyboard entries");
                
                foreach (ManagementObject obj in collection)
                {
                    try
                    {
                        var deviceId = SafeGetProperty(obj, "DeviceID") ?? $"KEYBOARD_{keyboards.Count + 1}";
                        var name = SafeGetProperty(obj, "Name") ?? "HID Keyboard Device";
                        var manufacturer = SafeGetProperty(obj, "Manufacturer") ?? "Unknown";
                        
                        System.Diagnostics.Debug.WriteLine($"Found keyboard: {name} (ID: {deviceId})");
                        
                        var keyboard = new KeyboardDevice
                        {
                            DeviceId = deviceId,
                            Name = name,
                            Manufacturer = manufacturer,
                            IsConnected = true
                        };
                        
                        keyboards.Add(keyboard);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading keyboard properties: {ex.Message}");
                        // Add a generic keyboard entry
                        keyboards.Add(new KeyboardDevice
                        {
                            DeviceId = $"KEYBOARD_{keyboards.Count + 1}",
                            Name = $"Keyboard Device {keyboards.Count + 1}",
                            Manufacturer = "Unknown",
                            IsConnected = true
                        });
                    }
                }
                
                // Also query Win32_PnPEntity for more detailed HID information
                System.Diagnostics.Debug.WriteLine("Querying Win32_PnPEntity for keyboard devices...");
                using var hidSearcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE ClassGuid='{4d36e96b-e325-11ce-bfc1-08002be10318}' AND PNPClass='Keyboard'");
                using var hidCollection = hidSearcher.Get();
                
                System.Diagnostics.Debug.WriteLine($"Found {hidCollection.Count} PnPEntity keyboard entries");
                
                foreach (ManagementObject obj in hidCollection)
                {
                    try
                    {
                        var deviceId = SafeGetProperty(obj, "DeviceID") ?? string.Empty;
                        var name = SafeGetProperty(obj, "Name") ?? "Unknown Keyboard";
                        var manufacturer = SafeGetProperty(obj, "Manufacturer") ?? "Unknown";
                        
                        // Extract VID and PID from DeviceID (format: USB\VID_046D&PID_C52B\...)
                        var (vendorId, productId) = ExtractVidPid(deviceId);
                        
                        // Check if we already have this keyboard from the previous query
                        var existing = keyboards.FirstOrDefault(k => k.DeviceId == deviceId);
                        if (existing != null)
                        {
                            existing.VendorId = vendorId;
                            existing.ProductId = productId;
                            if (existing.Name == "HID Keyboard Device")
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading PnP keyboard properties: {ex.Message}");
                    }
                }
                
                // Add a fallback "Virtual Keyboard" if no real keyboards were detected
                if (keyboards.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No keyboards detected via WMI, adding fallback keyboard");
                    keyboards.Add(new KeyboardDevice
                    {
                        DeviceId = "FALLBACK_KEYBOARD",
                        Name = "All Keyboards (Fallback)",
                        Manufacturer = "System",
                        IsConnected = true
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error detecting keyboards: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Always provide at least a fallback keyboard
                keyboards.Add(new KeyboardDevice
                {
                    DeviceId = "FALLBACK_KEYBOARD",
                    Name = "All Keyboards (Fallback)",
                    Manufacturer = "System",
                    IsConnected = true
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"Total keyboards found: {keyboards.Count}");
            return keyboards.Distinct().ToList();
        }
        
        private string? SafeGetProperty(ManagementObject obj, string propertyName)
        {
            try
            {
                return obj[propertyName]?.ToString();
            }
            catch (ManagementException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not read property {propertyName}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error reading property {propertyName}: {ex.Message}");
                return null;
            }
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