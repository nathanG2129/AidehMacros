# AidehMacros - Magic Remote Control Keyboard

Transform any spare keyboard into a professional streaming remote control! AidehMacros lets you use a second keyboard as a dedicated macro device while keeping your main keyboard completely normal.

## 🎯 What Does It Do?

AidehMacros solves the problem content creators face: **remembering complex keyboard shortcuts while trying to focus on creating content**. Instead of fumbling with "Ctrl+Shift+F3" combinations, you can simply press "F1" on your macro keyboard to switch scenes, "F2" to start recording, or any key you prefer.

### Real Example: Streaming a Cooking Show
- You're mixing ingredients and want the overhead camera view
- Instead of fumbling with complex shortcuts on your messy main keyboard
- You simply tap "3" on your clean remote keyboard  
- Instantly switches to overhead view!

## 🚀 Key Features

- ✅ **No memorizing complex shortcuts** - just simple button presses
- ✅ **Main keyboard unaffected** - works exactly like always
- ✅ **Use keyboards you already own** - no expensive equipment needed  
- ✅ **Settings save automatically** - always ready when you start streaming
- ✅ **Modern Windows 11-style interface** - beautiful and easy to use
- ✅ **Multiple action types** - key combinations, text input, or run commands

## 📋 System Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Two keyboards (your main keyboard + any spare keyboard)

## 🛠️ Installation

1. **Download the Release**
   - Download the latest release from the Releases section
   - Extract the ZIP file to a folder like `C:\AidehMacros`

2. **Or Build from Source**
   ```bash
   git clone https://github.com/yourusername/AidehMacros.git
   cd AidehMacros
   dotnet build
   dotnet run
   ```

## 📚 How to Use

### Step 1: Setup Your Macro Keyboard
1. Connect your spare keyboard (this will become your macro remote)
2. Launch AidehMacros
3. Go to the **🔧 Setup** tab
4. Click **🔄 Refresh** to scan for keyboards
5. Select your spare keyboard from the dropdown
6. Test by pressing any key on your macro keyboard

### Step 2: Create Actions
1. Go to the **⚡ Actions** tab
2. Click **+ New Action**
3. Choose from three action types:

#### Key Combination
- **Use for:** OBS scene switching, Discord mute, etc.
- **Example:** Create "Switch to Camera 1" with keys: Ctrl + Shift + F3

#### Send Text
- **Use for:** Chat messages, commands, signatures
- **Example:** Automatically type "Thanks for watching!"

#### Run Command
- **Use for:** Launch programs, open websites, system commands
- **Example:** `notepad.exe` or `start https://obs.studio`

### Step 3: Map Keys to Actions
1. Go to the **🎯 Mappings** tab
2. Click **+ New Mapping**
3. Select a trigger key (F1, F2, 1, 2, etc.)
4. Choose which action to execute
5. Save and test!

### Step 4: Start Using Your Magic Remote!
- Press **F1** on your macro keyboard → Execute your first action
- Press **F2** on your macro keyboard → Execute your second action
- Your main keyboard works exactly as before

## 🎮 Perfect For

- **Streamers**: Quick scene changes, audio controls, chat commands
- **Content Creators**: Easy editing shortcuts, screen capture controls
- **Gamers**: Discord mute, streaming controls, game-specific macros
- **Presenters**: Slide controls, microphone management
- **Developers**: Code snippets, build commands, terminal shortcuts

## 💡 Pro Tips

1. **Start Simple**: Begin with F1-F12 keys for easy access
2. **Label Your Keys**: Use sticky notes to remember what each key does
3. **Test Before Streaming**: Always test your mappings before going live
4. **Use Descriptive Names**: Name actions clearly like "Mute Microphone" not "Action 1"
5. **Backup Settings**: Your config is saved in `%AppData%\AidehMacros\config.json`

## 🛡️ Security & Permissions

AidehMacros uses low-level keyboard hooks to detect input from your macro keyboard. This requires:
- **Windows permission prompts** on first run (this is normal)
- **Administrator privileges** for some advanced shortcuts
- **Antivirus may flag it** initially (add to whitelist if needed)

## 🐛 Troubleshooting

### Keyboard Not Detected
- Try unplugging and reconnecting your macro keyboard
- Click the **🔄 Refresh** button
- Restart AidehMacros as Administrator

### Actions Not Working
- Check that the action is **Enabled** (checkmark)
- Verify the key mapping is correct
- Test the action manually first
- Try running AidehMacros as Administrator

### Key Presses Showing on Main Keyboard
- Make sure you selected the correct keyboard in Setup
- The key should only work on your designated macro keyboard

## 🏗️ Technical Details

### Built With
- **.NET 8.0** - Modern Windows desktop framework
- **WPF** - Rich user interface
- **Win32 API** - Low-level keyboard input detection
- **WMI** - Hardware device enumeration

### Architecture
- **Models**: Data structures for keyboards, actions, and mappings
- **Services**: Keyboard detection, input hooks, macro execution
- **Configuration**: JSON-based settings persistence
- **UI**: Modern tabbed interface with validation

## 📝 Configuration File

Settings are automatically saved to:
```
%AppData%\AidehMacros\config.json
```

You can manually edit this file if needed, but the UI is recommended.

## 🤝 Contributing

Want to help make AidehMacros even better?

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the need for better streaming workflow tools
- Built for the content creator community
- Special thanks to streamers who provided feedback during development

## 📞 Support

- **Issues**: Report bugs or request features on GitHub Issues
- **Discussions**: Join the community discussions for tips and tricks
- **Documentation**: Check the Wiki for detailed guides

---

**Transform your spare keyboard into professional streaming controls today!** 🎬✨ 