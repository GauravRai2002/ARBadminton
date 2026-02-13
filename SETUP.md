# Setup Instructions - AR Badminton Net Detection

## Prerequisites Installation

### 1. Install Unity Hub
- Download from [unity.com/download](https://unity.com/download)
- Install Unity Hub application

### 2. Install Unity Editor
1. Open Unity Hub
2. Click **Installs** tab
3. Click **Install Editor**
4. Select **Unity 2022.3 LTS** (Long Term Support)
5. In module selection, check:
   - ✅ iOS Build Support
   - ✅ Android Build Support
   - ✅ Documentation
6. Click **Install**

### 3. For iOS Development (macOS only)
1. Install Xcode from Mac App Store (Xcode 14+)
2. Open Xcode and accept license agreement
3. Install Command Line Tools:
   ```bash
   xcode-select --install
   ```
4. Sign up for Apple Developer Program (required for device deployment)

### 4. For Android Development
1. Install Android Studio from [developer.android.com](https://developer.android.com/studio)
2. Run Android Studio, go to SDK Manager
3. Install:
   - Android SDK Platform 24 or higher
   - Android SDK Build-Tools
   - Android SDK Platform-Tools
4. Set ANDROID_SDK_ROOT environment variable

## Project Setup

### 1. Add Project to Unity Hub
1. Open Unity Hub
2. Click **Projects** tab
3. Click **Add** → **Add project from disk**
4. Navigate to `/Users/gauravrai/projects/ARBadmintonNet`
5. Click **Open**

### 2. Open Project
1. Click on the project in Unity Hub
2. Wait for Unity to load (first time may take several minutes)
3. Packages will auto-install based on manifest.json

### 3. Verify Package Installation
1. In Unity, go to **Window → Package Manager**
2. Verify these packages are installed:
   - AR Foundation (5.1.0)
   - ARKit XR Plugin (5.1.0)
   - ARCore XR Plugin (5.1.0)
   - Barracuda (3.0.0)
   - TextMeshPro
   - Input System

If any are missing:
1. Click **+** in Package Manager
2. Select **Add package by name**
3. Enter package name (e.g., `com.unity.xr.arfoundation`)

## Scene Setup

### Create Main Scene

1. **Create New Scene**
   - File → New Scene
   - Select "Basic (Built-in)" or "Basic (URP)"
   - Save as `Assets/Scenes/MainScene.unity`

2. **Add XR Origin**
   - Right-click in Hierarchy
   - GameObject → XR → XR Origin (Mobile AR)
   - This creates:
     - XR Origin (parent)
       - Camera Offset
         - Main Camera (AR Camera)
   - Add components to XR Origin:
     - AR Plane Manager
     - AR Raycast Manager
     - AR Anchor Manager

3. **Add AR Session**
   - GameObject → XR → AR Session
   - This handles AR lifecycle

4. **Create Game Manager**
   - GameObject → Create Empty
   - Rename to "GameManager"
   - Add all manager scripts:
     - `GameManager.cs`
     - `ARSessionManager.cs`
     - `ARPlaneDetectionManager.cs`
     - `NetPlacementController.cs`
     - `ColorBasedTracker.cs`
     - `NetCollisionDetector.cs`
     - `FeedbackManager.cs`
     - `AudioManager.cs`
     - `UIFeedbackController.cs`
   - Add AudioSource component (for AudioManager)

5. **Wire Up References**
   In GameManager Inspector:
   - Drag XR Origin's AR Session component to ARSessionManager
   - Drag AR Plane Manager to ARPlaneDetectionManager
   - Drag Main Camera (AR Camera) to relevant fields
   - Assign all scripts to their respective fields

### Create Net Prefab

1. **Create Net GameObject**
   ```
   GameObject → 3D Object → Cube
   Name: "BadmintonNet"
   Transform:
     - Scale: X=5.18, Y=1.55, Z=0.1
     - Position: (0, 0, 0)
   ```

2. **Create Net Material**
   - Right-click in Assets/Materials
   - Create → Material
   - Name: "NetMaterial"
   - Set Rendering Mode: Transparent
   - Adjust:
     - Albedo color: White with Alpha ~150
     - Metallic: 0
     - Smoothness: 0.3

3. **Apply Material**
   - Drag NetMaterial to BadmintonNet mesh

4. **Add Collider**
   - Select BadmintonNet
   - Add Component → Mesh Collider
   - Uncheck "Convex"

5. **Create Prefab**
   - Drag BadmintonNet from Hierarchy to Assets/Prefabs/
   - Delete from Hierarchy

6. **Assign to Controller**
   - Select GameManager
   - In NetPlacementController component
   - Drag BadmintonNet prefab to "Net Prefab" field

### Create UI Elements

1. **Create Canvas**
   - GameObject → UI → Canvas
   - Set Render Mode: Screen Space - Overlay
   - Add Canvas Scaler component
   - Set UI Scale Mode: Scale with Screen Size
   - Reference Resolution: 1080x1920

2. **Create Feedback Panel**
   ```
   Canvas → Right-click → UI → Panel
   Name: "FeedbackPanel"
   Rect Transform:
     - Anchor: Top Center
     - Pos Y: -200
     - Width: 600, Height: 200
   ```

3. **Add Feedback Text**
   ```
   FeedbackPanel → Right-click → UI → Text - TextMeshPro
   Name: "FeedbackText"
   Settings:
     - Font Size: 60
     - Alignment: Center Middle
     - Color: White
   ```

4. **Wire UI to Controller**
   - Select GameManager
   - In UIFeedbackController:
     - Drag FeedbackText to "Feedback Text"
     - Drag FeedbackPanel to "Feedback Panel"

## Platform Configuration

### iOS Configuration

1. **Switch Platform**
   - File → Build Settings
   - Select iOS
   - Click "Switch Platform"

2. **Player Settings** (Edit → Project Settings → Player)
   ```
   Company Name: Your Name
   Product Name: AR Badminton Net
   
   iOS Tab:
   - Identification:
     - Bundle Identifier: com.yourname.arbadmintonnet
   
   - Configuration:
     - Target minimum iOS Version: 13.0
     - Architecture: ARM64
     - Camera Usage Description: "Required for AR tracking and shuttle detection"
   
   - Other Settings:
     - Scripting Backend: IL2CPP
     - API Compatibility: .NET Standard 2.1
     - Target SDK: Device SDK
   ```

3. **XR Settings**
   - Edit → Project Settings → XR Plug-in Management
   - Click iOS tab
   - Enable: ARKit

### Android Configuration

1. **Switch Platform**
   - File → Build Settings
   - Select Android
   - Click "Switch Platform"

2. **Player Settings** (Edit → Project Settings → Player)
   ```
   Company Name: Your Name
   Product Name: AR Badminton Net
   
   Android Tab:
   - Identification:
     - Package Name: com.yourname.arbadmintonnet
   
   - Configuration:
     - Minimum API Level: Android 7.0 'Nougat' (API level 24)
     - Target API Level: Automatic (highest installed)
     - Scripting Backend: IL2CPP
     - API Compatibility: .NET Standard 2.1
     - Target Architectures: ARM64
   ```

3. **XR Settings**
   - Edit → Project Settings → XR Plug-in Management
   - Click Android tab
   - Enable: ARCore

4. **Android Manifest** (if needed)
   - Unity should auto-generate
   - Verify camera permissions are included

## Testing

### Test in Editor

1. **Install AR Foundation Remote** (optional)
   - Allows testing on device while editing
   - Available in Package Manager

2. **Use AR Simulation** (Unity 2022+)
   - Edit → Project Settings → XR Plug-in Management
   - Enable "AR Simulation"
   - Can test basic AR features in Editor

### Build to Device

#### iOS Build

1. **Connect Device**
   - Connect iPhone/iPad via USB
   - Trust computer if prompted

2. **Build**
   - File → Build Settings → iOS
   - Click "Build"
   - Choose output folder (e.g., `Builds/iOS`)
   - Wait for build to complete

3. **Deploy from Xcode**
   - Open generated `.xcodeproj` file
   - Select your device in device menu
   - In Signing & Capabilities:
     - Select your Team
   - Click Run (▶️) button
   - App installs and launches on device

#### Android Build

1. **Enable USB Debugging**
   - On Android device:
     - Settings → About Phone
     - Tap "Build Number" 7 times
     - Go back → Developer Options
     - Enable "USB Debugging"

2. **Connect Device**
   - Connect via USB
   - Allow USB debugging on device

3. **Build and Run**
   - File → Build Settings → Android
   - Check device appears in "Run Device" dropdown
   - Click "Build and Run"
   - Choose output location
   - App will install and launch automatically

## Verification

### Check AR Functionality

1. **Plane Detection**
   - Launch app
   - Point at floor/table
   - Move device slowly
   - Should see plane visualization

2. **Net Placement**
   - Tap on detected plane
   - Net should appear
   - Net should stay anchored

3. **Shuttle Detection**
   - Hold a yellow/white object (simulate shuttle)
   - Move it in front of camera
   - Check Unity logs for detection messages

4. **Collision Detection**
   - Move shuttle across placed net
   - Should hear sound
   - Should see UI message

## Troubleshooting Setup

### Unity Won't Open Project
- Ensure Unity version is 2022 LTS
- Delete `Library` folder and reopen
- Check Unity Hub logs

### Packages Won't Install
- Check internet connection
- Window → Package Manager → Advanced → Reset Packages
- Manually add packages by name

### Build Errors (iOS)
- Ensure Xcode is installed
- Update to latest Xcode
- Delete `iOS` build folder and rebuild

### Build Errors (Android)
- Verify Android SDK path in Unity preferences
- Update Android SDK tools
- Change API level if device incompatible

### AR Not Working on Device
- Verify device supports ARKit/ARCore
- Check camera permissions granted
- Ensure good lighting
- Try resetting AR session

## Next Steps

After successful setup:
1. ✅ Test net placement
2. ✅ Calibrate shuttle detection colors
3. ✅ Add your own audio files
4. ✅ Customize UI appearance
5. ✅ Test in various environments

Refer to README.md for usage guide and customization options.
