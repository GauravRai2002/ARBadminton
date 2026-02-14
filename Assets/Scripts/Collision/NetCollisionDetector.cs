using UnityEngine;
using ARBadmintonNet.Models;
using ARBadmintonNet.Utilities;
using System.Collections.Generic;

namespace ARBadmintonNet.Collision
{
    /// <summary>
    /// Detects when shuttle crosses or hits the virtual net
    /// Uses line-plane intersection and trajectory prediction
    /// </summary>
    public class NetCollisionDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject netObject;
        [SerializeField] private MeshCollider netCollider;
        
        [Header("Detection Settings")]
        [SerializeField] private float collisionThreshold = 0.1f; // meters
        [SerializeField] private int trajectoryLookAhead = 3; // frames
        
        [Header("Debug")]
        [SerializeField] private bool debugVisualization = false;
        [SerializeField] private Color debugRayColor = Color.red;
        
        private TrajectoryTracker trajectoryTracker;
        private Vector3 lastShuttlePosition;
        private bool hasLastPosition = false;
        private Plane netPlane;
        private bool netInitialized = false;
        
        public delegate void CollisionDetectedHandler(CollisionEvent collisionEvent);
        public event CollisionDetectedHandler OnCollisionDetected;
        
        private void Awake()
        {
            trajectoryTracker = new TrajectoryTracker(10);
            InitializeNetPlane();
        }
        
        private void Update()
        {
            // Net plane is initialized once when SetNetObject is called
            // No need to re-initialize every frame
        }
        
        private void InitializeNetPlane()
        {
            if (netObject == null)
            {
                Debug.LogWarning("Net object not assigned to collision detector");
                return;
            }
            
            // Create plane from net's forward direction and position
            Vector3 normal = netObject.transform.forward;
            Vector3 point = netObject.transform.position;
            netPlane = new Plane(normal, point);
            netInitialized = true;
            
            Debug.Log($"Net plane initialized: normal={normal}, point={point}");
        }
        
        public void UpdateShuttlePosition(Vector3 newPosition)
        {
            if (!netInitialized)
            {
                Debug.LogWarning("Net not initialized, cannot detect collisions");
                return;
            }
            
            // Add to trajectory tracker
            trajectoryTracker.AddMeasurement(newPosition, Time.time);
            
            // Check for collision if we have previous position
            if (hasLastPosition)
            {
                CheckForCollision(lastShuttlePosition, newPosition);
            }
            
            lastShuttlePosition = newPosition;
            hasLastPosition = true;
        }
        
        private void CheckForCollision(Vector3 previousPos, Vector3 currentPos)
        {
            // Create line segment from previous to current position
            Vector3 direction = currentPos - previousPos;
            float distance = direction.magnitude;
            
            if (distance < 0.001f)
                return; // No movement
            
            Ray ray = new Ray(previousPos, direction.normalized);
            
            // Check intersection with net plane
            if (netPlane.Raycast(ray, out float enter))
            {
                if (enter <= distance + collisionThreshold)
                {
                    // Collision detected!
                    Vector3 intersectionPoint = ray.GetPoint(enter);
                    
                    // Verify intersection is within net bounds
                    if (IsPointWithinNetBounds(intersectionPoint))
                    {
                        // Determine which side the shuttle came from
                        NetSide side = DetermineSide(previousPos);
                        
                        // Calculate impact velocity
                        Vector3 velocity = trajectoryTracker.CurrentVelocity;
                        float impactSpeed = velocity.magnitude;
                        
                        // Create collision event
                        var collisionEvent = new CollisionEvent(
                            intersectionPoint,
                            side,
                            impactSpeed,
                            direction.normalized
                        );
                        
                        Debug.Log($"COLLISION DETECTED! Side: {side}, Point: {intersectionPoint}, Speed: {impactSpeed:F2} m/s");
                        
                        OnCollisionDetected?.Invoke(collisionEvent);
                        
                        // Visualize collision point
                        if (debugVisualization)
                        {
                            DrawCollisionDebug(intersectionPoint, side);
                        }
                    }
                }
            }
            
            // Draw debug ray
            if (debugVisualization)
            {
                Debug.DrawLine(previousPos, currentPos, debugRayColor, 0.5f);
            }
        }
        
