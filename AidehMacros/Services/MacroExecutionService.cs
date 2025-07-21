using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AidehMacros.Models;

namespace AidehMacros.Services
{
    public class MacroExecutionService
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
        
        private const uint KEYEVENTF_KEYUP = 0x0002;
        
        public async Task ExecuteActionAsync(MacroAction action)
        {
            if (!action.IsEnabled) return;
            
            try
            {
                switch (action.Type)
                {
                    case ActionType.KeyCombination:
                        await ExecuteKeyCombinationAsync(action.KeyCombination);
                        break;
                        
                    case ActionType.SendText:
                        await SendTextAsync(action.Data);
                        break;
                        
                    case ActionType.RunCommand:
                        await RunCommandAsync(action.Data);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing macro action: {ex.Message}");
            }
        }
        
        private async Task ExecuteKeyCombinationAsync(List<string> keys)
        {
            if (keys.Count == 0) return;
            
            var virtualKeys = new List<byte>();
            
            // Convert key names to virtual key codes
            foreach (var keyName in keys)
            {
                var vk = GetVirtualKeyCode(keyName);
                if (vk != 0)
                    virtualKeys.Add(vk);
            }
            
            if (virtualKeys.Count == 0) return;
            
            // Press all keys down
            foreach (var vk in virtualKeys)
            {
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                await Task.Delay(10); // Small delay between key presses
            }
            
            // Release all keys (in reverse order)
            for (int i = virtualKeys.Count - 1; i >= 0; i--)
            {
                keybd_event(virtualKeys[i], 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                await Task.Delay(10);
            }
        }
        
        private async Task SendTextAsync(string text)
        {
            foreach (char c in text)
            {
                var vk = VkKeyScan(c);
                var keyCode = (byte)(vk & 0xFF);
                var shiftState = (vk >> 8) & 0xFF;
                
                // Check if shift is needed
                if ((shiftState & 1) != 0)
                {
                    keybd_event(0x10, 0, 0, UIntPtr.Zero); // Shift down
                }
                
                keybd_event(keyCode, 0, 0, UIntPtr.Zero);
                keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                if ((shiftState & 1) != 0)
                {
                    keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
                }
                
                await Task.Delay(10);
            }
        }
        
        private async Task RunCommandAsync(string command)
        {
            await Task.Run(() =>
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = Process.Start(startInfo);
                    process?.WaitForExit(5000); // 5 second timeout
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error running command: {ex.Message}");
                }
            });
        }
        
        private byte GetVirtualKeyCode(string keyName)
        {
            return keyName.ToUpper() switch
            {
                "CTRL" or "CONTROL" => 0x11,
                "ALT" => 0x12,
                "SHIFT" => 0x10,
                "WIN" or "WINDOWS" => 0x5B,
                "TAB" => 0x09,
                "ENTER" or "RETURN" => 0x0D,
                "ESC" or "ESCAPE" => 0x1B,
                "SPACE" => 0x20,
                "BACKSPACE" => 0x08,
                "DELETE" => 0x2E,
                "HOME" => 0x24,
                "END" => 0x23,
                "PAGEUP" => 0x21,
                "PAGEDOWN" => 0x22,
                "UP" => 0x26,
                "DOWN" => 0x28,
                "LEFT" => 0x25,
                "RIGHT" => 0x27,
                "F1" => 0x70,
                "F2" => 0x71,
                "F3" => 0x72,
                "F4" => 0x73,
                "F5" => 0x74,
                "F6" => 0x75,
                "F7" => 0x76,
                "F8" => 0x77,
                "F9" => 0x78,
                "F10" => 0x79,
                "F11" => 0x7A,
                "F12" => 0x7B,
                "0" => 0x30,
                "1" => 0x31,
                "2" => 0x32,
                "3" => 0x33,
                "4" => 0x34,
                "5" => 0x35,
                "6" => 0x36,
                "7" => 0x37,
                "8" => 0x38,
                "9" => 0x39,
                "A" => 0x41,
                "B" => 0x42,
                "C" => 0x43,
                "D" => 0x44,
                "E" => 0x45,
                "F" => 0x46,
                "G" => 0x47,
                "H" => 0x48,
                "I" => 0x49,
                "J" => 0x4A,
                "K" => 0x4B,
                "L" => 0x4C,
                "M" => 0x4D,
                "N" => 0x4E,
                "O" => 0x4F,
                "P" => 0x50,
                "Q" => 0x51,
                "R" => 0x52,
                "S" => 0x53,
                "T" => 0x54,
                "U" => 0x55,
                "V" => 0x56,
                "W" => 0x57,
                "X" => 0x58,
                "Y" => 0x59,
                "Z" => 0x5A,
                _ => 0
            };
        }
    }
} 