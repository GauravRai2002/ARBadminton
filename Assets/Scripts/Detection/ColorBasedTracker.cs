using System.Collections.Generic;
using UnityEngine;
using ARBadmintonNet.Models;
using ARBadmintonNet.Utilities;

namespace ARBadmintonNet.Detection
{
    /// <summary>
    /// Color-based shuttle tracking using HSV thresholding
    /// Fast but less accurate than ML detection
    /// </summary>
    public class ColorBasedTracker : MonoBehaviour
    {
        [Header("Color Detection Settings")]
        [SerializeField] private Color targetColor = Color.yellow;
        [SerializeField] private float hueThreshold = 15f; // ±15 degrees
        [SerializeField] private float saturationMin = 0.4f; // 40%
        [SerializeField] private float valueMin = 0.4f; // 40%
        
        [Header("Size Constraints")]
        [SerializeField] private float minShuttleSize = 30f; // pixels
        [SerializeField] private float maxShuttleSize = 150f; // pixels
        
        [Header("Performance")]
        [SerializeField] private int detectionFrameSkip = 1; // Process every N frames
        
        private Camera arCamera;
        private Texture2D cameraTexture;
        private int frameCounter = 0;
        private Vector2 lastDetectedPosition;
        private float lastDetectionTime;
        
        public delegate void ShuttleDetectedHandler(ShuttleData shuttleData);
        public event ShuttleDetectedHandler OnShuttleDetected;
        
        private void Awake()
        {
            arCamera = Camera.main;
        }
        
        private void Update()
        {
            frameCounter++;
            if (frameCounter % detectionFrameSkip != 0)
                return;
                
            DetectShuttle();
        }
        
        private void DetectShuttle()
        {
            // Get camera frame
            if (!TryGetCameraFrame(out Texture2D frame))
                return;
            
            // Find shuttle by color
            if (TryFindShuttleByColor(frame, out Vector2 screenPos, out Rect boundingBox))
            {
                // Convert to 3D position
                if (TryConvert2DTo3D(screenPos, out Vector3 worldPos))
                {
                    var shuttleData = new ShuttleData
                    {
                        Position = worldPos,
                        ScreenPosition = screenPos,
                        BoundingBox = boundingBox,
                        Confidence = CalculateConfidence(boundingBox),
                        Method = DetectionMethod.ColorTracking
                    };
                    
                    // Calculate velocity if we have previous position
                    if (lastDetectionTime > 0)
                    {
                        float deltaTime = Time.time - lastDetectionTime;
                        Vector2 displacement = screenPos - lastDetectedPosition;
                        shuttleData.Velocity = new Vector3(
                            displacement.x / deltaTime,
                            displacement.y / deltaTime,
                            0
                        );
                    }
                    
                    lastDetectedPosition = screenPos;
                    lastDetectionTime = Time.time;
                    
                    OnShuttleDetected?.Invoke(shuttleData);
                }
            }
        }
        
        private bool TryGetCameraFrame(out Texture2D frame)
        {
            frame = null;
            
            // In a real implementation, this would come from the AR camera feed
            // For Unity, you'd use ARCameraManager.TryAcquireLatestCpuImage()
            // This is a simplified version
            
            try
            {
                // Create or reuse texture
                if (cameraTexture == null || 
                    cameraTexture.width != Screen.width || 
                    cameraTexture.height != Screen.height)
                {
                    cameraTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                }
                
                // Capture screen (in production, use AR camera frame)
                RenderTexture currentRT = RenderTexture.active;
                RenderTexture tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
                
                arCamera.targetTexture = tempRT;
                arCamera.Render();
                
                RenderTexture.active = tempRT;
                cameraTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                cameraTexture.Apply();
                
                arCamera.targetTexture = null;
                RenderTexture.active = currentRT;
                RenderTexture.ReleaseTemporary(tempRT);
                
                frame = cameraTexture;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get camera frame: {e.Message}");
                return false;
            }
        }
        
        private bool TryFindShuttleByColor(Texture2D frame, out Vector2 position, out Rect boundingBox)
        {
            position = Vector2.zero;
            boundingBox = new Rect();
            
            // Get target HSV range
            Color.RGBToHSV(targetColor, out float targetH, out float targetS, out float targetV);
            
            int width = frame.width;
            int height = frame.height;
            
            // Lists to store matching pixels
            List<Vector2Int> matchingPixels = new List<Vector2Int>();
            
            // Downsample for performance (check every 4th pixel)
            int step = 4;
            
            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    Color pixel = frame.GetPixel(x, y);
                    
                    if (IsColorMatch(pixel, targetH, targetS, targetV))
                    {
                        matchingPixels.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            // Need minimum pixels to consider it a detection
            if (matchingPixels.Count < 10)
                return false;
            
            // Calculate centroid and bounding box
            Vector2 centroid = CalculateCentroid(matchingPixels);
            Rect bounds = CalculateBoundingBox(matchingPixels);
            
            // Validate size
            float area = bounds.width * bounds.height;
            float diameter = Mathf.Sqrt(area / Mathf.PI) * 2;
            
            if (diameter < minShuttleSize || diameter > maxShuttleSize)
                return false;
            
            position = centroid;
            boundingBox = bounds;
            return true;
        }
        
        private bool IsColorMatch(Color pixel, float targetH, float targetS, float targetV)
        {
            Color.RGBToHSV(pixel, out float h, out float s, out float v);
            
            // Check hue (with wraparound at 0/1)
            float hueDiff = Mathf.Abs(h - targetH);
            if (hueDiff > 0.5f)
                hueDiff = 1f - hueDiff;
            
            bool hueMatch = hueDiff <= (hueThreshold / 360f);
            bool satMatch = s >= saturationMin;
            bool valMatch = v >= valueMin;
            
            return hueMatch && satMatch && valMatch;
        }
        
        private Vector2 CalculateCentroid(List<Vector2Int> points)
        {
            if (points.Count == 0)
                return Vector2.zero;
            
            Vector2 sum = Vector2.zero;
            foreach (var point in points)
            {
                sum += new Vector2(point.x, point.y);
            }
            
            return sum / points.Count;
        }
        
        private Rect CalculateBoundingBox(List<Vector2Int> points)
        {
            if (points.Count == 0)
                return new Rect();
            
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            
            foreach (var point in points)
            {
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        
        private float CalculateConfidence(Rect boundingBox)
        {
            // Simple confidence based on size consistency
            // Ideal shuttle should be roughly circular
            float aspectRatio = boundingBox.width / Mathf.Max(boundingBox.height, 0.1f);
            float circularity = 1f - Mathf.Abs(1f - aspectRatio);
            
            return Mathf.Clamp01(circularity);
        }
        
        private bool TryConvert2DTo3D(Vector2 screenPos, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            
            // Raycast from camera through screen point
            Ray ray = arCamera.ScreenPointToRay(screenPos);
            
            // In a real AR implementation, you'd raycast against:
            // 1. Detected AR planes
            // 2. Estimated depth from shuttle size
            // 3. ML-based depth estimation
            
            // Simplified: assume shuttle is 3 meters away
            float estimatedDistance = 3f;
            worldPos = ray.GetPoint(estimatedDistance);
            
            return true;
        }
        
        public void SetTargetColor(Color color)
        {
            targetColor = color;
        }
        
        public void SetDetectionParameters(float hueThresh, float satMin, float valMin)
        {
            hueThreshold = hueThresh;
            saturationMin = satMin;
            valueMin = valMin;
        }
    }
}
