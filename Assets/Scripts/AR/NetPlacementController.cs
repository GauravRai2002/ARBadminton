using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Controls the placement and positioning of the virtual badminton net
    /// </summary>
    public class NetPlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private GameObject netPrefab;
        [SerializeField] private GameObject placementIndicator;
        
        [Header("Configuration")]
        [SerializeField] private NetConfiguration netConfig;
        
        [Header("Settings")]
        [SerializeField] private float placementHeight = 1.55f; // Net height in meters
        [SerializeField] private LayerMask placementLayer;
        
        private GameObject netInstance;
        private bool isPlacementMode = true;
        
        // Store the placed world position
        private Vector3 placedWorldPosition;
        private Quaternion placedWorldRotation;
        private bool hasPlacedNet = false;
        
        // Debug timer
        private float debugTimer = 0f;
        
        public NetConfiguration NetConfig => netConfig;
        public bool IsNetPlaced => netConfig.IsPlaced;
        public bool IsNetLocked => netConfig.IsLocked;
        
        public delegate void NetPlacedHandler(Vector3 position, Quaternion rotation);
        public event NetPlacedHandler OnNetPlaced;
        
        public delegate void NetRemovedHandler();
        public event NetRemovedHandler OnNetRemoved;
        
        private void Awake()
        {
            if (arCamera == null)
                arCamera = Camera.main;
                
            if (raycastManager == null)
                raycastManager = FindObjectOfType<ARRaycastManager>();
                
            if (netConfig == null)
                netConfig = new NetConfiguration();
                
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
        }
        
        private void Update()
        {
            if (isPlacementMode && !netConfig.IsLocked)
            {
                UpdatePlacementIndicator();
                
                // Place net on tap
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        TryPlaceNet(touch.position);
                    }
                }
                
                // For testing in editor with mouse
                #if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0))
                {
                    TryPlaceNet(Input.mousePosition);
                }
                #endif
            }
        }
        
        /// <summary>
        /// LateUpdate runs AFTER all Update calls and AR camera tracking updates.
        /// This is the correct place to enforce world-space positioning for AR objects.
        /// </summary>
        private void LateUpdate()
        {
            if (netInstance != null && hasPlacedNet)
            {
                // Force the net to the stored world position EVERY frame in LateUpdate
                // This runs after AR tracking has updated the camera position
                netInstance.transform.position = placedWorldPosition;
                netInstance.transform.rotation = placedWorldRotation;
                
                // Ensure no parent (could have been reparented by AR system)
                if (netInstance.transform.parent != null)
                {
                    netInstance.transform.SetParent(null, true);
                }
            }
        }
        
        private void UpdatePlacementIndicator()
        {
            if (placementIndicator == null) return;
            
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            
            if (TryGetPlacementPose(screenCenter, out Vector3 position, out Quaternion rotation))
            {
                placementIndicator.SetActive(true);
                placementIndicator.transform.position = position;
                placementIndicator.transform.rotation = rotation;
            }
            else
            {
                placementIndicator.SetActive(false);
            }
        }
        
        public void TryPlaceNet(Vector2 screenPosition)
        {
            if (TryGetPlacementPose(screenPosition, out Vector3 position, out Quaternion rotation))
            {
                PlaceNet(position, rotation);
            }
        }
        
        private bool TryGetPlacementPose(Vector2 screenPosition, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            
            // Raycast against AR planes
            var hits = new System.Collections.Generic.List<ARRaycastHit>();
            if (raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;
                position = hitPose.position;
                rotation = hitPose.rotation;
                
                // Adjust position to net height
                position.y += placementHeight / 2f; // Center of net at correct height
                
                return true;
            }
            
            return false;
        }
        
        public void PlaceNet(Vector3 position, Quaternion rotation)
        {
            // Remove existing net if any
            if (netInstance != null)
            {
                RemoveNet();
            }
            
            // Store the target world position BEFORE instantiation
            placedWorldPosition = position;
            placedWorldRotation = rotation;
            
            // Instantiate without any parent
            netInstance = Instantiate(netPrefab);
            
            // CRITICAL: Ensure no parent
            netInstance.transform.SetParent(null, true);
            
            // Set position IMMEDIATELY
            netInstance.transform.position = position;
            netInstance.transform.rotation = rotation;
            
            hasPlacedNet = true;
            
            // Update configuration
            netConfig.SetPosition(position, rotation);
            
            // Disable placement mode
            isPlacementMode = false;
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
            
            Debug.Log($"Net placed at {position}, dist={Vector3.Distance(arCamera != null ? arCamera.transform.position : Vector3.zero, position):F2}m");
            
            OnNetPlaced?.Invoke(position, rotation);
        }
        
        public void RemoveNet()
        {
            if (netInstance != null)
            {
                Destroy(netInstance);
                netInstance = null;
            }
            
            hasPlacedNet = false;
            netConfig.Reset();
            isPlacementMode = true;
            
            Debug.Log("Net removed");
            OnNetRemoved?.Invoke();
        }
        
        public void LockNet()
        {
            netConfig.Lock();
            Debug.Log("Net locked in place");
        }
        
        public void UnlockNet()
        {
            netConfig.Unlock();
            // Stay in non-placement mode — net stays visible, just unlocked for adjustment
            Debug.Log("Net unlocked for adjustment");
        }
        
        /// <summary>
        /// Move the net by a delta in world space
        /// </summary>
        public void MoveNet(Vector3 delta)
        {
            if (netInstance == null || !hasPlacedNet) return;
            
            placedWorldPosition += delta;
            netInstance.transform.position = placedWorldPosition;
            
            // Update config
            netConfig.SetPosition(placedWorldPosition, placedWorldRotation);
            Debug.Log($"Net moved to {placedWorldPosition}");
        }
        
        /// <summary>
        /// Rotate the net around Y axis by given degrees
        /// </summary>
        public void RotateNet(float degrees)
        {
            RotateNetAxis(Vector3.up, degrees);
        }
        
        /// <summary>
        /// Rotate the net around specified axis by given degrees
        /// </summary>
        public void RotateNetAxis(Vector3 axis, float degrees)
        {
            if (netInstance == null || !hasPlacedNet) return;
            
            placedWorldRotation *= Quaternion.AngleAxis(degrees, axis);
            netInstance.transform.rotation = placedWorldRotation;
            
            // Update config
            netConfig.SetPosition(placedWorldPosition, placedWorldRotation);
            Debug.Log($"Net rotated by {degrees} degrees around {axis}");
        }
        
        public void EnablePlacementMode()
        {
            isPlacementMode = true;
            if (placementIndicator != null)
                placementIndicator.SetActive(true);
        }
        
        public void DisablePlacementMode()
        {
            isPlacementMode = false;
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
        }
        
        public GameObject GetNetInstance()
        {
            return netInstance;
        }
    }
}

