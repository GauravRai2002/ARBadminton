# Build & Deployment Guide

## iOS Build Process

### Prerequisites
- ✅ macOS computer
- ✅ Xcode 14+ installed
- ✅ iOS device (iPhone 6S or newer)
- ✅ Apple Developer Account
- ✅ USB cable

### Step 1: Configure Unity Project for iOS

1. **Open Build Settings**
   ```
   File → Build Settings
   ```

2. **Select iOS Platform**
   - Click on "iOS" in platform list
   - Click "Switch Platform" (wait for completion)

3. **Configure Player Settings**
   ```
   Click "Player Settings" button
   ```
   
   **Resolution and Presentation:**
   - Default Orientation: Auto Rotation
   - Allowed Orientations: Portrait, Landscape
   
   **Other Settings:**
   - Bundle Identifier: `com.yourcompany.arbadmintonnet`
     (Must be unique, use reverse domain notation)
   - Signing Team ID: (Your Apple Developer Team ID)
   - Camera Usage Description: "Camera access required for AR and shuttle detection"
   - Target minimum iOS Version: 13.0
   - Architecture: ARM64
   - Target SDK: Device SDK
   
   **XR Settings:**
   - Scripting Backend: IL2CPP
   - API Compatibility Level: .NET Standard 2.1

### Step 2: Build Xcode Project

1. **Start Build**
   ```
   File → Build Settings → iOS → Build
   ```

2. **Choose Output Location**
   - Create folder: `Builds/iOS`
   - Click "Select Folder"

3. **Wait for Build**
   - Build process takes 5-15 minutes
   - Unity will generate Xcode project

### Step 3: Configure Xcode

1. **Open Xcode Project**
   - Navigate to `Builds/iOS/`
   - Double-click `Unity-iPhone.xcodeproj`

2. **Select Target Device**
   - Connect iPhone via USB
   - Trust computer on iPhone if prompted
   - Select your iPhone in device dropdown (top of Xcode)

3. **Configure Signing**
   - Select "Unity-iPhone" target in left panel
   - Go to "Signing & Capabilities" tab
   - Check "Automatically manage signing"
   - Select your Team (Apple Developer Account)
   
   If you see signing errors:
   - Change Bundle Identifier to something unique
   - Ensure your Apple ID is added in Xcode → Preferences → Accounts

4. **Verify Capabilities**
   Ensure these are enabled:
   - ✅ Camera (should be automatic)
   
   If needed, click "+ Capability" to add more

### Step 4: Deploy to Device

1. **Build and Run**
   - Click the Play button (▶️) in Xcode toolbar
   - OR: Product → Run (⌘R)

2. **First-Time Trust**
   On your iPhone:
   - Go to Settings → General → Device Management
   - Tap on your developer certificate
   - Tap "Trust [Your Name]"

3. **Launch App**
   - App should launch automatically
   - OR: Tap app icon on home screen

### Step 5: Grant Permissions

When app launches:
1. Allow camera access when prompted
2. Point camera at floor/ground
3. Move device slowly to detect planes

## Android Build Process

### Prerequisites
- ✅ Android device with ARCore support
- ✅ USB cable
- ✅ USB debugging enabled on device
- ✅ Android SDK installed

### Step 1: Enable Developer Options on Android

1. **Open Settings** on Android device
2. **Navigate to About Phone**
3. **Tap "Build Number" 7 times**
   - You'll see "You are now a developer!"
4. **Go back to Settings**
5. **Find "Developer Options"**
6. **Enable "USB Debugging"**

### Step 2: Configure Unity Project for Android

1. **Open Build Settings**
   ```
   File → Build Settings
   ```

2. **Select Android Platform**
   - Click on "Android" in platform list
   - Click "Switch Platform" (wait for completion)

3. **Configure Player Settings**
   ```
   Click "Player Settings" button
   ```
   
   **Other Settings:**
   - Package Name: `com.yourcompany.arbadmintonnet`
     (Must be unique, use reverse domain notation)
   - Version: 1.0
   - Bundle Version Code: 1
   - Minimum API Level: Android 7.0 'Nougat' (API level 24)
   - Target API Level: Automatic (highest installed)
   - Scripting Backend: IL2CPP
   - API Compatibility Level: .NET Standard 2.1
   - Target Architectures: ARM64 ✅

4. **Configure XR Settings**
   ```
   Edit → Project Settings → XR Plug-in Management
   ```
   - Click Android tab (robot icon)
   - Enable: ✅ ARCore

### Step 3: Connect Android Device

1. **Connect via USB**
2. **Allow USB Debugging**
   - Popup appears on Android: "Allow USB debugging?"
   - Check "Always allow from this computer"
   - Tap "OK"

3. **Verify Connection in Unity**
   - File → Build Settings
   - "Run Device" dropdown should show your device
   - If not showing:
     - Check USB cable
     - Disable/re-enable USB debugging
     - Try different USB port

### Step 4: Build and Deploy

1. **Build and Run**
   ```
   File → Build Settings → Android
   Ensure your device is selected in "Run Device"
   Click "Build And Run"
   ```