        private bool IsPointWithinNetBounds(Vector3 point)
        {
            if (netObject == null)
                return false;
            
            // Transform point to net's local space
            Vector3 localPoint = netObject.transform.InverseTransformPoint(point);
            
            // Check if point is within net dimensions
            // Assuming net is centered at origin in local space
            float halfWidth = 5.18f / 2f;  // Standard badminton net width
            float halfHeight = 1.55f / 2f; // Standard net height
            
            bool withinWidth = Mathf.Abs(localPoint.x) <= halfWidth;
            bool withinHeight = Mathf.Abs(localPoint.y) <= halfHeight;
            bool nearSurface = Mathf.Abs(localPoint.z) <= collisionThreshold;
            
            return withinWidth && withinHeight && nearSurface;
        }
        
        private NetSide DetermineSide(Vector3 previousPosition)
        {
            if (netObject == null)
                return NetSide.SideA;
            
            // Calculate which side of the net plane the previous position was on
            Vector3 toPoint = previousPosition - netObject.transform.position;
            float dot = Vector3.Dot(toPoint, netObject.transform.forward);
            
            // Positive dot = same direction as forward = Side A (front)
            // Negative dot = opposite direction = Side B (back)
            return dot > 0 ? NetSide.SideA : NetSide.SideB;
        }
        
        private void DrawCollisionDebug(Vector3 point, NetSide side)
        {
            Color color = side == NetSide.SideA ? Color.green : Color.blue;
            Debug.DrawRay(point, Vector3.up * 0.5f, color, 2f);
            Debug.DrawRay(point, Vector3.down * 0.5f, color, 2f);
            Debug.DrawRay(point, Vector3.left * 0.5f, color, 2f);
            Debug.DrawRay(point, Vector3.right * 0.5f, color, 2f);
        }
        
        public void SetNetObject(GameObject net)
        {
            netObject = net;
            InitializeNetPlane();
            
            // Try to get mesh collider
            if (net != null)
            {
                netCollider = net.GetComponent<MeshCollider>();
            }
        }
        
        public void Reset()
        {
            trajectoryTracker.Reset();
            hasLastPosition = false;
            Debug.Log("Collision detector reset");
        }
        
        public Vector3 PredictCollisionPoint()
        {
            if (!trajectoryTracker.IsInitialized)
                return Vector3.zero;
            
            // Predict shuttle path
            Vector3[] futurePath = trajectoryTracker.GetTrajectoryPath(0.5f, trajectoryLookAhead);
            
            // Check each segment for intersection
            for (int i = 0; i < futurePath.Length - 1; i++)
            {
                Vector3 p1 = futurePath[i];
                Vector3 p2 = futurePath[i + 1];
                Vector3 dir = p2 - p1;
                
                Ray ray = new Ray(p1, dir.normalized);
                
                if (netPlane.Raycast(ray, out float enter))
                {
                    if (enter <= dir.magnitude)
                    {
                        return ray.GetPoint(enter);
                    }
                }
            }
            
            return Vector3.zero;
        }
        
        public bool CheckVisualOverlap(Ray ray, out RaycastHit hitInfo)
        {
            if (netCollider != null)
            {
                // Raycast against the mesh collider
                if (netCollider.Raycast(ray, out hitInfo, 20.0f))
                {
                    if (debugVisualization)
                    {
                        Debug.DrawLine(ray.origin, hitInfo.point, Color.magenta, 1.0f);
                        // Debug.Log($"[NetCollision] Visual overlap detected at {hitInfo.point}");
                    }
                    return true;
                }
            }
            hitInfo = new RaycastHit();
            return false;
        }

        private void OnDrawGizmos()
        {
            if (!debugVisualization || !netInitialized)
                return;
                
            // Draw net plane
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 netPos = netObject != null ? netObject.transform.position : Vector3.zero;
            Vector3 netNormal = netObject != null ? netObject.transform.forward : Vector3.forward;
            
            // Draw plane normal
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(netPos, netNormal * 0.5f);
        }
    }
}
