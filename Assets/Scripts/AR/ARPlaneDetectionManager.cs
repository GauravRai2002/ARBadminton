using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Manages AR plane detection and provides suitable planes for net placement
    /// </summary>
    public class ARPlaneDetectionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARPlaneManager planeManager;
        
        [Header("Settings")]
        [SerializeField] private PlaneDetectionMode detectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
        [SerializeField] private float minPlaneSize = 0.5f; // Minimum plane size in meters
        
        private Dictionary<TrackableId, ARPlane> detectedPlanes = new Dictionary<TrackableId, ARPlane>();
        
        public List<ARPlane> HorizontalPlanes { get; private set; } = new List<ARPlane>();
        public List<ARPlane> VerticalPlanes { get; private set; } = new List<ARPlane>();
        
        private void Awake()
        {
            if (planeManager == null)
                planeManager = FindObjectOfType<ARPlaneManager>();
            
            if (planeManager == null)
                Debug.LogError("ARPlaneDetectionManager: No ARPlaneManager found in scene!");
        }
        
        private void OnEnable()
        {
            planeManager.planesChanged += OnPlanesChanged;
            SetDetectionMode(detectionMode);
        }
        
        private void OnDisable()
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
        
        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Handle added planes
            foreach (var plane in args.added)
            {
                if (IsPlaneSuitable(plane))
                {
                    detectedPlanes[plane.trackableId] = plane;
                    CategorizePlane(plane);
                }
            }
            
            // Handle updated planes
            foreach (var plane in args.updated)
            {
                if (detectedPlanes.ContainsKey(plane.trackableId))
                {
                    UpdatePlaneCategories(plane);
                }
            }
            
            // Handle removed planes
            foreach (var plane in args.removed)
            {
                if (detectedPlanes.ContainsKey(plane.trackableId))
                {
                    detectedPlanes.Remove(plane.trackableId);
                    RemovePlaneFromCategories(plane);
                }
            }
        }
        
        private bool IsPlaneSuitable(ARPlane plane)
        {
            // Check if plane is large enough
            Vector2 size = plane.size;
            float area = size.x * size.y;
            return area >= (minPlaneSize * minPlaneSize);
        }
        
        private void CategorizePlane(ARPlane plane)
        {
            if (plane.alignment == PlaneAlignment.HorizontalUp || 
                plane.alignment == PlaneAlignment.HorizontalDown)
            {
                if (!HorizontalPlanes.Contains(plane))
                    HorizontalPlanes.Add(plane);
            }
            else if (plane.alignment == PlaneAlignment.Vertical)
            {
                if (!VerticalPlanes.Contains(plane))
                    VerticalPlanes.Add(plane);
            }
        }
        
        private void UpdatePlaneCategories(ARPlane plane)
        {
            RemovePlaneFromCategories(plane);
            CategorizePlane(plane);
        }
        
        private void RemovePlaneFromCategories(ARPlane plane)
        {
            HorizontalPlanes.Remove(plane);
            VerticalPlanes.Remove(plane);
        }
        
        public void SetDetectionMode(PlaneDetectionMode mode)
        {
            planeManager.requestedDetectionMode = mode;
        }
        
        public void EnablePlaneVisualization(bool enable)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(enable);
            }
        }
        
        public ARPlane GetNearestPlane(Vector3 position, bool horizontalOnly = false)
        {
            ARPlane nearest = null;
            float minDistance = float.MaxValue;
            
            var planesToCheck = horizontalOnly ? HorizontalPlanes : new List<ARPlane>(detectedPlanes.Values);
            
            foreach (var plane in planesToCheck)
            {
                float distance = Vector3.Distance(position, plane.center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = plane;
                }
            }
            
            return nearest;
        }
        
        public bool HasSuitablePlanes()
        {
            return HorizontalPlanes.Count > 0 || VerticalPlanes.Count > 0;
        }
    }
}
