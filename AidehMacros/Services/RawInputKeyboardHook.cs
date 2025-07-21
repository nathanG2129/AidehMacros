using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Diagnostics;

namespace AidehMacros.Services
{
    public class RawInputKeyboardHook
    {
        #region Constants and Structures

        private const uint RIM_TYPEKEYBOARD = 1;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWKEYBOARD keyboard;
        }

        #endregion

        #region DLL Imports

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

        #endregion

        #region Events

        public event EventHandler<RawKeyboardEventArgs>? KeyDown;
        public event EventHandler<RawKeyboardEventArgs>? KeyUp;

        #endregion

        #region Fields

        private IntPtr _windowHandle;
        private HwndSource? _hwndSource;
        private readonly Dictionary<IntPtr, string> _deviceNames = new();
        private string? _targetDeviceId;
        private bool _isDetectionMode = false;

        #endregion

        #region Public Methods

        public bool Initialize(IntPtr windowHandle)
        {
            try
            {
                _windowHandle = windowHandle;
                
                // Register for raw input from all keyboards
                var device = new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01, // Generic Desktop Controls
                    usUsage = 0x06,     // Keyboard
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = windowHandle
                };

                var devices = new[] { device };
                bool success = RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
                
                if (success)
                {
                    // Hook into the window's message processing
                    _hwndSource = HwndSource.FromHwnd(windowHandle);
                    if (_hwndSource != null)
                    {
                        _hwndSource.AddHook(WndProc);
                        Debug.WriteLine("Raw Input keyboard hook initialized successfully");
                        return true;
                    }
                }
                
                Debug.WriteLine($"Failed to initialize Raw Input keyboard hook. Success: {success}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Raw Input: {ex.Message}");
                return false;
            }
        }

        public void StartDetectionMode()
        {
            _isDetectionMode = true;
            _targetDeviceId = null;
            Debug.WriteLine("Started keyboard detection mode - press any key on your macro keyboard");
        }

        public void StopDetectionMode()
        {
            _isDetectionMode = false;
            Debug.WriteLine("Stopped keyboard detection mode");
        }

        public void SetTargetDevice(string deviceId)
        {
            _targetDeviceId = deviceId;
            Debug.WriteLine($"Target device set to: {deviceId}");
        }

        public string? GetTargetDevice()
        {
            return _targetDeviceId;
        }

        public void Dispose()
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
        }

        #endregion

        #region Private Methods

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;

            if (msg == WM_INPUT)
            {
                ProcessRawInput(lParam);
            }

            return IntPtr.Zero;
        }

        private void ProcessRawInput(IntPtr lParam)
        {
            try
            {
                uint dwSize = 0;
                GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

                if (dwSize == 0) return;

                IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                try
                {
                    uint bytesRead = (uint)GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>());
                    if (bytesRead != dwSize) return;

                    var rawInput = Marshal.PtrToStructure<RAWINPUT>(buffer);
                    
                    if (rawInput.header.dwType == RIM_TYPEKEYBOARD)
                    {
                        ProcessKeyboardInput(rawInput);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing raw input: {ex.Message}");
            }
        }

        private void ProcessKeyboardInput(RAWINPUT rawInput)
        {
            try
            {
                var deviceHandle = rawInput.header.hDevice;
                var keyboard = rawInput.keyboard;
                var deviceId = GetDeviceId(deviceHandle);
                
                // Only process key down events
                if ((keyboard.Flags & 0x01) == 0) // RI_KEY_MAKE (key down)
                {
                    var key = KeyInterop.KeyFromVirtualKey(keyboard.VKey);
                    
                    bool isFromTargetDevice = false;
                    
                    if (_isDetectionMode)
                    {
                        // In detection mode, any keypress identifies the target device
                        _targetDeviceId = deviceId;
                        _isDetectionMode = false;
                        isFromTargetDevice = true;
                        Debug.WriteLine($"Keyboard detected! Device ID: {deviceId}");
                    }
                    else if (!string.IsNullOrEmpty(_targetDeviceId))
                    {
                        // Normal mode - check if this is from our target device
                        isFromTargetDevice = deviceId == _targetDeviceId;
                    }
                    
                    var args = new RawKeyboardEventArgs
                    {
                        Key = key,
                        VirtualKeyCode = keyboard.VKey,
                        DeviceHandle = deviceHandle,
                        DeviceId = deviceId,
                        IsFromTargetDevice = isFromTargetDevice
                    };

                    KeyDown?.Invoke(this, args);
                    
                    Debug.WriteLine($"Key: {key} (VK: {keyboard.VKey}) from device: {deviceHandle:X8} ({deviceId}), IsTarget: {isFromTargetDevice}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing keyboard input: {ex.Message}");
            }
        }

        private string GetDeviceId(IntPtr deviceHandle)
        {
            if (_deviceNames.ContainsKey(deviceHandle))
            {
                return _deviceNames[deviceHandle];
            }

            // For simplicity, we'll just use the device handle as a unique identifier
            // This is much more reliable than trying to decode device names
            string deviceId = $"DEVICE_{deviceHandle:X8}";
            _deviceNames[deviceHandle] = deviceId;
            return deviceId;
        }

        #endregion
    }

    public class RawKeyboardEventArgs : EventArgs
    {
        public Key Key { get; set; }
        public uint VirtualKeyCode { get; set; }
        public IntPtr DeviceHandle { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public bool IsFromTargetDevice { get; set; }
    }
} 