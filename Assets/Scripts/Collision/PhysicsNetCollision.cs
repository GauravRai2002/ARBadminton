using UnityEngine;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Collision
{
    /// <summary>
    /// Physics-based collision detection for the net using Unity's trigger system.
    /// Detects any object (not just shuttle) that passes through the net.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PhysicsNetCollision : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minimumVelocity = 0.1f; // m/s - ignore very slow movements
        [SerializeField] private float collisionCooldown = 0.5f; // seconds between collision events
        
        private float lastCollisionTime = -999f;
        private Transform netTransform;
        
        public delegate void CollisionDetectedHandler(CollisionEvent collisionEvent);
        public event CollisionDetectedHandler OnCollisionDetected;
        
        private void Awake()
        {
            netTransform = transform;
            
            // Ensure collider is a trigger
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning("[PhysicsNetCollision] Collider is not a trigger! Setting isTrigger = true");
                col.isTrigger = true;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check cooldown
            if (Time.time - lastCollisionTime < collisionCooldown)
                return;
            
            // Get velocity of the object
            Rigidbody rb = other.GetComponent<Rigidbody>();
            Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
            float speed = velocity.magnitude;
            
            // Ignore very slow movements (helps filter out stationary objects)
            if (speed < minimumVelocity)
            {
                Debug.Log($"[PhysicsNetCollision] Ignoring slow collision: {speed:F2} m/s");
                return;
            }
            
            // Get collision point (approximate as the closest point on the collider)
            Vector3 collisionPoint = other.ClosestPoint(netTransform.position);
            
            // Determine which side the object came from
            NetSide side = DetermineSide(other.transform.position);
            
            // Create collision event
            var collisionEvent = new CollisionEvent(
                collisionPoint,
                side,
                speed,
                velocity.normalized
            );
            
            Debug.Log($"[PhysicsNetCollision] COLLISION! Object: {other.gameObject.name}, Side: {side}, Speed: {speed:F2} m/s");
            
            lastCollisionTime = Time.time;
            OnCollisionDetected?.Invoke(collisionEvent);
        }
        
        private NetSide DetermineSide(Vector3 objectPosition)
        {
            // Calculate which side of the net the object is on
            Vector3 toObject = objectPosition - netTransform.position;
            float dot = Vector3.Dot(toObject, netTransform.forward);
            
            // Positive dot = same direction as forward = Side A (front)
            // Negative dot = opposite direction = Side B (back)
            return dot > 0 ? NetSide.SideA : NetSide.SideB;
        }
        
        /// <summary>
        /// Alternative method for objects without Rigidbody - tracks velocity manually
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            // This could be used to track objects without Rigidbody
            // by storing previous positions and calculating velocity
            // For now, we rely on OnTriggerEnter
        }
    }
}
