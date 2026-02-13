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
        
        [Header("Detection Components")]
        [SerializeField] private MotionBasedTracker motionTracker;
        
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
                
            if (motionTracker == null)
                motionTracker = FindObjectOfType<MotionBasedTracker>();
                
            if (collisionDetector == null)
                collisionDetector = FindObjectOfType<NetCollisionDetector>();
                
            if (feedbackManager == null)
                feedbackManager = FindObjectOfType<FeedbackManager>();
        }
        
        private void Start()
        {
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
            
            // Motion detection events (detects ANY moving object)
            if (motionTracker != null)
            {
                motionTracker.OnShuttleDetected += OnShuttleDetected;
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
        
        private void OnShuttleDetected(ShuttleData shuttleData)
        {
            // Only process if net is placed
            if (netPlacement == null || !netPlacement.IsNetPlaced)
                return;
            
            // Update collision detector with new shuttle position
            if (collisionDetector != null && shuttleData.Confidence > 0.3f)
            {
                collisionDetector.UpdateShuttlePosition(shuttleData.Position);
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
        
        private void OnDestroy()
        {
            // Clean up event handlers
            if (netPlacement != null)
            {
                netPlacement.OnNetPlaced -= OnNetPlaced;
                netPlacement.OnNetRemoved -= OnNetRemoved;
            }
            
            if (motionTracker != null)
            {
                motionTracker.OnShuttleDetected -= OnShuttleDetected;
            }
            
            if (collisionDetector != null)
            {
                collisionDetector.OnCollisionDetected -= OnCollisionDetected;
            }
        }
    }
}
