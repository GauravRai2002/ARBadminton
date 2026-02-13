using UnityEngine;
using ARBadmintonNet.Utilities;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Detection
{
    /// <summary>
    /// Predicts shuttle trajectory using Kalman filtering
    /// Provides smooth position estimates and velocity calculations
    /// </summary>
    public class TrajectoryPredictor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int historySize = 10;
        [SerializeField] private float minConfidenceThreshold = 0.3f;
        [SerializeField] private float maxVelocityThreshold = 50f; // m/s, to filter unrealistic values
        
        [Header("Prediction")]
        [SerializeField] private bool enablePrediction = true;
        [SerializeField] private float predictionTimeAhead = 0.1f; // seconds
        [SerializeField] private int trajectoryPathSteps = 5;
        
        private TrajectoryTracker tracker;
        
        public Vector3 CurrentPosition => tracker?.CurrentPosition ?? Vector3.zero;
        public Vector3 CurrentVelocity => tracker?.CurrentVelocity ?? Vector3.zero;
        public bool IsInitialized => tracker?.IsInitialized ?? false;
        
        private void Awake()
        {
            tracker = new TrajectoryTracker(historySize);
        }
        
        /// <summary>
        /// Add a new shuttle detection measurement
        /// </summary>
        public void AddMeasurement(ShuttleData shuttleData)
        {
            if (shuttleData == null)
                return;
                
            // Filter low confidence detections
            if (shuttleData.Confidence < minConfidenceThreshold)
                return;
            
            // Add measurement to tracker
            float timestamp = Time.time;
            tracker.AddMeasurement(shuttleData.Position, timestamp);
            
            // Validate velocity (filter out unrealistic spikes)
            if (tracker.CurrentVelocity.magnitude > maxVelocityThreshold)
            {
                Debug.LogWarning($"Unrealistic velocity detected: {tracker.CurrentVelocity.magnitude} m/s. Resetting tracker.");
                tracker.Reset();
            }
        }
        
        /// <summary>
        /// Get predicted position at a future time
        /// </summary>
        public Vector3 PredictPosition(float secondsAhead)
        {
            if (!enablePrediction || !IsInitialized)
                return CurrentPosition;
                
            return tracker.PredictPosition(secondsAhead);
        }
        
        /// <summary>
        /// Get predicted position using default prediction time
        /// </summary>
        public Vector3 GetPredictedPosition()
        {
            return PredictPosition(predictionTimeAhead);
        }
        
        /// <summary>
        /// Get trajectory path for visualization or collision prediction
        /// </summary>
        public Vector3[] GetTrajectoryPath()
        {
            if (!IsInitialized)
                return new Vector3[0];
                
            return tracker.GetTrajectoryPath(predictionTimeAhead * trajectoryPathSteps, trajectoryPathSteps);
        }
        
        /// <summary>
        /// Get average velocity over recent history
        /// </summary>
        public Vector3 GetAverageVelocity()
        {
            if (!IsInitialized)
                return Vector3.zero;
                
            return tracker.GetAverageVelocity();
        }
        
        /// <summary>
        /// Reset the trajectory tracker
        /// </summary>
        public void Reset()
        {
            tracker?.Reset();
        }
        
        /// <summary>
        /// Check if shuttle is moving (velocity above threshold)
        /// </summary>
        public bool IsShuttleMoving(float velocityThreshold = 0.5f)
        {
            return IsInitialized && CurrentVelocity.magnitude > velocityThreshold;
        }
        
        /// <summary>
        /// Get time until predicted collision with a plane
        /// </summary>
        public bool TryGetTimeToPlane(Plane plane, out float timeToCollision)
        {
            timeToCollision = 0f;
            
            if (!IsInitialized || !IsShuttleMoving())
                return false;
            
            Vector3 velocity = CurrentVelocity;
            Vector3 position = CurrentPosition;
            
            // Calculate intersection time using plane equation
            float velocityDotNormal = Vector3.Dot(velocity, plane.normal);
            
            // Check if moving towards plane
            if (Mathf.Abs(velocityDotNormal) < 0.01f)
                return false;
            
            float distance = plane.GetDistanceToPoint(position);
            timeToCollision = -distance / velocityDotNormal;
            
            // Only return valid future times
            return timeToCollision > 0;
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!IsInitialized || !Application.isPlaying)
                return;
            
            // Draw current position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(CurrentPosition, 0.05f);
            
            // Draw velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(CurrentPosition, CurrentVelocity * 0.1f);
            
            // Draw predicted trajectory
            if (enablePrediction)
            {
                Vector3[] path = GetTrajectoryPath();
                Gizmos.color = Color.cyan;
                
                Vector3 previousPoint = CurrentPosition;
                foreach (Vector3 point in path)
                {
                    Gizmos.DrawLine(previousPoint, point);
                    Gizmos.DrawWireSphere(point, 0.02f);
                    previousPoint = point;
                }
            }
        }
    }
}
