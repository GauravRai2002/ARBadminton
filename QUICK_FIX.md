# Quick Rebuild Instructions

## What Was Fixed:

1. **Net Anchoring**: Fixed AR anchor to prevent net from moving with camera
2. **Net Visibility**: Added script to make net bright green and opaque

## Steps to Rebuild:

### 1. Add Visibility Script to Prefab

1. In Unity, navigate to **Assets/Prefabs**
2. **Double-click BadmintonNet** prefab to edit it
3. With BadmintonNet selected, click **Add Component**
4. Search for **Net Visibility Helper**
5. Add it
6. **Save** the prefab (File → Save or Ctrl/Cmd+S)

### 2. Rebuild for iPhone

1. **File → Build Settings**
2. Click **Build** (overwrite the existing build folder)
3. Wait for build to complete
4. **Open the Xcode project**
5. **Run** on your iPhone 16 Pro

### 3. Test the App

Now when you tap to place the net:
- ✅ Net will be **bright green** and clearly visible
- ✅ Net will **stay in place** and not move with the camera
- ✅ Net will be **anchored to the real world**

## Expected Behavior:

1. Open app → Camera starts
2. Move phone to detect surfaces (white planes appear)
3. **Tap on floor** → Bright green net appears
4. **Net stays fixed** in that location
5. Move camera around → Net remains anchored to world

---

**Rebuild now and test!** The net will be visible and stable. 🎯
