using UnityEngine;
using System.Collections.Generic;

namespace ARBadmintonNet.Utilities
{
    /// <summary>
    /// Kalman Filter for smooth shuttle trajectory prediction
    /// Reduces noise and provides velocity estimation
    /// </summary>
    public class KalmanFilter
    {
        // State vector: [x, y, z, vx, vy, vz]
        private Vector3 position;
        private Vector3 velocity;
        
        // Covariance matrix (simplified 1D per component)
        private float positionVariance = 1.0f;
        private float velocityVariance = 1.0f;
        
        // Process noise
        private float processNoise = 0.1f;
        
        // Measurement noise
        private float measurementNoise = 1.0f;
        
        public Vector3 Position => position;
        public Vector3 Velocity => velocity;
        
        public KalmanFilter(Vector3 initialPosition, float posVar = 1.0f, float velVar = 1.0f)
        {
            position = initialPosition;
            velocity = Vector3.zero;
            positionVariance = posVar;
            velocityVariance = velVar;
        }
        
        /// <summary>
        /// Prediction step - predict next state based on motion model
        /// </summary>
        public void Predict(float deltaTime)
        {
            // State prediction: x = x + v * dt
            position += velocity * deltaTime;
            
            // Covariance prediction
            positionVariance += velocityVariance * deltaTime * deltaTime + processNoise;
            velocityVariance += processNoise;
        }
        
        /// <summary>
        /// Update step - correct prediction with measurement
        /// </summary>
        public Vector3 Update(Vector3 measurement, float deltaTime)
        {
            // Prediction step
            Predict(deltaTime);
            
            // Calculate Kalman gain
            float kalmanGain = positionVariance / (positionVariance + measurementNoise);
            
            // Update position estimate
            Vector3 innovation = measurement - position;
            position += innovation * kalmanGain;
            
            // Update velocity estimate
            if (deltaTime > 0.001f)
            {
                velocity = innovation / deltaTime;
            }
            
            // Update covariance
            positionVariance *= (1 - kalmanGain);
            
            return position;
        }
        
        /// <summary>
        /// Get predicted position at future time
        /// </summary>
        public Vector3 PredictPosition(float futureTime)
        {
            return position + velocity * futureTime;
        }
        
        /// <summary>
        /// Reset filter with new initial state
        /// </summary>
        public void Reset(Vector3 newPosition)
        {
            position = newPosition;
            velocity = Vector3.zero;
            positionVariance = 1.0f;
            velocityVariance = 1.0f;
        }
        
        public void SetProcessNoise(float noise)
        {
            processNoise = noise;
        }
        
        public void SetMeasurementNoise(float noise)
        {
            measurementNoise = noise;
        }
    }
    
    /// <summary>
    /// Helper class to track shuttle trajectory over time
    /// </summary>
    public class TrajectoryTracker
    {
        private KalmanFilter filter;
        private Queue<Vector3> positionHistory;
        private Queue<float> timestampHistory;
        private int maxHistorySize = 10;
        private float lastUpdateTime;
        
        public Vector3 CurrentPosition => filter.Position;
        public Vector3 CurrentVelocity => filter.Velocity;
        public bool IsInitialized { get; private set; }
        
        public TrajectoryTracker(int historySize = 10)
        {
            maxHistorySize = historySize;
            positionHistory = new Queue<Vector3>(maxHistorySize);
            timestampHistory = new Queue<float>(maxHistorySize);
            IsInitialized = false;
        }
        
        public void AddMeasurement(Vector3 position, float timestamp)
        {
            if (!IsInitialized)
            {
                filter = new KalmanFilter(position);
                IsInitialized = true;
                lastUpdateTime = timestamp;
            }
            else
            {
                float deltaTime = timestamp - lastUpdateTime;
                filter.Update(position, deltaTime);
                lastUpdateTime = timestamp;
            }
            
            // Update history
            positionHistory.Enqueue(position);
            timestampHistory.Enqueue(timestamp);
            
            if (positionHistory.Count > maxHistorySize)
            {
                positionHistory.Dequeue();
                timestampHistory.Dequeue();
            }
        }
        
        public Vector3 PredictPosition(float secondsAhead)
        {
            if (!IsInitialized)
                return Vector3.zero;
                
            return filter.PredictPosition(secondsAhead);
        }
        
        public Vector3[] GetTrajectoryPath(float secondsAhead, int steps)
        {
            if (!IsInitialized)
                return new Vector3[0];
                
            Vector3[] path = new Vector3[steps];
            float timeStep = secondsAhead / steps;
            
            for (int i = 0; i < steps; i++)
            {
                path[i] = filter.PredictPosition(timeStep * (i + 1));
            }
            
            return path;
        }
        
        public Vector3 GetAverageVelocity()
        {
            if (positionHistory.Count < 2)
                return Vector3.zero;
                
            var positions = positionHistory.ToArray();
            var timestamps = timestampHistory.ToArray();
            
            Vector3 totalVelocity = Vector3.zero;
            int count = 0;
            
            for (int i = 1; i < positions.Length; i++)
            {
                float dt = timestamps[i] - timestamps[i - 1];
                if (dt > 0.001f)
                {
                    totalVelocity += (positions[i] - positions[i - 1]) / dt;
                    count++;
                }
            }
            
            return count > 0 ? totalVelocity / count : Vector3.zero;
        }
        
        public void Reset()
        {
            positionHistory.Clear();
            timestampHistory.Clear();
            IsInitialized = false;
        }
    }
}
