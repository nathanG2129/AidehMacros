using System;
using System.Diagnostics;
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
            _hookID = SetHook(_proc);
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
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);
                    
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
                        return (IntPtr)1; // Suppress the key
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);
                    
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