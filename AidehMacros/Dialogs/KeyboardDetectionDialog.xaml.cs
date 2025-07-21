using System;
using System.Windows;
using System.Windows.Interop;
using AidehMacros.Services;

namespace AidehMacros
{
    public partial class KeyboardDetectionDialog : Window
    {
        public string? DetectedKeyboardId { get; private set; }
        public bool WasDetected { get; private set; } = false;

        private RawInputKeyboardHook _keyboardHook;
        private bool _isDetecting = false;

        public KeyboardDetectionDialog()
        {
            InitializeComponent();
            _keyboardHook = new RawInputKeyboardHook();
            _keyboardHook.KeyDown += OnKeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the keyboard hook when the window is loaded
            var windowHandle = new WindowInteropHelper(this).Handle;
            if (!_keyboardHook.Initialize(windowHandle))
            {
                System.Windows.MessageBox.Show("Failed to initialize keyboard detection. Please try again.", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void DetectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDetecting)
            {
                StartDetection();
            }
            else
            {
                StopDetection();
            }
        }

        private void StartDetection()
        {
            _isDetecting = true;
            _keyboardHook.StartDetectionMode();
            
            DetectButton.Content = "Cancel Detection";
            StatusText.Text = "🎯 Press ANY key on your macro keyboard now...";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            DetectedKeyboardText.Visibility = Visibility.Collapsed;
            OkButton.IsEnabled = false;
        }

        private void StopDetection()
        {
            _isDetecting = false;
            _keyboardHook.StopDetectionMode();
            
            DetectButton.Content = "Start Detection";
            StatusText.Text = "Detection cancelled. Ready to try again.";
            StatusText.Foreground = System.Windows.Media.Brushes.Blue;
        }

        private void OnKeyDown(object? sender, RawKeyboardEventArgs e)
        {
            if (_isDetecting && e.IsFromTargetDevice)
            {
                // Key was detected from the target device!
                Dispatcher.Invoke(() =>
                {
                    DetectedKeyboardId = e.DeviceId;
                    WasDetected = true;
                    _isDetecting = false;
                    
                    StatusText.Text = "✅ Keyboard detected successfully!";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    
                    DetectedKeyboardText.Text = $"Detected Device: {e.DeviceId}";
                    DetectedKeyboardText.Visibility = Visibility.Visible;
                    
                    DetectButton.Content = "Detect Different Keyboard";
                    OkButton.IsEnabled = true;
                });
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (WasDetected && !string.IsNullOrEmpty(DetectedKeyboardId))
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _keyboardHook?.Dispose();
            base.OnClosed(e);
        }
    }
} 