using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace AidehMacros.Services
{
    public class LowLevelKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        public event EventHandler<KeyboardHookEventArgs>? KeyDown;
        public event EventHandler<KeyboardHookEventArgs>? KeyUp;
        
        private LowLevelKeyboardProc _proc = HookCallback;
        private IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardHook? _instance;
        
        // Duplicate message detection
        private static readonly Dictionary<string, DateTime> _lastHookMessage = new();
        private static readonly TimeSpan _duplicateWindow = TimeSpan.FromMilliseconds(20);
        
        public LowLevelKeyboardHook()
        {
            _instance = this;
        }
        
        public IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName == null) return IntPtr.Zero;
                
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        public void StartHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("LowLevelKeyboardHook: Hook already started, stopping first");
                StopHook();
            }
            
            _hookID = SetHook(_proc);
            if (_hookID != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Hook started successfully with ID {_hookID:X8}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Failed to start hook, Win32 error: {error}");
            }
        }
        
        public void StopHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
        
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook.HookCallback: nCode={nCode}, wParam={wParam:X8}, lParam={lParam:X8}");
            
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);
                    var keyName = key.ToString();
                    var now = DateTime.Now;
                    
                    // Check for duplicate messages (research showed this happens during menu mode, fast typing, etc.)
                    var messageKey = $"{keyName}_DOWN";
                    if (_lastHookMessage.ContainsKey(messageKey) && 
                        (now - _lastHookMessage[messageKey]) < _duplicateWindow)
                    {
                        System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Duplicate DOWN message for {keyName}, ignoring");
                        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                    }
                    
                    _lastHookMessage[messageKey] = now;
                    
                    System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Key DOWN detected: {key} (VK: {hookStruct.vkCode})");
                    
                    var args = new KeyboardHookEventArgs
                    {
                        Key = key,
                        VirtualKeyCode = hookStruct.vkCode,
                        ScanCode = hookStruct.scanCode,
                        Flags = hookStruct.flags,
                        Time = hookStruct.time,
                        ExtraInfo = hookStruct.dwExtraInfo
                    };
                    
                    _instance?.KeyDown?.Invoke(_instance, args);
                    
                    if (args.Handled)
                    {
                        System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Key {key} was HANDLED (blocked)");
                        return (IntPtr)1; // Suppress the key
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Key {key} was NOT handled (allowed through)");
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);
                    var keyName = key.ToString();
                    var now = DateTime.Now;
                    
                    // Check for duplicate UP messages
                    var messageKey = $"{keyName}_UP";
                    if (_lastHookMessage.ContainsKey(messageKey) && 
                        (now - _lastHookMessage[messageKey]) < _duplicateWindow)
                    {
                        System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Duplicate UP message for {keyName}, ignoring");
                        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                    }
                    
                    _lastHookMessage[messageKey] = now;
                    
                    System.Diagnostics.Debug.WriteLine($"LowLevelKeyboardHook: Key UP detected: {key} (VK: {hookStruct.vkCode})");
                    
                    var args = new KeyboardHookEventArgs
                    {
                        Key = key,
                        VirtualKeyCode = hookStruct.vkCode,
                        ScanCode = hookStruct.scanCode,
                        Flags = hookStruct.flags,
                        Time = hookStruct.time,
                        ExtraInfo = hookStruct.dwExtraInfo
                    };
                    
                    _instance?.KeyUp?.Invoke(_instance, args);
                }
                
                // Clean up old message tracking entries periodically
                if (_lastHookMessage.Count > 20)
                {
                    var cutoff = DateTime.Now - _duplicateWindow;
                    var keysToRemove = _lastHookMessage.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _lastHookMessage.Remove(key);
                    }
                }
            }
            
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
    
    public class KeyboardHookEventArgs : EventArgs
    {
        public Key Key { get; set; }
        public uint VirtualKeyCode { get; set; }
        public uint ScanCode { get; set; }
        public uint Flags { get; set; }
        public uint Time { get; set; }
        public IntPtr ExtraInfo { get; set; }
        public bool Handled { get; set; }
    }
} 