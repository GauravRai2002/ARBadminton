using UnityEngine;

namespace ARBadmintonNet.Models
{
    /// <summary>
    /// Data structure for tracking shuttle position and movement
    /// </summary>
    public class ShuttleData
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public float Confidence { get; set; }
        public long Timestamp { get; set; }
        public DetectionMethod Method { get; set; }
        
        // 2D screen position for reference
        public Vector2 ScreenPosition { get; set; }
        
        // Bounding box from detector
        public Rect BoundingBox { get; set; }
        
        public ShuttleData()
        {
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        
        public ShuttleData(Vector3 position, float confidence, DetectionMethod method)
        {
            Position = position;
            Confidence = confidence;
            Method = method;
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    
    public enum DetectionMethod
    {
        ColorTracking,
        OpenCV,
        MLDetection,
        ML, // Added for YOLODetector compatibility
        Hybrid,
        Manual
    }
}
