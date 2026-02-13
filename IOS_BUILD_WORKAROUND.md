# iOS Build Workaround - Building Without Unity Hub Module

Since the iOS Build Support module installation is failing in Unity Hub, you can build your app directly from Unity using Xcode.

## Prerequisites
✅ Xcode is installed (you have this)
✅ Unity project is complete (you have this)
✅ iPhone 16 Pro available for testing

## Steps to Build:

### 1. Configure Player Settings in Unity

1. Open your Unity project
2. Go to **Edit → Project Settings**
3. Click **Player** in the left sidebar
4. Click the **iOS tab** (phone icon) at the top

#### Required Settings:
- **Company Name**: Your name or company
- **Product Name**: ARBadmintonNet
- **Bundle Identifier**: `com.yourname.arbadmintonnet` (must be unique, all lowercase)
  - Change "yourname" to your actual name, e.g., `com.gaurav.arbadmintonnet`
- **Version**: 1.0
- **Build Number**: 1

#### Other Settings Section:
- **Target minimum iOS Version**: 13.0 or higher
- **Architecture**: ARM64
- **Camera Usage Description**: "Required for AR tracking and shuttle detection"

### 2. Try Building from Unity

1. Go to **File → Build Settings** (or Window → Build Profiles)
2. Select **iOS** platform
3. Click **Switch Platform** (wait for it to finish)
4. Click **Build**
5. Choose a folder to save the Xcode project (e.g., create a folder called "iOSBuild" on Desktop)
6. Click **Save**

Unity will attempt to build. If it asks about iOS module, try one of these:

**Option A**: It may build anyway if Xcode is detected
**Option B**: Close the error and try the manual package installation below

### 3. Manual iOS Module Installation (If Build Fails)

If Unity refuses to build without the module:

1. **Close Unity and Unity Hub**
2. Download iOS Support manually:
   - Go to: https://unity.com/releases/editor/archive
   - Find Unity 6000.3.7f1
   - Download **iOS Build Support** component separately
   - Install it manually

3. **Or try this**: Unity might let you install packages via Package Manager
   - Window → Package Manager
   - Look for iOS-related packages
   - Install them directly

### 4. After Build Succeeds:

1. Navigate to your build folder (e.g., Desktop/iOSBuild)
2. Open the **Unity-iPhone.xcodeproj** file (opens in Xcode)
3. In Xcode:
   - Select your **iPhone 16 Pro** from the device dropdown
   - Go to **Signing & Capabilities** tab
   - Check **Automatically manage signing**
   - Select your **Apple ID team**
4. Click the **Play/Run button** ▶️ in Xcode
5. App will build and deploy to your iPhone!

## Troubleshooting

**"Missing iOS module" error in Unity:**
- Try restarting Unity after Xcode installation
- Check if Unity detects Xcode: `Unity → Preferences → External Tools`

**Xcode signing errors:**
- Make sure you're logged into Xcode with your Apple ID
- Use a free Apple Developer account (no paid membership needed for testing)

**Build errors in Xcode:**
- Clean build: Product → Clean Build Folder
- Rebuild from Unity if needed

## Quick Test Without Device (Unity Editor)

While figuring out the build:
1. Press **Play ▶️** in Unity Editor
2. Won't have full AR, but can test logic
3. Check Console for any script errors

---

Let me know if the direct build works or if you need help with any specific error!
