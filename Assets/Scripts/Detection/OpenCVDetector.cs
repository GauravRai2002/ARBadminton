using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Detection
{
    public class OpenCVDetector : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Camera arCamera;
        // Force Reimport

        [Header("Detection Settings")]
        [SerializeField] private float motionThreshold = 0.25f; // Increased to ignore camera noise
        [SerializeField] private int minChangedPixels = 300;    // Increased to ignore specks
        [SerializeField] private float maxMotionCoverage = 0.35f; // New: Ignore if >35% of screen moves (Lighting change)
        [SerializeField] private float downscaleFactor = 0.15f; 
        [SerializeField] private int processEveryNFrames = 2; 

        [Header("Debug")]
        [SerializeField] private bool showDebugVisuals = true;

        // Events
        public delegate void ShuttleDetectedHandler(ShuttleData shuttleData);
        public event ShuttleDetectedHandler OnShuttleDetected;

        // Internal State
        private int frameCount = 0;
        private Color32[] previousFrameColors;
        private int processingWidth;
        private int processingHeight;
        
        // Debug
        private List<Rect> debugRects = new List<Rect>();

        // Simple class to track clusters of motion
        private class MotionCluster
        {
            public float SumX, SumY;
            public int PixelCount;
            public int MinX, MaxX, MinY, MaxY;

            public MotionCluster(int x, int y)
            {
                SumX = x; SumY = y;
                PixelCount = 1;
                MinX = MaxX = x;
                MinY = MaxY = y;
            }

            public void AddPixel(int x, int y)
            {
                SumX += x; SumY += y;
                PixelCount++;
                if (x < MinX) MinX = x;
                if (x > MaxX) MaxX = x;
                if (y < MinY) MinY = y;
                if (y > MaxY) MaxY = y;
            }

            public bool IsClose(int x, int y, float threshold)
            {
                // Simple distance check to bounding box center approximation
                float centerX = SumX / PixelCount;
                float centerY = SumY / PixelCount;
                float distSq = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                return distSq < (threshold * threshold);
            }
        }

        private void Start()
        {
            Debug.Log("[OpenCVDetector] Start() called.");
            if (cameraManager == null) cameraManager = FindObjectOfType<ARCameraManager>();
            if (arCamera == null) arCamera = Camera.main;

            // Auto-register with Game Manager to fix loose connections
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.RegisterDetector(this);
            }
            else
            {
                Debug.LogError("[OpenCVDetector] GameManager NOT FOUND! Events will trigger but nobody is listening.");
            }
        }

        private void OnEnable()
        {
            Debug.Log("[OpenCVDetector] OnEnable() called.");
            // Event unreliable on some Vulkan devices, using Update() loop instead
        }

        private void Update()
        {
             // Heartbeat for debug
             if (Time.frameCount % 60 == 0) 
             {
                 Debug.Log($"[OpenCVDetector Status] Enabled: {this.enabled}, CamMgr: {(cameraManager != null ? "OK" : "NULL")}, Frames: {frameCount}");
             }

            // Manual Frame Throttling
            frameCount++;
            if (frameCount % processEveryNFrames == 0)
            {
                ProcessRenderTexture();
            }
        }

        private void OnDisable()
        {
            // Nothing to unsubscribe
        }

        private void ProcessRenderTexture()
        {
            if (arCamera == null) return;
            
            var bg = cameraManager.GetComponent<ARCameraBackground>();
            if (bg == null) bg = FindObjectOfType<ARCameraBackground>();

            if (bg != null && bg.material != null)
            {
                // 1. Determine low-res dimensions
                int targetW = (int)(arCamera.pixelWidth * downscaleFactor);
                int targetH = (int)(arCamera.pixelHeight * downscaleFactor);
                
                // Clamp to reasonable minimums
                if (targetW < 10) targetW = 10;
                if (targetH < 10) targetH = 10;

                // 2. Capture Screen/Camera to RenderTexture
                // Fix: Metal requires depth buffer if shader writes to it (even if we don't use it)
                RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 16, RenderTextureFormat.ARGB32);
                Graphics.Blit(null, rt, bg.material);
                
                // 3. Read Pixels to Texture2D
                Texture2D texture = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
                texture.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                // 4. Process Logic (Pure C#)
                DetectMotion(texture.GetPixels32(), targetW, targetH);
                
                Destroy(texture); // Cleanup
            }
            else
            {
                // Silent fallback or infrequent warning to avoid spam
                if (frameCount % 120 == 0) 
                   Debug.LogWarning($"[OpenCVDetector] ARCameraBackground missing.");
                 
                 bg = FindObjectOfType<ARCameraBackground>();
            }
        }

        private void DetectMotion(Color32[] currentColors, int width, int height)
        {
            // Initialize previous frame if needed
            if (previousFrameColors == null || previousFrameColors.Length != currentColors.Length)
            {
                previousFrameColors = currentColors;
                processingWidth = width;
                processingHeight = height;
                return;
            }

            // Compare frames
            long sumX = 0, sumY = 0;
            int changedPixelsCount = 0;
            
            // Optimization: Step loop 
            int step = 2; 

            // Threshold in byte value (0-765 total diff)
            int threshVal = (int)(765 * motionThreshold);

            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int i = y * width + x;
                    Color32 c1 = currentColors[i];
                    Color32 c2 = previousFrameColors[i];

                    // Simple Manhattan distance for color diff
                    int diff = Mathf.Abs(c1.r - c2.r) + Mathf.Abs(c1.g - c2.g) + Mathf.Abs(c1.b - c2.b);
                    
                    if (diff > threshVal) 
                    {
                        changedPixelsCount++;
                        sumX += x;
                        sumY += y;
                    }
                }
            }

            // Store current as previous for next frame
            previousFrameColors = currentColors;

            // IGNORE if too much motion (likely global lighting / camera shake)
            int totalPxSampled = (width / step) * (height / step);
            if (changedPixelsCount > totalPxSampled * maxMotionCoverage)
            {
                // Too much motion!
                debugRects.Clear();
                return;
            }

            // Check if motion is significant
            if (changedPixelsCount > (minChangedPixels / (step * step)))
            {
                // CENTER OF MASS LOGIC to avoid "Full Screen" boxes
                float rawCenterX = (float)sumX / changedPixelsCount;
                float rawCenterY = (float)sumY / changedPixelsCount;

                // Calculate Mean Deviation (Spread) to determine box size
                float sumDevX = 0, sumDevY = 0;
                // Re-iterating is too slow, so we approximate spread based on density
                // For a denser cluster (shuttle), deviation is low. For noise, it's high.
                // Simplified: We just define a fixed "Interaction Size" around the center.
                
                // Let's use a dynamic size based on an assumption of the object size at 3m distance
                // Or simply clamp the min/max if we calculated them. 
                // Since we didn't calculate min/max in the loop above (to save perf?), we construct a box.
                
                // Wait, let's use a semi-fixed size box for better UX
                float boxRadius = Mathf.Sqrt(changedPixelsCount) * step * 1.5f; 
                // Ensure box isn't HUGE
                boxRadius = Mathf.Clamp(boxRadius, 20f, width * 0.2f); // Max 20% of screen width

                float minX = rawCenterX - boxRadius;
                float maxX = rawCenterX + boxRadius;
                float minY = rawCenterY - boxRadius;
                float maxY = rawCenterY + boxRadius;

                // Convert to Screen Coords
                float scaleX = arCamera.pixelWidth / (float)width;
                float scaleY = arCamera.pixelHeight / (float)height;

                float screenX = minX * scaleX;
                float screenY = minY * scaleY;
                float screenW = (maxX - minX) * scaleX;
                float screenH = (maxY - minY) * scaleY;

                Rect screenRect = new Rect(screenX, screenY, screenW, screenH);
                
                // Store for debug drawing
                if (showDebugVisuals)
                {
                    debugRects.Clear();
                    debugRects.Add(screenRect);
                    Debug.Log($"[Motion] Detected! Px: {changedPixelsCount}");
                }

                // Construct ShuttleData
                Vector2 centerScreenPos = screenRect.center;
                
                // Estimate world position (Raycast to 3m depth)
                Vector3 worldPos = Vector3.zero;
                if (arCamera != null)
                {
                    Ray ray = arCamera.ScreenPointToRay(centerScreenPos);
                    worldPos = ray.GetPoint(3.0f); // Default depth assumption
                }

                ShuttleData data = new ShuttleData
                {
                    ScreenPosition = centerScreenPos,
                    Position = worldPos, // World position for collision
                    BoundingBox = screenRect,
                    Confidence = Mathf.Clamp01((float)changedPixelsCount / 1000f),
                    Method = DetectionMethod.OpenCV 
                };

                OnShuttleDetected?.Invoke(data);
            }
            else
            {
                debugRects.Clear();
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugVisuals || debugRects == null) return;

            GUI.skin.box.fontSize = 20;
            GUI.skin.box.fontStyle = FontStyle.Bold;
            GUI.color = Color.red;
            
            foreach (var rect in debugRects)
            {
                // GUI coordinates are Top-Left origin. Screen Rect is Bottom-Left.
                // Convert Y:
                float guiY = Screen.height - (rect.y + rect.height);
                
                GUI.Box(new Rect(rect.x, guiY, rect.width, rect.height), "Motion");
            }
        }
        
        // Stub for ModeSelectionUI compatibility
        public void SetTargetColor(Color color)
        {
            // No-op for motion detection
        }
    }
}
