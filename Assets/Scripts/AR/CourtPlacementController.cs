using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Controls placement and positioning of the virtual badminton court markings.
    /// Mirrors NetPlacementController's pattern: tap-to-place, world-space locking, movement, rotation.
    /// </summary>
    public class CourtPlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private GameObject courtPrefab;
        [SerializeField] private GameObject netPrefab;
        
        private GameObject courtInstance;
        private GameObject netChild;
        private bool isPlacementMode = true;
        
        // Store the placed world position
        private Vector3 placedWorldPosition;
        private Quaternion placedWorldRotation;
        private bool hasPlacedCourt = false;
        private bool isLocked = false;
        
        public bool IsCourtPlaced => hasPlacedCourt;
        public bool IsCourtLocked => isLocked;
        
        public delegate void CourtPlacedHandler(Vector3 position, Quaternion rotation);
        public event CourtPlacedHandler OnCourtPlaced;
        
        public delegate void CourtRemovedHandler();
        public event CourtRemovedHandler OnCourtRemoved;
        
        public event System.Action OnCourtLocked;
        public event System.Action OnCourtUnlocked;
        
        private void Awake()
        {
            if (arCamera == null)
                arCamera = Camera.main;
                
            if (raycastManager == null)
                raycastManager = FindObjectOfType<ARRaycastManager>();
        }
        
        private void Update()
        {
            if (isPlacementMode && !isLocked)
            {
                // Place court on tap
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        TryPlaceCourt(touch.position);
                    }
                }
                
                #if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0))
                {
                    TryPlaceCourt(Input.mousePosition);
                }
                #endif
            }
        }
        
        private void LateUpdate()
        {
            if (courtInstance != null && hasPlacedCourt)
            {
                // Force world position every frame
                courtInstance.transform.position = placedWorldPosition;
                courtInstance.transform.rotation = placedWorldRotation;
                
                if (courtInstance.transform.parent != null)
                {
                    courtInstance.transform.SetParent(null, true);
                }
            }
        }
        
        public void TryPlaceCourt(Vector2 screenPosition)
        {
            if (TryGetPlacementPose(screenPosition, out Vector3 position, out Quaternion rotation))
            {
                PlaceCourt(position, rotation);
            }
        }
        
        private bool TryGetPlacementPose(Vector2 screenPosition, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            
            var hits = new System.Collections.Generic.List<ARRaycastHit>();
            if (raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;
                position = hitPose.position;
                // Court lies flat on the ground, Y rotation from hit
                rotation = Quaternion.Euler(0, hitPose.rotation.eulerAngles.y, 0);
                return true;
            }
            
            return false;
        }
        
        public void PlaceCourt(Vector3 position, Quaternion rotation)
        {
            if (courtInstance != null)
            {
                RemoveCourt();
            }
            
            placedWorldPosition = position;
            placedWorldRotation = rotation;
            
            courtInstance = Instantiate(courtPrefab);
            courtInstance.transform.SetParent(null, true);
            courtInstance.transform.position = position;
            courtInstance.transform.rotation = rotation;
            
            // Instantiate net as child of court
            Debug.Log($"[CourtPlacement] Attempting to attach net. netPrefab is: {(netPrefab != null ? "ASSIGNED" : "NULL/NOT ASSIGNED")}");
            
            if (netPrefab != null)
            {
                Debug.Log($"[CourtPlacement] Instantiating net prefab: {netPrefab.name}");
                netChild = Instantiate(netPrefab, courtInstance.transform);
                
                Debug.Log($"[CourtPlacement] Net instantiated: {(netChild != null ? "SUCCESS" : "FAILED")}");
                
                if (netChild != null)
                {
                    // Position net at center of court (local space)
                    // Net height is 1.55m, so we position it at 0.775m (half height) above ground
                    netChild.transform.localPosition = new Vector3(0, 0.775f, 0);
                    netChild.transform.localRotation = Quaternion.identity;
                    
                    // Scale net to match court width
                    // Net default width is 5.18m (singles), court doubles width is 6.10m
                    // Scale factor: 6.10 / 5.18 = 1.178
                    float courtDoublesWidth = 6.10f; // BWF standard doubles width
                    float netDefaultWidth = 5.18f;   // Net's default width (singles)
                    float scaleX = courtDoublesWidth / netDefaultWidth;
                    netChild.transform.localScale = new Vector3(scaleX, 1f, 1f);
                    
                    Debug.Log($"[CourtPlacement] Net attached to court at center. Active: {netChild.activeSelf}, Position: {netChild.transform.position}, Scale: {netChild.transform.localScale}");
                }
            }
            else
            {
                Debug.LogWarning("[CourtPlacement] ⚠️ NET PREFAB NOT ASSIGNED! Go to Inspector and assign the BadmintonNet prefab to the 'Net Prefab' field.");
            }
            
            hasPlacedCourt = true;
            isPlacementMode = false;
            
            Debug.Log($"Court placed at {position}");
            OnCourtPlaced?.Invoke(position, rotation);
        }
        
        public void RemoveCourt()
        {
            if (courtInstance != null)
            {
                Destroy(courtInstance);
                courtInstance = null;
            }
            
            hasPlacedCourt = false;
            isLocked = false;
            isPlacementMode = true;
            
            OnCourtRemoved?.Invoke();
        }
        
        public void LockCourt()
        {
            isLocked = true;
            OnCourtLocked?.Invoke();
        }
        
        public void UnlockCourt()
        {
            isLocked = false;
            OnCourtUnlocked?.Invoke();
        }
        
        public void MoveCourt(Vector3 delta)
        {
            if (courtInstance == null || !hasPlacedCourt) return;
            
            placedWorldPosition += delta;
            courtInstance.transform.position = placedWorldPosition;
        }
        
        public void RotateCourt(float degrees)
        {
            RotateCourtAxis(Vector3.up, degrees);
        }
        
        public void RotateCourtAxis(Vector3 axis, float degrees)
        {
            if (courtInstance == null || !hasPlacedCourt) return;
            
            placedWorldRotation *= Quaternion.AngleAxis(degrees, axis);
            courtInstance.transform.rotation = placedWorldRotation;
        }
        
        public void EnablePlacementMode()
        {
            isPlacementMode = true;
        }
        
        public void DisablePlacementMode()
        {
            isPlacementMode = false;
        }
        
        public GameObject GetCourtInstance()
        {
            return courtInstance;
        }
        
        /// <summary>
        /// Get the net instance that is attached to the court
        /// </summary>
        public GameObject GetNetInstance()
        {
            return netChild;
        }
        
        /// <summary>
        /// Show or hide the court markings
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (courtInstance != null)
            {
                courtInstance.SetActive(visible);
            }
        }
    }
}
