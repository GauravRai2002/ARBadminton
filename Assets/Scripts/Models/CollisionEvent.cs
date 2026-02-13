using UnityEngine;

namespace ARBadmintonNet.Models
{
    /// <summary>
    /// Represents a collision event when shuttle hits the net
    /// </summary>
    public class CollisionEvent
    {
        public Vector3 CollisionPoint { get; set; }
        public NetSide Side { get; set; }
        public long Timestamp { get; set; }
        public float ImpactVelocity { get; set; }
        public Vector3 IncomingDirection { get; set; }
        
        public CollisionEvent(Vector3 point, NetSide side, float velocity, Vector3 direction)
        {
            CollisionPoint = point;
            Side = side;
            ImpactVelocity = velocity;
            IncomingDirection = direction;
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    
    public enum NetSide
    {
        SideA,  // Front side
        SideB   // Back side
    }
}
