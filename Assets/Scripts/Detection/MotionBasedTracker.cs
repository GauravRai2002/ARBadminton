using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Detection
{
    /// <summary>
    /// Detects any moving object in the AR camera feed using frame differencing.
    /// Draws a visible bounding box on screen around detected motion.
    /// </summary>
    public class MotionBasedTracker : MonoBehaviour
    {
        [Header("Motion Detection Settings")]
        [SerializeField] private float motionThreshold = 30f; // Brightness change threshold per pixel
        [SerializeField] private int minMotionPixels = 50; // Min motion pixels to trigger detection
        [SerializeField] private int maxMotionPixels = 5000; // Max motion pixels (rejects camera shake)
        [SerializeField] private float maxBoundingBoxCoverage = 0.4f; // Max fraction of frame a bbox can cover

        [Header("Performance")]
        [SerializeField] private int detectionFrameSkip = 3; // Process every N frames
        [SerializeField] private int downscaleFactor = 8;

        [Header("3D Estimation")]
        [SerializeField] private float estimatedDepth = 3f;

        [Header("Cooldown")]
        [SerializeField] private float detectionCooldown = 0.5f;

        [Header("Debug Visualization")]
        [SerializeField] private bool showBoundingBox = true;
        [SerializeField] private Color boxColor = Color.green;
        [SerializeField] private float boxDisplayDuration = 0.5f;

        private Camera arCamera;
        private ARCameraManager cameraManager;
        private float[] previousFrame;
        private int prevWidth;
        private int prevHeight;
        private int frameCounter = 0;
        private float lastDetectionTime = -10f;
        private Vector2 lastMotionCentroid;
        private bool hasPreviousFrame = false;

        // Debug overlay state
        private Rect currentBoundingBox;
        private float boxDisplayUntil = 0f;
        private int lastMotionPixelCount = 0;
        private string debugStatus = "Initializing...";
        private int framesProcessed = 0;
        private int framesWithMotion = 0;
        private Texture2D boxTexture;

        public delegate void ShuttleDetectedHandler(ShuttleData shuttleData);
        public event ShuttleDetectedHandler OnShuttleDetected;

        private void Awake()
        {
            arCamera = Camera.main;
            cameraManager = FindObjectOfType<ARCameraManager>();

            // Create a 1x1 white texture for drawing boxes
            boxTexture = new Texture2D(1, 1);
            boxTexture.SetPixel(0, 0, Color.white);
            boxTexture.Apply();
        }

        private void OnEnable()
        {
            // DISABLED for debugging OpenCVDetector
            // if (cameraManager != null)
            // {
            //     cameraManager.frameReceived += OnCameraFrameReceived;
            //     debugStatus = "Subscribed to AR camera frames";
            // }
            // else
            // {
            //     debugStatus = "No ARCameraManager - using render capture fallback";
            // }
        }

        private void OnDisable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            frameCounter++;
            if (frameCounter % detectionFrameSkip != 0)
                return;

            // Try to get CPU image from AR camera
            // if (cameraManager.TryAcquireLatestCpuImage(out var cpuImage))
            // {
            //     // Process the image silently
            //     ProcessCpuImage(cpuImage);
            //     cpuImage.Dispose();
            // }
            // else
            // {
            //     debugStatus = "TryAcquireLatestCpuImage FAILED (Disabled)";
            // }

            // Always use Render Capture fallback for now (Vulkan fix)
            ProcessRenderCapture();
        }

        private void Update()
        {
            // Fallback: if no ARCameraManager, use render-based capture
            if (cameraManager == null)
            {
                frameCounter++;
                if (frameCounter % detectionFrameSkip != 0)
                    return;

                ProcessRenderCapture();
            }

            // Periodic status log (roughly every 10 seconds)
            if (frameCounter % 600 == 1)
            {
                Debug.Log($"[MotionTracker] Processed: {framesProcessed} | Motions: {framesWithMotion} | thresh={motionThreshold}");
            }
        }

        private void ProcessCpuImage(UnityEngine.XR.ARSubsystems.XRCpuImage cpuImage)
        {
            int scaledWidth = cpuImage.width / downscaleFactor;
            int scaledHeight = cpuImage.height / downscaleFactor;

            if (scaledWidth < 10 || scaledHeight < 10)
            {
                debugStatus = $"Image too small after downscale: {scaledWidth}x{scaledHeight}";
                return;
            }

            try
            {
                var conversionParams = new UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                    outputDimensions = new Vector2Int(scaledWidth, scaledHeight),
                    outputFormat = TextureFormat.R8,
                    transformation = UnityEngine.XR.ARSubsystems.XRCpuImage.Transformation.None
                };

                int bufferSize = cpuImage.GetConvertedDataSize(conversionParams);
                var buffer = new NativeArray<byte>(bufferSize, Allocator.Temp);

                cpuImage.Convert(conversionParams, buffer);

                float[] currentFrame = new float[scaledWidth * scaledHeight];
                for (int i = 0; i < buffer.Length && i < currentFrame.Length; i++)
                {
                    currentFrame[i] = buffer[i];
                }

                buffer.Dispose();

                DetectMotion(currentFrame, scaledWidth, scaledHeight);
            }
            catch (System.Exception e)
            {
                debugStatus = $"CPU image error: {e.Message}";
                Debug.LogError($"[MotionTracker] {debugStatus}");
            }
        }

        private void ProcessRenderCapture()
        {
            if (arCamera == null)
                return;

            int scaledWidth = Screen.width / downscaleFactor;
            int scaledHeight = Screen.height / downscaleFactor;

            if (scaledWidth < 10 || scaledHeight < 10)
                return;

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture tempRT = RenderTexture.GetTemporary(scaledWidth, scaledHeight, 24);

            arCamera.targetTexture = tempRT;
            arCamera.Render();

            RenderTexture.active = tempRT;
            Texture2D tex = new Texture2D(scaledWidth, scaledHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, scaledWidth, scaledHeight), 0, 0);
            tex.Apply();

            arCamera.targetTexture = null;
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(tempRT);

            Color[] pixels = tex.GetPixels();
            float[] currentFrame = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                currentFrame[i] = (pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f) * 255f;
            }

            Destroy(tex);

            DetectMotion(currentFrame, scaledWidth, scaledHeight);
        }

        private void DetectMotion(float[] currentFrame, int width, int height)
        {
            framesProcessed++;

            if (!hasPreviousFrame || prevWidth != width || prevHeight != height)
            {
                previousFrame = currentFrame;
                prevWidth = width;
                prevHeight = height;
                hasPreviousFrame = true;
                debugStatus = $"First frame stored: {width}x{height}";
                return;
            }

            // Frame differencing
            List<Vector2Int> motionPixels = new List<Vector2Int>();
            float maxDiff = 0;
            float totalDiff = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    float diff = Mathf.Abs(currentFrame[idx] - previousFrame[idx]);
                    totalDiff += diff;
                    if (diff > maxDiff) maxDiff = diff;

                    if (diff > motionThreshold)
                    {
                        motionPixels.Add(new Vector2Int(x, y));
                    }
                }
            }

            previousFrame = currentFrame;

            float avgDiff = totalDiff / (width * height);
            int motionCount = motionPixels.Count;

            // Update debug status (no logging)
            if (framesProcessed % 10 == 0)
            {
                debugStatus = $"Frame {framesProcessed}: {motionCount} motion px, avg={avgDiff:F1}, max={maxDiff:F1}";
            }

            // Check cooldown
            if (Time.time - lastDetectionTime < detectionCooldown)
            {
                return;
            }

            // Check if we have meaningful motion
            if (motionCount < minMotionPixels)
            {
                return;
            }

            if (motionCount > maxMotionPixels)
            {
                debugStatus = $"Rejected: too many motion pixels ({motionCount}) — likely camera shake";
                return;
            }

            framesWithMotion++;

            // Calculate centroid
            Vector2 centroid = Vector2.zero;
            foreach (var p in motionPixels)
            {
                centroid += new Vector2(p.x, p.y);
            }
            centroid /= motionCount;

            // Calculate bounding box in downscaled coords
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var p in motionPixels)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            // Scale to screen coordinates
            float screenX = centroid.x * downscaleFactor;
            float screenY = centroid.y * downscaleFactor;
            Vector2 screenPos = new Vector2(screenX, screenY);

            // Bounding box in screen coordinates
            Rect screenBoundingBox = new Rect(
                minX * downscaleFactor,
                minY * downscaleFactor,
                (maxX - minX + 1) * downscaleFactor,
                (maxY - minY + 1) * downscaleFactor
            );

            // Reject bounding boxes that cover too much of the frame
            float frameArea = Screen.width * Screen.height;
            float bboxArea = screenBoundingBox.width * screenBoundingBox.height;
            if (frameArea > 0 && (bboxArea / frameArea) > maxBoundingBoxCoverage)
            {
                debugStatus = $"Rejected: bbox covers {(bboxArea / frameArea * 100):F0}% of frame (max {maxBoundingBoxCoverage * 100:F0}%)";
                return;
            }

            // Update debug overlay
            currentBoundingBox = screenBoundingBox;
            boxDisplayUntil = Time.time + boxDisplayDuration;
            lastMotionPixelCount = motionCount;

            debugStatus = $"MOTION! {motionCount}px at ({screenX:F0},{screenY:F0}) box=({screenBoundingBox.width:F0}x{screenBoundingBox.height:F0})";

            // Convert to 3D world position
            if (arCamera != null)
            {
                Ray ray = arCamera.ScreenPointToRay(screenPos);
                Vector3 worldPos = ray.GetPoint(estimatedDepth);

                float motionRatio = (float)motionCount / (width * height);
                float confidence = Mathf.Clamp01(motionRatio * 10f);

                var shuttleData = new ShuttleData
                {
                    Position = worldPos,
                    ScreenPosition = screenPos,
                    BoundingBox = screenBoundingBox,
                    Confidence = confidence,
                    Method = DetectionMethod.ColorTracking
                };

                if (lastMotionCentroid != Vector2.zero)
                {
                    float deltaTime = Time.deltaTime * detectionFrameSkip;
                    if (deltaTime > 0)
                    {
                        Vector2 displacement = screenPos - lastMotionCentroid;
                        shuttleData.Velocity = new Vector3(
                            displacement.x / deltaTime,
                            displacement.y / deltaTime,
                            0
                        );
                    }
                }

                lastMotionCentroid = screenPos;
                lastDetectionTime = Time.time;

                OnShuttleDetected?.Invoke(shuttleData);
            }
        }

        /// <summary>
        /// Draw bounding box overlay on screen using OnGUI
        /// </summary>
        private void OnGUI()
        {
#if UNITY_EDITOR
            if (!showBoundingBox)
                return;

            // Always show debug status text at top of screen
            GUI.color = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 28;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;

            // Background box for readability
            float statusHeight = 120f;
            GUI.Box(new Rect(10, 40, Screen.width - 20, statusHeight), "");
            GUI.Label(new Rect(20, 50, Screen.width - 40, 35), $"Motion Tracker: {debugStatus}", style);
            GUI.Label(new Rect(20, 85, Screen.width - 40, 35), $"Frames: {framesProcessed} | Motions: {framesWithMotion} | Last: {lastMotionPixelCount}px", style);
            GUI.Label(new Rect(20, 120, Screen.width - 40, 35), $"Threshold: {motionThreshold} | MinPx: {minMotionPixels} | Skip: {detectionFrameSkip}", style);

            // Draw bounding box if we have recent motion
            if (Time.time < boxDisplayUntil)
            {
                // Convert from bottom-left screen coords to GUI top-left coords
                float guiX = currentBoundingBox.x;
                float guiY = Screen.height - currentBoundingBox.y - currentBoundingBox.height;
                float guiW = currentBoundingBox.width;
                float guiH = currentBoundingBox.height;

                // Draw border (4 rectangles forming a frame)
                int borderWidth = 4;
                GUI.color = boxColor;

                // Top
                GUI.DrawTexture(new Rect(guiX, guiY, guiW, borderWidth), boxTexture);
                // Bottom
                GUI.DrawTexture(new Rect(guiX, guiY + guiH - borderWidth, guiW, borderWidth), boxTexture);
                // Left
                GUI.DrawTexture(new Rect(guiX, guiY, borderWidth, guiH), boxTexture);
                // Right
                GUI.DrawTexture(new Rect(guiX + guiW - borderWidth, guiY, borderWidth, guiH), boxTexture);

                // Label
                GUI.color = Color.white;
                style.fontSize = 24;
                style.normal.textColor = boxColor;
                GUI.Label(new Rect(guiX, guiY - 35, 400, 30), $"Motion: {lastMotionPixelCount}px", style);
            }
#endif
        }

        public void SetMotionThreshold(float threshold)
        {
            motionThreshold = threshold;
        }

        public void SetMinMotionPixels(int minPixels)
        {
            minMotionPixels = minPixels;
        }
    }
}
