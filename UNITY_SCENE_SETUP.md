# Unity Scene Setup Guide - AR Badminton Net

## Overview
This guide will walk you through setting up the Unity scene for the AR Badminton Net Detection application. Follow these steps in order.

## Prerequisites
- Unity 2022 LTS installed
- ARBadmintonNet project opened in Unity
- All C# scripts are in place (no compilation errors)

---

## Step 1: Open/Create Main Scene

1. In Unity, go to **File → New Scene** or open `Assets/Scenes/MainScene.unity` if it exists
2. Select **3D (Built-in Render Pipeline)** or **3D (URP)** template
3. Save the scene as `MainScene.unity` in `Assets/Scenes/`

---

## Step 2: Setup XR Origin (AR Foundation)

### Create XR Origin
1. Delete the default **Main Camera** (we'll use AR Camera instead)
2. Right-click in Hierarchy → **XR → XR Origin (Mobile AR)**
3. This creates:
   ```
   XR Origin
   ├── Camera Offset
   │   └── Main Camera (AR Camera)
   ```

### Add AR Components to XR Origin
Select **XR Origin** GameObject and add these components:

1. **AR Plane Manager**
   - Click **Add Component** → Search "AR Plane Manager"
   - Settings:
     - Detection Mode: **Everything** (or **Horizontal** only)
     - Plane Prefab: *(optional - for visualization)*

2. **AR Raycast Manager**
   - Click **Add Component** → Search "AR Raycast Manager"

3. **AR Anchor Manager**  
   - Click **Add Component** → Search "AR Anchor Manager"

### Tag the AR Camera
1. Select **Main Camera** (under XR Origin → Camera Offset)
2. In Inspector, set Tag to **MainCamera**

---

## Step 3: Create AR Session

1. Right-click in Hierarchy → **XR → AR Session**
2. No configuration needed - this handles AR lifecycle

---

## Step 4: Create GameManager GameObject

1. Right-click in Hierarchy → **Create Empty**
2. Rename to **GameManager**
3. Reset Transform (Position: 0,0,0)

### Add All Manager Scripts
Select **GameManager** and add these components in order:

1. **Game Manager** (script)
2. **AR Session Manager** (script)
3. **AR Plane Detection Manager** (script)
4. **Net Placement Controller** (script)
5. **Color Based Tracker** (script)
6. **Net Collision Detector** (script)
7. **Feedback Manager** (script)
8. **Audio Manager** (script)
9. **UI Feedback Controller** (script)
10. **Audio Source** (component)
11. **Coordinate Converter** (script)
12. **Trajectory Predictor** (script)

---

## Step 5: Wire Up Script References

Now we need to connect references between scripts. Select **GameManager**:

### Game Manager Component
- **AR Session Manager**: Drag GameManager's `AR Session Manager` component
- **Plane Manager**: Drag `XR Origin` GameObject
- **Net Placement**: Drag GameManager's `Net Placement Controller` component
- **Shuttle Tracker**: Drag GameManager's `Color Based Tracker` component
- **Collision Detector**: Drag GameManager's `Net Collision Detector` component
- **Feedback Manager**: Drag GameManager's `Feedback Manager` component

### AR Session Manager Component
- **AR Session**: Drag `AR Session` GameObject from Hierarchy
- **Plane Manager**: Drag `XR Origin` GameObject

### AR Plane Detection Manager Component
- **Plane Manager**: This should auto-find, else drag `XR Origin`

### Net Placement Controller Component
- **AR Camera**: Drag `Main Camera` (under XR Origin)
- **Raycast Manager**: Drag `XR Origin`
- **Net Prefab**: *(Will assign after creating prefab - Step 6)*
- **Placement Indicator**: *(Will assign after creating prefab - Step 6)*
- **Placement Height**: 1.55 (meters)

### Color Based Tracker Component
- **AR Camera**: Drag `Main Camera`
- **Target Color**: Yellow (H: 0.1, S: 1, V: 1)
- **Frame Skip**: 2 (process every 2nd frame for performance)
- **Min Shuttle Size**: 30 pixels
- **Max Shuttle Size**: 150 pixels

### Net Collision Detector Component
- *(Net Object will be set automatically at runtime)*

### Feedback Manager Component
- **Audio Manager**: Drag GameManager's `Audio Manager` component
- **UI Controller**: Drag GameManager's `UI Feedback Controller` component
- **Hit Indicator Prefab**: *(Will assign after creating prefab - Step 7)*
- **Indicator Lifetime**: 2 seconds

### Audio Manager Component
- **Net Hit Sound**: *(Will assign after importing audio - Step 8)*
- **Placement Confirm Sound**: *(Will assign after importing audio - Step 8)*
- **Volume**: 1.0
- **Use Different Tones Per Side**: false (optional)

### Audio Source Component
- **Play On Awake**: Unchecked
- **Loop**: Unchecked
- **Spatial Blend**: 0 (2D sound)

### UI Feedback Controller Component
- **Feedback Text**: *(Will assign after creating UI - Step 9)*
- **Feedback Panel**: *(Will assign after creating UI - Step 9)*
- **Display Duration**: 2 seconds

### Coordinate Converter Component
- **AR Camera**: Drag `Main Camera`
- **Raycast Manager**: Drag `XR Origin`
- **Default Depth**: 2.0 meters

### Trajectory Predictor Component
- **History Size**: 10
- **Min Confidence Threshold**: 0.3
- **Enable Prediction**: Checked

---

## Step 6: Create Net Prefab

### Create Net GameObject
1. Right-click in Hierarchy → **3D Object → Cube**
2. Rename to **BadmintonNet**
3. Set Transform:
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: **(5.18, 1.55, 0.1)** ← Standard badminton net dimensions

### Create Net Material
1. In Project window, navigate to `Assets/Materials/`
2. Right-click → **Create → Material**
3. Name it **NetMaterial**
4. Settings:
   - **Rendering Mode**: Transparent (Standard shader) or **Surface Type**: Transparent (URP)
   - **Albedo/Base Map**: White (255, 255, 255)
   - **Alpha**: ~150 (or 0.6 normalized)
   - **Metallic**: 0
   - **Smoothness**: 0.3

5. Drag **NetMaterial** onto the **BadmintonNet** GameObject

### Add Mesh Collider
1. Select **BadmintonNet**
2. **Add Component** → **Mesh Collider**
3. Settings:
   - **Convex**: Unchecked ✗
   - **Is Trigger**: Unchecked ✗

### Create Prefab
1. Create folder `Assets/Prefabs/` if it doesn't exist
2. Drag **BadmintonNet** from Hierarchy to `Assets/Prefabs/`
3. Delete **BadmintonNet** from Hierarchy
4. Assign prefab to **Net Placement Controller**:
   - Select GameManager
   - Find `Net Placement Controller` component
   - Drag `BadmintonNet` prefab to **Net Prefab** field

### Create Placement Indicator Prefab (Optional but recommended)
1. Create **Quad** or **Plane** GameObject
2. Scale to (1, 1, 1)
3. Add simple material with pulsing/glowing effect
4. Save as `Assets/Prefabs/NetPlacementIndicator.prefab`
5. Assign to **Net Placement Controller → Placement Indicator** field

---

## Step 7: Create Hit Indicator Prefab

1. Right-click in Hierarchy → **3D Object → Sphere**
2. Rename to **HitIndicator**
3. Scale to (0.1, 0.1, 0.1) - small indicator
4. Create a bright material (Red or Yellow)
5. *(Optional)* Add **Particle System** for visual effect
6. Save as `Assets/Prefabs/HitIndicator.prefab`
7. Delete from Hierarchy
8. Assign to **Feedback Manager → Hit Indicator Prefab**

---

## Step 8: Import Audio Files

### Option 1: Use Provided Audio
If you have audio files:
1. Copy audio files to `Assets/Audio/`
2. Unity will auto-import
3. Assign in **Audio Manager** component

### Option 2: Placeholder Audio (For Testing)
Create silence/beep placeholders:
1. In `Assets/Audio/`, the setup script will create placeholder files
2. Or use free sounds from Unity Asset Store

### Assign Audio
1. Select **GameManager**
2. Find **Audio Manager** component
3. Drag audio clips:
   - **Net Hit Sound**: `net_hit.wav`
   - **Placement Confirm Sound**: `placement_confirm.wav`

---

## Step 9: Create UI Canvas

### Create Canvas
1. Right-click in Hierarchy → **UI → Canvas**
2. Canvas settings:
   - **Render Mode**: Screen Space - Overlay
   - **Pixel Perfect**: Checked (optional)

3. Add **Canvas Scaler** component (should be automatic):
   - **UI Scale Mode**: Scale with Screen Size
   - **Reference Resolution**: 1080 x 1920
   - **Match**: 0.5 (balance width/height)

### Create Feedback Panel
1. Right-click on **Canvas** → **UI → Panel**
2. Rename to **FeedbackPanel**
3. Set Rect Transform:
   - **Anchor Preset**: Top-Center (hold Alt+Shift while clicking)
   - **Pos Y**: -200
   - **Width**: 600
   - **Height**: 200

4. Set Panel background:
   - Color: Dark semi-transparent (e.g., rgba(0, 0, 0, 150))

### Create Feedback Text
1. Right-click on **FeedbackPanel** → **UI → Text - TextMeshPro**
   - If prompted to import TMP Essentials, click "Import"
2. Rename to **FeedbackText**
3. Settings:
   - **Text**: (leave empty)
   - **Font Size**: 60
   - **Alignment**: Center-Middle
   - **Color**: White
   - **Auto Size**: *(optional)* Check if text overflows

4. Make text fill panel:
   - Rect Transform → Anchor Preset: **Stretch-Stretch** (bottom-right icon)
   - **Left, Right, Top, Bottom**: all 0

### Create Instruction Text (Optional)
1. Right-click on **Canvas** → **UI → Text - TextMeshPro**
2. Rename to **InstructionText**
3. Set to bottom-center
4. Text: "Tap on a surface to place net"
5. Font size: 36

### Wire UI References
1. Select **GameManager**
2. Find **UI Feedback Controller** component
3. Assign:
   - **Feedback Text**: Drag `FeedbackText` from Hierarchy
   - **Feedback Panel**: Drag `FeedbackPanel` from Hierarchy

---

## Step 10: Configure Project Settings

### XR Plug-in Management

#### For iOS:
1. **Edit → Project Settings → XR Plug-in Management**
2. Click **iOS** tab (phone icon)
3. Enable: **☑ ARKit**

#### For Android:
1. Same menu, click **Android** tab (robot icon)
2. Enable: **☑ ARCore**

### Player Settings

#### iOS:
1. **Edit → Project Settings → Player → iOS tab**
2. **Other Settings:**
   - **Camera Usage Description**: "Required for AR tracking and shuttle detection"
   - **Architecture**: ARM64
   - **Target minimum iOS Version**: 13.0+
3. **Identification:**
   - **Bundle Identifier**: com.yourname.arbadmintonnet (must be unique)

#### Android:
1. **Edit → Project Settings → Player → Android tab**
2. **Other Settings:**
   - **Minimum API Level**: Android 7.0 (API 24)
   - **Target API Level**: Automatic
   - **Scripting Backend**: IL2CPP
   - **Target Architectures**: ARM64 ✓
3. **Identification:**
   - **Package Name**: com.yourname.arbadmintonnet (must be unique)

---

## Step 11: Test in Editor (Optional)

Unity 2022+ supports AR simulation:

1. **Window → XR → AR Foundation → AR Debug Menu**
2. In Play mode, you can simulate AR features
3. Or use **Device Simulator** for mobile screen testing

**Note**: Full AR testing requires a physical device.

---

## Step 12: Build to Device

### iOS Build:
1. **File → Build Settings**
2. Select **iOS** → **Switch Platform**
3. Click **Build** (or Build and Run if device connected)
4. Open Xcode project from build folder
5. Select device, configure signing, and Run

### Android Build:
1. **File → Build Settings**
2. Select **Android** → **Switch Platform**
3. Connect device via USB (USB debugging enabled)
4. Click **Build and Run**

See [BUILD_GUIDE.md](file:///Users/gauravrai/projects/ARBadmintonNet/BUILD_GUIDE.md) for detailed build instructions.

---

## Verification Checklist

Before building, verify:

- [ ] No errors in Unity Console
- [ ] All script references in GameManager are assigned (no "None" or "Missing")
- [ ] BadmintonNet prefab exists and is assigned
- [ ] UI elements are created and wired up
- [ ] Audio files are imported (or placeholders ready)
- [ ] XR Plug-in Management configured for target platform
- [ ] Player Settings configured (bundle ID, API levels, camera permissions)

---

## Troubleshooting

**"Missing script reference"**
- Ensure all scripts compiled without errors
- Check script name matches class name exactly
- Drag components from correct GameObject

**"AR Session won't start"**
- Verify XR Plug-in Management is configured
- Check device supports ARKit/ARCore
- Ensure camera permissions in Player Settings

**"Plane detection not working"**
- AR Plane Manager must be on XR Origin object
- Ensure good lighting and textured surfaces
- Move device slowly to help tracking

---

## Next Steps

After scene setup is complete:
1. Build to device
2. Test net placement
3. Test shuttle detection
4. Verify collision detection
5. Iterate and tune parameters

Refer to [implementation_plan.md](file:///Users/gauravrai/.gemini/antigravity/brain/f0c7f11f-c02b-4053-a4cc-ee924d2a25a6/implementation_plan.md) for testing procedures.
