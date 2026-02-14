using UnityEngine;
using ARBadmintonNet.AR;
using ARBadmintonNet.Detection;
using ARBadmintonNet.Collision;
using ARBadmintonNet.Feedback;
using ARBadmintonNet.Models;

namespace ARBadmintonNet
{
    /// <summary>
    /// Main controller that orchestrates all components
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARSessionManager arSessionManager;
        [SerializeField] private ARPlaneDetectionManager planeManager;
        [SerializeField] private NetPlacementController netPlacement;
        [SerializeField] private CourtPlacementController courtPlacement;
        
        [Header("Detection Components")]
        [SerializeField] private MotionBasedTracker motionTracker;
        [SerializeField] private OpenCVDetector openCVDetector;
        [SerializeField] private YOLODetector yoloDetector; // NEW ML Detector
        
        [Header("Collision & Feedback")]
        [SerializeField] private NetCollisionDetector collisionDetector;
        [SerializeField] private FeedbackManager feedbackManager;
        
        [Header("Settings")]
        [SerializeField] private bool autoStartTracking = true;
        
        private bool isTracking = false;
        
        private void Awake()
        {
            // Auto-find components if not assigned
            if (arSessionManager == null)
                arSessionManager = FindObjectOfType<ARSessionManager>();
                
            if (planeManager == null)
                planeManager = FindObjectOfType<ARPlaneDetectionManager>();
                
            if (netPlacement == null)
                netPlacement = FindObjectOfType<NetPlacementController>();

            if (courtPlacement == null)
                courtPlacement = FindObjectOfType<CourtPlacementController>();
                
            if (motionTracker == null)
            if (motionTracker == null)
                motionTracker = FindObjectOfType<MotionBasedTracker>();

            if (openCVDetector == null)
                openCVDetector = FindObjectOfType<OpenCVDetector>();
                
            if (collisionDetector == null)
                collisionDetector = FindObjectOfType<NetCollisionDetector>();
                
            if (feedbackManager == null)
                feedbackManager = FindObjectOfType<FeedbackManager>();
        }
        
        private void Start()
        {
            if (openCVDetector == null)
            {
                Debug.LogWarning("[GameManager] OpenCVDetector reference is null. Attempting to find or create...");
                openCVDetector = FindObjectOfType<OpenCVDetector>();
                if (openCVDetector == null)
                {
                    var go = new GameObject("OpenCVDetector_GM");
                    openCVDetector = go.AddComponent<OpenCVDetector>();
                    Debug.Log("[GameManager] Created new OpenCVDetector_GM");
                }
            }
            else
            {
                Debug.Log($"[GameManager] OpenCVDetector found and assigned. Enabled: {openCVDetector.enabled}");
            }

            if (yoloDetector == null)
            {
                yoloDetector = FindObjectOfType<YOLODetector>();
                if (yoloDetector == null)
                {
                    var go = new GameObject("YOLODetector_GM");
                    yoloDetector = go.AddComponent<YOLODetector>();
                    Debug.Log("[GameManager] Created new YOLODetector_GM");
                }
            }
            
            // Ensure it's enabled for testing
            if (openCVDetector != null && !openCVDetector.enabled)
            {
                Debug.Log("[GameManager] Forcing OpenCVDetector ENABLED for testing.");
                openCVDetector.enabled = true;
            }

            SetupEventHandlers();
            
            if (autoStartTracking)
            {
                StartTracking();
            }
        }
        
        private void SetupEventHandlers()
        {
            // Net placement events
            if (netPlacement != null)
            {
                netPlacement.OnNetPlaced += OnNetPlaced;
                netPlacement.OnNetRemoved += OnNetRemoved;
            }

            // Court placement events
            if (courtPlacement != null)
            {
                courtPlacement.OnCourtPlaced += OnCourtPlaced;
                courtPlacement.OnCourtRemoved += OnCourtRemoved;
            }
            
            // Motion detection events (detects ANY moving object)
            if (motionTracker != null)
            {
                motionTracker.OnShuttleDetected += OnShuttleDetected;
            }

            if (openCVDetector != null)
            {
                openCVDetector.OnShuttleDetected += OnShuttleDetected;
            }

            if (yoloDetector != null)
            {
                yoloDetector.OnObjectsDetected += OnYOLOObjectsDetected;
            }
            
            // Collision events
            if (collisionDetector != null)
            {
                collisionDetector.OnCollisionDetected += OnCollisionDetected;
            }
        }
        
