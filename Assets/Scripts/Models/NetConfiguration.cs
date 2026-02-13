using UnityEngine;

namespace ARBadmintonNet.Models
{
    /// <summary>
    /// Configuration for the badminton net
    /// </summary>
    [System.Serializable]
    public class NetConfiguration
    {
        [Header("Net Dimensions (in meters)")]
        public float Width = 5.18f;           // Standard badminton net width
        public float Height = 1.55f;          // Standard net height
        public float Thickness = 0.1f;        // Net thickness for collision
        
        [Header("Position & Rotation")]
        public Vector3 Position;
        public Quaternion Rotation;
        
        [Header("State")]
        public bool IsPlaced = false;
        public bool IsLocked = false;
        
        [Header("Visual Settings")]
        [Range(0f, 1f)]
        public float Transparency = 0.6f;
        public Color NetColor = new Color(1f, 1f, 1f, 0.6f);
        
        [Header("Detection Settings")]
        public bool EnableCollisionDetection = true;
        public float CollisionSensitivity = 1.0f;
        
        public void SetPosition(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            IsPlaced = true;
        }
        
        public void Lock()
        {
            IsLocked = true;
        }
        
        public void Unlock()
        {
            IsLocked = false;
        }
        
        public void Reset()
        {
            IsPlaced = false;
            IsLocked = false;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }
    }
}
