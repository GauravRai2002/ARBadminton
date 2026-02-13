using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

namespace ARBadmintonNet.Utilities
{
    /// <summary>
    /// Converts between 2D screen coordinates and 3D AR world space
    /// Handles depth estimation and camera perspective
    /// </summary>
    public class CoordinateConverter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARRaycastManager raycastManager;
        
        [Header("Depth Estimation Settings")]
        [SerializeField] private float defaultDepth = 2.0f; // meters, if no raycast hit
        [SerializeField] private float shuttleRealSize = 0.065f; // meters (standard shuttlecock diameter ~6.5cm)
        [SerializeField] private float referencePixelSize = 50f; // pixels when shuttle is at 2m
        
        private void Awake()
        {
            if (arCamera == null)
                arCamera = Camera.main;
                
            if (raycastManager == null)
                raycastManager = FindObjectOfType<ARRaycastManager>();
        }
        
        /// <summary>
        /// Convert 2D screen position to 3D world position using AR raycasting
        /// </summary>
        public bool ScreenToWorldPosition(Vector2 screenPosition, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            
            if (raycastManager == null || arCamera == null)
            {
                Debug.LogWarning("CoordinateConverter: Missing required references");
                return false;
            }
            
            // Try raycast against AR planes
            var hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                worldPosition = hits[0].pose.position;
                return true;
            }
            
            // Fallback: Use default depth
            worldPosition = ScreenToWorldAtDepth(screenPosition, defaultDepth);
            return false; // Indicate we used fallback
        }
        
        /// <summary>
        /// Convert screen position to world position at a specific depth
        /// </summary>
        public Vector3 ScreenToWorldAtDepth(Vector2 screenPosition, float depth)
        {
            if (arCamera == null)
            {
                Debug.LogWarning("CoordinateConverter: AR Camera is null");
                return Vector3.zero;
            }
            
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, depth);
            return arCamera.ScreenToWorldPoint(screenPoint);
        }
        
        /// <summary>
        /// Estimate depth based on shuttle apparent size in pixels
        /// Uses inverse square law: distance = sqrt(realSize / apparentSize) * referenceDistance
        /// </summary>
        public float EstimateDepthFromSize(float pixelDiameter)
        {
            if (pixelDiameter <= 0)
                return defaultDepth;
            
            // Simple inverse relationship (simplified version)
            // Real implementation would use camera FOV and more accurate calculations
            float estimatedDepth = (referencePixelSize / pixelDiameter) * 2.0f;
            
            // Clamp to reasonable range (0.5m to 10m)
            estimatedDepth = Mathf.Clamp(estimatedDepth, 0.5f, 10f);
            
            return estimatedDepth;
        }
        
        /// <summary>
        /// Convert screen bounds (bounding box) to estimated 3D position
        /// </summary>
        public bool BoundsToWorldPosition(Rect screenBounds, out Vector3 worldPosition, out float confidence)
        {
            worldPosition = Vector3.zero;
            confidence = 0f;
            
            if (arCamera == null)
                return false;
            
            // Use center of bounds
            Vector2 screenCenter = screenBounds.center;
            
            // Estimate depth from size
            float diameter = Mathf.Max(screenBounds.width, screenBounds.height);
            float estimatedDepth = EstimateDepthFromSize(diameter);
            
            // Try raycast first
            if (ScreenToWorldPosition(screenCenter, out Vector3 raycastPosition))
            {
                worldPosition = raycastPosition;
                confidence = 1.0f;
                return true;
            }
            
            // Fallback to depth estimation
            worldPosition = ScreenToWorldAtDepth(screenCenter, estimatedDepth);
            confidence = 0.5f; // Lower confidence for estimated depth
            return true;
        }
        
        /// <summary>
        /// Convert world position to screen position
        /// </summary>
        public Vector2 WorldToScreenPosition(Vector3 worldPosition)
        {
            if (arCamera == null)
                return Vector2.zero;
            
            Vector3 screenPoint = arCamera.WorldToScreenPoint(worldPosition);
            return new Vector2(screenPoint.x, screenPoint.y);
        }
        
        /// <summary>
        /// Check if a world position is visible in camera view
        /// </summary>
        public bool IsPositionVisible(Vector3 worldPosition)
        {
            if (arCamera == null)
                return false;
            
            Vector3 viewportPoint = arCamera.WorldToViewportPoint(worldPosition);
            
            // Check if in front of camera and within viewport bounds
            return viewportPoint.z > 0 && 
                   viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1;
        }
        
        /// <summary>
        /// Get ray from screen position through camera
        /// </summary>
        public Ray GetRayFromScreen(Vector2 screenPosition)
        {
            if (arCamera == null)
                return new Ray();
            
            return arCamera.ScreenPointToRay(screenPosition);
        }
        
        /// <summary>
        /// Try to find intersection point between screen ray and a plane
        /// </summary>
        public bool RaycastToPlane(Vector2 screenPosition, Plane plane, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            
            if (arCamera == null)
                return false;
            
            Ray ray = GetRayFromScreen(screenPosition);
            
            if (plane.Raycast(ray, out float enter))
            {
                hitPoint = ray.GetPoint(enter);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculate distance from camera to world position
        /// </summary>
        public float GetDistanceToCamera(Vector3 worldPosition)
        {
            if (arCamera == null)
                return 0f;
            
            return Vector3.Distance(arCamera.transform.position, worldPosition);
        }
        
        /// <summary>
        /// Validate that screen position is within screen bounds
        /// </summary>
        public bool IsValidScreenPosition(Vector2 screenPosition)
        {
            return screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                   screenPosition.y >= 0 && screenPosition.y <= Screen.height;
        }
    }
}