2. **Choose Output Location**
   - Create folder: `Builds/Android`
   - Name file: `ARBadmintonNet.apk`
   - Click "Save"

3. **Wait for Build**
   - Build process takes 5-20 minutes
   - Unity will:
     - Build APK
     - Install to device
     - Launch app automatically

### Step 5: Grant Permissions

When app launches:
1. Allow camera access when prompted
2. Point camera at floor/ground
3. Move device slowly to detect planes

## Alternative: Manual APK Installation

If "Build and Run" doesn't work:

### Create APK Only

1. **Build APK**
   ```
   File → Build Settings → Android → Build
   ```
   - Save as `ARBadmintonNet.apk`

2. **Install via ADB**
   ```bash
   cd /path/to/android-sdk/platform-tools
   ./adb install /path/to/ARBadmintonNet.apk
   ```

3. **Or Transfer APK to Device**
   - Email APK to yourself
   - Download on Android device
   - Tap to install
   - Enable "Install from unknown sources" if needed

## Troubleshooting

### iOS Issues

**"No code signing identities found"**
- Solution: Add Apple ID in Xcode → Preferences → Accounts
- Or: Join Apple Developer Program

**"Failed to verify code signature"**
- Solution: In Xcode, change Bundle Identifier to unique value
- Clean build: Product → Clean Build Folder
- Rebuild

**"App crashes on launch"**
- Check Unity console for errors before building
- Verify ARKit is enabled in XR Plug-in Management
- Check camera usage description is set

**"AR session doesn't start"**
- Ensure device supports ARKit (iPhone 6S+)
- Check iOS version (13.0+)
- Restart device

### Android Issues

**"Unable to install APK"**
- Enable "Install from unknown sources"
- Check storage space on device
- Uninstall previous version

**"Device not detected"**
- Install/update Android USB drivers
- Try different USB cable
- Restart ADB:
  ```bash
  adb kill-server
  adb start-server
  ```

**"ARCore not supported"**
- Verify device in [ARCore supported devices](https://developers.google.com/ar/devices)
- Update Google Play Services
- Update ARCore from Play Store

**"Build failed: Unable to merge android manifests"**
- Delete `Assets/Plugins/Android` folder
- Let Unity regenerate manifest
- Rebuild

### Performance Issues

**Low FPS on device**
- Reduce detection frame skip (check every 2-3 frames instead of every frame)
- Lower camera resolution
- Disable debug visualization
- Use simpler 3D models

**High battery drain**
- Normal for AR apps
- Reduce screen brightness
- Close other apps
- Consider adding power-saving mode

## Testing Checklist

### Before Release

- [ ] Test on minimum supported iOS version (13.0)
- [ ] Test on minimum supported Android version (7.0)
- [ ] Test in various lighting conditions
- [ ] Test on different surface types (ground, table, floor)
- [ ] Test shuttle detection with different colors
- [ ] Test collision accuracy from multiple angles
- [ ] Verify audio feedback works
- [ ] Verify UI displays correctly on different screen sizes
- [ ] Test battery consumption over 15-minute session
- [ ] Check app permissions are minimal
- [ ] Verify app size is reasonable

### Performance Targets

- FPS: Should maintain 60 FPS in AR view
- Build Size: < 150 MB
- Memory: < 200 MB RAM usage
- Detection Latency: < 100ms
- Battery: < 20% drain per 15 minutes

## Distribution

### iOS Distribution

**TestFlight (Recommended for testing):**
1. Archive app in Xcode: Product → Archive
2. Upload to App Store Connect
3. Configure TestFlight
4. Invite testers via email

**App Store:**
1. Archive and upload (same as TestFlight)
2. Fill out App Store listing
3. Submit for review
4. Wait for approval (1-3 days typically)

### Android Distribution

**Google Play Console (Internal Testing):**
1. Create app in Google Play Console
2. Upload APK
3. Create internal test track
4. Invite testers

**Direct Distribution:**
1. Build signed APK (with release keystore)
2. Share APK file directly
3. Users install via "unknown sources"

## Optimization Tips

### Reduce Build Size

- Remove unused assets
- Use texture compression
- Use asset bundles for large resources
- Strip engine code in Player Settings

### Improve Performance

- Lower physics update rate
- Use object pooling for visual indicators
- Optimize shader complexity
- Reduce draw calls

### Battery Optimization

- Lower camera frame rate if possible
- Reduce detection frequency
- Implement power-saving mode
- Stop tracking when app backgrounded

## Resources

- [Unity AR Foundation Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@latest)
- [ARKit Requirements](https://developer.apple.com/documentation/arkit)
- [ARCore Supported Devices](https://developers.google.com/ar/devices)
- [Unity iOS Build Guide](https://docs.unity3d.com/Manual/iphone-GettingStarted.html)
- [Unity Android Build Guide](https://docs.unity3d.com/Manual/android-GettingStarted.html)

---

Good luck with your AR Badminton Net Detection app! 🏸
