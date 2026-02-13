# AR Badminton Net Detection

An augmented reality mobile application that places a virtual badminton net in your environment and detects when a shuttle hits it, providing real-time audio and visual feedback.

## 🎯 Features

- **AR Net Placement**: Place a virtual badminton net in your environment with precise positioning
- **Real-time Shuttle Detection**: Detects shuttles using computer vision (color-based tracking)
- **Collision Detection**: Accurately detects when shuttle hits the net using trajectory prediction
- **Side Detection**: Identifies which side (A or B) hit the net
- **Audio Feedback**: Plays sound when net is hit (optional different tones per side)
- **Visual Feedback**: Shows visual indicator at collision point
- **UI Feedback**: Displays which side hit the net with color-coded messages

## 🛠️ Technical Stack

- **Unity 2022 LTS** - Game engine
- **AR Foundation 5.x** - Cross-platform AR framework
- **ARKit** - iOS AR support
- **ARCore** - Android AR support
- **C#** - Programming language

## 📋 Requirements

### For iOS Development
- macOS (for Xcode)
- Xcode 14 or later
- iOS device with ARKit support (iPhone 6S or newer, iOS 13+)
- Apple Developer Account (for device deployment)

### For Android Development
- Android device with ARCore support ([Check device list](https://developers.google.com/ar/devices))
- Android SDK (API Level 24 or higher)
- USB debugging enabled on device

### Unity Setup
- Unity 2022 LTS or later
- Unity Hub
- iOS Build Support module (for iOS)
- Android Build Support module (for Android)

## 🚀 Quick Start

### 1. Open Project in Unity

1. Install Unity Hub
2. Install Unity 2022 LTS from Unity Hub
3. Click "Add" in Unity Hub and select this project folder
4. Open the project

### 2. Install Required Packages

The project uses `Packages/manifest.json` to specify dependencies. Unity will automatically install:
- AR Foundation 5.1.0
- ARKit XR Plugin 5.1.0
- ARCore XR Plugin 5.1.0
- Barracuda 3.0.0 (for future ML features)
- TextMeshPro
- Input System

If packages don't install automatically:
1. Open Unity
2. Go to **Window → Package Manager**
3. Install the packages listed above

### 3. Setup the Main Scene

1. Open `Assets/Scenes/MainScene.unity` (you'll need to create this)
2. Add the following GameObjects:

#### AR Foundation Setup
- Create **XR Origin** (GameObject → XR → XR Origin)
  - This includes the AR Camera and AR Session
- Add **AR Plane Manager** component to XR Origin
- Add **AR Raycast Manager** component to XR Origin

#### Game Components
- Create empty GameObject named "**GameManager**"
  - Add `GameManager.cs` script
  - Add `ARSessionManager.cs` script
  - Add `ARPlaneDetectionManager.cs` script
  - Add `NetPlacementController.cs` script
  - Add `ColorBasedTracker.cs` script
  - Add `NetCollisionDetector.cs` script
  - Add `FeedbackManager.cs` script
  - Add `AudioManager.cs` script (also add AudioSource component)
  - Add `UIFeedbackController.cs` script

### 4. Create the Net Prefab

1. Create a 3D object for the net:
   - GameObject → 3D Object → Cube
   - Set scale to (5.18, 1.55, 0.1) for standard badminton net dimensions
   - Add semi-transparent material (Alpha ~0.6)
   - Add Mesh Collider component (uncheck "Convex")
2. Save as Prefab: Drag to `Assets/Prefabs/BadmintonNet.prefab`
3. Assign to NetPlacementController's "Net Prefab" field

### 5. Build for iOS

1. Switch platform: **File → Build Settings → iOS → Switch Platform**
2. Click **Player Settings**:
   - Set Bundle Identifier (e.g., `com.yourname.arbadminton`)
   - Set Camera Usage Description: "Camera access for AR"
   - Set Target iOS Version: 13.0 or higher
3. Click **Build** and select output folder
4. Open generated Xcode project
5. In Xcode:
   - Select your development team
   - Connect iOS device
   - Click Run (▶️)

### 6. Build for Android

1. Switch platform: **File → Build Settings → Android → Switch Platform**
2. Click **Player Settings**:
   - Set Package Name (e.g., `com.yourname.arbadminton`)
   - Set Minimum API Level: 24
   - Under XR Settings, ensure ARCore is enabled
3. Connect Android device with USB debugging
4. Click **Build and Run**

## 📖 Usage Guide

### Placing the Net

1. Launch the app
2. Point camera at a flat surface (ground, floor, table)
3. Move device slowly to help AR detect the plane
4. Tap on the detected surface to place the net
5. Net will lock in position automatically

### Playing

1. After net is placed, shuttle detection starts automatically
2. The app will track yellow/white shuttles in the camera view
3. When shuttle crosses the net:
   - You'll hear a sound
   - A visual indicator appears at the collision point
   - UI shows which side hit the net (Side A or Side B)

### Tips for Best Results

- **Good Lighting**: Ensure adequate lighting for better shuttle detection
- **Plain Background**: Avoid cluttered backgrounds for better tracking
- **Steady Net Placement**: Place net on stable, flat surface
- **Clear View**: Keep shuttle visible in camera frame
- **Distance**: Stand 3-5 meters from the net for optimal detection

## 🎨 Customization

### Adjust Shuttle Color Detection

Edit `ColorBasedTracker.cs`:
```csharp
// Change target color (default: yellow)
targetColor = Color.yellow;

// Adjust HSV thresholds
hueThreshold = 15f;        // ±degrees
saturationMin = 0.4f;      // 0-1
valueMin = 0.4f;           // 0-1
```

### Modify Net Appearance

Edit `NetConfiguration.cs` or adjust in Inspector:
- Width, Height, Thickness
- Transparency (0-1)
- Net Color

### Audio Settings

Assign your own audio clips in `AudioManager`:
- Net hit sound
- Placement confirmation sound
- Enable different tones per side

## 🏗️ Project Structure

```
Assets/
├── Scenes/              # Unity scenes
├── Scripts/
│   ├── AR/              # AR session and plane detection
│   ├── Detection/       # Shuttle tracking algorithms
│   ├── Collision/       # Net collision detection
│   ├── Feedback/        # Audio/visual/UI feedback
│   ├── Models/          # Data structures
│   └── Utilities/       # Helper classes
├── Prefabs/             # Reusable game objects
├── Materials/           # Visual materials
└── Audio/               # Sound effects
```

## 🔍 How It Works

### Architecture Overview

1. **AR Session**: Initializes AR tracking and plane detection
2. **Net Placement**: User taps to place virtual net on detected plane
3. **Shuttle Detection**: Color-based tracking finds shuttle in camera feed
4. **3D Position**: Converts 2D screen position to 3D world coordinates
5. **Trajectory Tracking**: Kalman filter smooths position and predicts movement
6. **Collision Detection**: Line-plane intersection checks if shuttle crosses net
7. **Side Detection**: Determines which side the shuttle came from
8. **Feedback**: Triggers audio, visual, and UI responses

### Detection Algorithm

```
1. Capture camera frame
2. Convert to HSV color space
3. Apply color thresholding (yellow shuttle)
4. Find contours and centroid
5. Validate size constraints
6. Convert 2D → 3D position using raycasting
7. Apply Kalman filtering for smoothing
8. Check trajectory against net plane
9. Detect collision and determine side
10. Trigger feedback
```

## 🐛 Troubleshooting

### AR Session Not Starting
- Ensure device supports ARKit (iOS) or ARCore (Android)
- Check camera permissions are granted
- Verify AR Foundation packages are installed

### Net Won't Place
- Move device to scan environment
- Ensure adequate lighting
- Look for flat, textured surfaces

### Shuttle Not Detected
- Check lighting conditions
- Verify shuttle color matches target (yellow/white)
- Adjust color thresholds in `ColorBasedTracker`
- Ensure shuttle is in camera view

### Collisions Not Detected
- Verify net is placed and locked
- Check if shuttle detection is working (Debug logs)
- Ensure shuttle passes through net area
- Review collision threshold settings

### Performance Issues
- Reduce detection frame skip value
- Lower camera resolution
- Disable debug visualization
- Use lighter 3D models

## 🚧 Future Enhancements

### Phase 2 Features (Planned)
- **ML-Based Detection**: YOLOv8-nano model for better accuracy
- **Improved Depth Estimation**: More accurate 3D positioning
- **Score Tracking**: Keep track of hits per side
- **Multi-Player Mode**: Support for match scoring
- **Replay System**: Record and replay detected hits
- **Settings Menu**: User-adjustable parameters

### Phase 3 Features (Ideas)
- **Multiple Net Support**: Practice with multiple nets
- **AR Coaching**: Visual guides for proper play area
- **Statistics**: Track accuracy, speed, patterns
- **Cloud Integration**: Save and share sessions
- **Social Features**: Challenges and leaderboards

## 📄 License

This project is provided as-is for educational and personal use.

## 🙏 Acknowledgments

- Unity AR Foundation team
- ARKit and ARCore developers
- Computer vision community

## 📞 Support

For issues or questions:
1. Check troubleshooting section
2. Review Unity console for error messages
3. Ensure all requirements are met
4. Test on different devices/conditions

---

**Built with Unity & AR Foundation**
# ARBadminton