        private void OnNetPlaced(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"Net placed at {position}");
            
            // Set net reference in collision detector
            if (collisionDetector != null && netPlacement != null)
            {
                collisionDetector.SetNetObject(netPlacement.GetNetInstance());
            }
            
            // Wire up physics-based collision detection
            var netInstance = netPlacement.GetNetInstance();
            if (netInstance != null)
            {
                var physicsCollision = netInstance.GetComponent<ARBadmintonNet.Collision.PhysicsNetCollision>();
                if (physicsCollision != null)
                {
                    physicsCollision.OnCollisionDetected += OnCollisionDetected;
                    Debug.Log("[GameManager] PhysicsNetCollision events wired to FeedbackManager");
                }
                else
                {
                    Debug.LogWarning("[GameManager] PhysicsNetCollision component not found on net prefab");
                }
            }
            
            // DON'T auto-lock — let user adjust position first via NetPlacementUI
            // Tracking will start when user locks the net
        }
        
        private void OnNetRemoved()
        {
            Debug.Log("Net removed");
            StopTracking();
            
            // Unwire physics collision events
            if (netPlacement != null)
            {
                var netInstance = netPlacement.GetNetInstance();
                if (netInstance != null)
                {
                    var physicsCollision = netInstance.GetComponent<ARBadmintonNet.Collision.PhysicsNetCollision>();
                    if (physicsCollision != null)
                    {
                        physicsCollision.OnCollisionDetected -= OnCollisionDetected;
                    }
                }
            }
            
            if (collisionDetector != null)
            {
                collisionDetector.Reset();
            }
        }
        
        private void OnCourtPlaced(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"Court placed at {position}");
            
            // Set net reference in collision detector (Court has its own net)
            if (collisionDetector != null && courtPlacement != null)
            {
                collisionDetector.SetNetObject(courtPlacement.GetNetInstance());
            }
            
            // Wire up physics-based collision detection
            var netInstance = courtPlacement.GetNetInstance();
            if (netInstance != null)
            {
                var physicsCollision = netInstance.GetComponent<ARBadmintonNet.Collision.PhysicsNetCollision>();
                if (physicsCollision != null)
                {
                    physicsCollision.OnCollisionDetected += OnCollisionDetected;
                    Debug.Log("[GameManager] PhysicsNetCollision (Court) wired to FeedbackManager");
                }
            }
        }

        private void OnCourtRemoved()
        {
            Debug.Log("Court removed");
            StopTracking();
            
            if (courtPlacement != null)
            {
                var netInstance = courtPlacement.GetNetInstance();
                if (netInstance != null)
                {
                    var physicsCollision = netInstance.GetComponent<ARBadmintonNet.Collision.PhysicsNetCollision>();
                    if (physicsCollision != null)
                    {
                        physicsCollision.OnCollisionDetected -= OnCollisionDetected;
                    }
                }
            }
            
            if (collisionDetector != null)
            {
                collisionDetector.Reset();
            }
        }
        
        private void OnYOLOObjectsDetected(System.Collections.Generic.List<ShuttleData> objects)
        {
            foreach (var data in objects)
            {
                // Relay valid detections to the main handler
                OnShuttleDetected(data);
            }
        }

        private void OnShuttleDetected(ShuttleData shuttleData)
        {
            Debug.Log($"[GameManager] Shuttle Data Received. Conf: {shuttleData.Confidence:F2}");

            // Only process if net is placed
            // Check if ANY game mode is active
            bool isNetMode = (netPlacement != null && netPlacement.IsNetPlaced);
            bool isCourtMode = (courtPlacement != null && courtPlacement.IsCourtPlaced);

            if (!isNetMode && !isCourtMode)
            {
               // Debug.LogWarning("[GameManager] Ignored shuttle - No Net/Court placed.");
                return;
            }
            
            // Update collision detector with new shuttle position
            if (collisionDetector != null && shuttleData.Confidence > 0.3f)
            {
                // Standard 3D tracking update
                collisionDetector.UpdateShuttlePosition(shuttleData.Position);

                // For OpenCV (2D), also check visual overlap with the Net Collider
                // because we don't have accurate depth to cross the plane in 3D
                if (shuttleData.Method == DetectionMethod.OpenCV)
                {
                    Ray ray = Camera.main.ScreenPointToRay(shuttleData.ScreenPosition);
                    if (collisionDetector.CheckVisualOverlap(ray, out RaycastHit hit))
                    {
                        // Manually trigger collision logic
                        Debug.Log($"[GameManager] Visual Hit on Net! Point: {hit.point}");
                        
                        // Construct a synthetic collision event
                        CollisionEvent evt = new CollisionEvent(
                            hit.point,
                            NetSide.SideA, // Assume impact from front for now
                            5.0f,          // Fake speed
                            ray.direction
                        );
                        
                        OnCollisionDetected(evt);
                    }
                }
            }
        }
        
        private void OnCollisionDetected(CollisionEvent collisionEvent)
        {
            Debug.Log($"Collision detected! Side: {collisionEvent.Side}");
            
            // Trigger feedback
            if (feedbackManager != null)
            {
                feedbackManager.OnNetHit(collisionEvent);
            }
        }
        
        public void StartTracking()
        {
            if (isTracking)
                return;
                
            isTracking = true;
            
            if (motionTracker != null)
            {
                motionTracker.enabled = true;
            }

            if (openCVDetector != null)
            {
                openCVDetector.enabled = true;
            }

            if (yoloDetector != null)
            {
                yoloDetector.enabled = true;
            }
            
            Debug.Log("Shuttle tracking started");
        }
        
        public void StopTracking()
        {
            if (!isTracking)
                return;
                
            isTracking = false;
            
            if (motionTracker != null)
            {
                motionTracker.enabled = false;
            }

            if (openCVDetector != null)
            {
                openCVDetector.enabled = false;
            }

            if (yoloDetector != null)
            {
                yoloDetector.enabled = false;
            }
            
            Debug.Log("Shuttle tracking stopped");
        }
        
        public void ResetEverything()
        {
            StopTracking();
            
            if (netPlacement != null)
            {
                netPlacement.RemoveNet();
            }
            
            if (collisionDetector != null)
            {
                collisionDetector.Reset();
            }
            
            Debug.Log("Game reset");
        }

        public void SetDetectionMode(bool useML)
        {
            // Only effective if tracking is active
            if (!isTracking) return;

            if (useML)
            {
                if (openCVDetector != null) openCVDetector.enabled = false;
                if (yoloDetector != null) yoloDetector.enabled = true;
                Debug.Log("[GameManager] Switched to ML Detection");
            }
            else
            {
                if (yoloDetector != null) yoloDetector.enabled = false;
                if (openCVDetector != null) openCVDetector.enabled = true;
                Debug.Log("[GameManager] Switched to Motion Detection");
            }
        }
        
        public void RegisterDetector(OpenCVDetector detector)
        {
            if (this.openCVDetector == detector) return; // Already registered
            
            // Unsubscribe from old if exists
            if (this.openCVDetector != null)
            {
                this.openCVDetector.OnShuttleDetected -= OnShuttleDetected;
            }

            this.openCVDetector = detector;
            this.openCVDetector.OnShuttleDetected += OnShuttleDetected;
            Debug.Log($"[GameManager] OpenCVDetector verified and registered: {detector.name}");
            
            // Sync state
            if (isTracking) detector.enabled = true;
        }

        private void OnDestroy()
        {
            // Clean up event handlers
            if (netPlacement != null)
            {
                netPlacement.OnNetPlaced -= OnNetPlaced;
                netPlacement.OnNetRemoved -= OnNetRemoved;
            }

            if (courtPlacement != null)
            {
                courtPlacement.OnCourtPlaced -= OnCourtPlaced;
                courtPlacement.OnCourtRemoved -= OnCourtRemoved;
            }
            
            if (motionTracker != null)
            {
                motionTracker.OnShuttleDetected -= OnShuttleDetected;
            }

            if (openCVDetector != null)
            {
                openCVDetector.OnShuttleDetected -= OnShuttleDetected;
            }
            
            if (yoloDetector != null)
            {
                yoloDetector.OnObjectsDetected -= OnYOLOObjectsDetected;
            }

            if (collisionDetector != null)
            {
                collisionDetector.OnCollisionDetected -= OnCollisionDetected;
            }
        }
    }
}
