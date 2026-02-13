using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Manages AR session initialization and lifecycle
    /// </summary>
    public class ARSessionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARPlaneManager planeManager;
        
        [Header("Settings")]
        [SerializeField] private bool autoStartSession = false;
        
        public bool IsSessionActive { get; private set; }
        public TrackingState TrackingState { get; private set; }
        
        private void Awake()
        {
            if (arSession == null)
                arSession = FindObjectOfType<ARSession>();
            
            if (arSession == null)
                Debug.LogError("ARSessionManager: No ARSession found in scene!");
            
            // Force disable at startup to prevent camera usage until mode selection
            if (arSession != null)
                arSession.enabled = false;
        }
        
        private void Start()
        {
            if (autoStartSession)
            {
                StartARSession();
            }
        }
        
        private void Update()
        {
            UpdateTrackingState();
        }
        
        public void StartARSession()
        {
            Debug.Log($"Starting AR Session... (Current State: {ARSession.state})");
            if (arSession != null)
            {
                arSession.enabled = true;
                IsSessionActive = true;
            }
        }
        
        public void StopARSession()
        {
            Debug.Log("Stopping AR Session...");
            if (arSession != null)
                arSession.enabled = false;
            IsSessionActive = false;
        }
        
        public void ResetSession()
        {
            Debug.Log("Resetting AR Session...");
            if (arSession != null)
            {
                // Reset is not available on instance, use static Reset
                // This clears all AR tracking data
                arSession.Reset();
            }
        }
        
        private void UpdateTrackingState()
        {
            TrackingState = ARSession.state == ARSessionState.SessionTracking 
                ? TrackingState.Tracking 
                : TrackingState.None;
        }
        
        public bool IsTrackingValid()
        {
            return TrackingState == TrackingState.Tracking && 
                   ARSession.state == ARSessionState.SessionTracking;
        }
        
        private void OnDestroy()
        {
            StopARSession();
        }
    }
}
