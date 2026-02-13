using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace ARBadmintonNet.Replay
{
    /// <summary>
    /// Manages screen recording for instant replay.
    /// Detects platform at runtime and delegates to iOS ReplayKit or Android MediaProjection.
    /// </summary>
    public class ReplayManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float replayDuration = 30f;
        
        public event System.Action<string> OnReplayReady;
        public event System.Action<string> OnReplayError;
        public event System.Action OnBufferingStarted;
        public event System.Action<string> OnBufferingFailed;
        
        public bool IsBuffering { get; private set; }
        public bool IsExporting { get; private set; }
        
        private static ReplayManager instance;
        public static ReplayManager Instance => instance;
        
        // ====== iOS Native Declarations ======
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ReplayKit_StartClipBuffering();
        
        [DllImport("__Internal")]
        private static extern void ReplayKit_StopClipBuffering();
        
        [DllImport("__Internal")]
        private static extern void ReplayKit_ExportClip(float duration);
        
        [DllImport("__Internal")]
        private static extern System.IntPtr ReplayKit_GetExportedClipPath();
        
        [DllImport("__Internal")]
        private static extern int ReplayKit_GetExportStatus();
        
        [DllImport("__Internal")]
        private static extern System.IntPtr ReplayKit_GetErrorMessage();
        
        [DllImport("__Internal")]
        private static extern bool ReplayKit_GetIsBuffering();
        
        [DllImport("__Internal")]
        private static extern void ReplayKit_ResetExportStatus();
#endif
        
        // ====== Android Native References ======
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject screenRecorderBridge;
        private bool androidInitialized = false;
#endif
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializePlatform();
        }
        
        private void InitializePlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    var bridgeClass = new AndroidJavaClass("com.arbadmintonnet.replay.ScreenRecorderBridge");
                    screenRecorderBridge = bridgeClass.CallStatic<AndroidJavaObject>("getInstance", currentActivity);
                    androidInitialized = true;
                    Debug.Log("[ReplayManager] Android ScreenRecorderBridge initialized");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ReplayManager] Failed to initialize Android bridge: {e.Message}");
                androidInitialized = false;
            }
#endif
        }
        
        /// <summary>
        /// Start continuous clip buffering. Call when AR session begins.
        /// </summary>
        public void StartBuffering()
        {
            if (IsBuffering) return;
            
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
#if UNITY_IOS && !UNITY_EDITOR
                ReplayKit_StartClipBuffering();
                StartCoroutine(CheckBufferingStarted_iOS());
#endif
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!androidInitialized || screenRecorderBridge == null)
                {
                    Debug.LogError("[ReplayManager] Android bridge not initialized");
                    return;
                }
                screenRecorderBridge.Call("startBuffering");
                StartCoroutine(CheckBufferingStarted_Android());
#endif
            }
            else
            {
                Debug.Log("[ReplayManager] Clip buffering only available on iOS/Android device");
            }
        }
        
        /// <summary>
        /// Stop clip buffering. Call when returning to startup screen.
        /// </summary>
        public void StopBuffering()
        {
            if (!IsBuffering) return;
            
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
#if UNITY_IOS && !UNITY_EDITOR
                ReplayKit_StopClipBuffering();
                IsBuffering = false;
                Debug.Log("[ReplayManager] Buffering stopped (iOS)");
#endif
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (screenRecorderBridge != null)
                {
                    screenRecorderBridge.Call("stopBuffering");
                    IsBuffering = false;
                    Debug.Log("[ReplayManager] Buffering stopped (Android)");
                }
#endif
            }
        }
        
        /// <summary>
        /// Export the last N seconds of buffered video.
        /// </summary>
        public void ExportReplay()
        {
            ExportReplay(replayDuration);
        }
        
        /// <summary>
        /// Export the last N seconds of buffered video.
        /// </summary>
        public void ExportReplay(float duration)
        {
            if (IsExporting)
            {
                Debug.LogWarning("[ReplayManager] Already exporting");
                return;
            }
            
            if (!IsBuffering)
            {
                OnReplayError?.Invoke("Replay buffering is not active");
                return;
            }
            
            IsExporting = true;
            
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
#if UNITY_IOS && !UNITY_EDITOR
                ReplayKit_ResetExportStatus();
                ReplayKit_ExportClip(duration);
                StartCoroutine(WaitForExport_iOS());
#endif
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (screenRecorderBridge != null)
                {
                    screenRecorderBridge.Call("resetExportStatus");
                    screenRecorderBridge.Call("exportClip", duration);
                    StartCoroutine(WaitForExport_Android());
                }
                else
                {
                    IsExporting = false;
                    OnReplayError?.Invoke("Android recorder not available");
                }
#endif
            }
            else
            {
                IsExporting = false;
                Debug.Log($"[ReplayManager] ExportReplay({duration}s) — only works on iOS/Android device");
                OnReplayError?.Invoke("Replay only available on iOS/Android device");
            }
        }
        
        // ====== iOS Coroutines ======
        
#if UNITY_IOS && !UNITY_EDITOR
        private IEnumerator CheckBufferingStarted_iOS()
        {
            yield return new WaitForSeconds(1f);
            IsBuffering = ReplayKit_GetIsBuffering();
            if (IsBuffering)
            {
                Debug.Log("[ReplayManager] Clip buffering confirmed active (iOS)");
                OnBufferingStarted?.Invoke();
            }
            else
            {
                Debug.LogWarning("[ReplayManager] Clip buffering may not have started (iOS)");
                OnBufferingFailed?.Invoke("Screen recording failed to start");
            }
        }
        
        private IEnumerator WaitForExport_iOS()
        {
            float timeout = 15f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                int status = ReplayKit_GetExportStatus();
                
                if (status == 2) // Success
                {
                    IsExporting = false;
                    System.IntPtr pathPtr = ReplayKit_GetExportedClipPath();
                    string path = Marshal.PtrToStringAnsi(pathPtr);
                    Debug.Log($"[ReplayManager] Replay exported (iOS): {path}");
                    OnReplayReady?.Invoke(path);
                    yield break;
                }
                else if (status == 3) // Error
                {
                    IsExporting = false;
                    System.IntPtr msgPtr = ReplayKit_GetErrorMessage();
                    string msg = Marshal.PtrToStringAnsi(msgPtr);
                    Debug.LogError($"[ReplayManager] Export error (iOS): {msg}");
                    OnReplayError?.Invoke(msg);
                    yield break;
                }
                
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            
            IsExporting = false;
            OnReplayError?.Invoke("Export timed out");
            Debug.LogError("[ReplayManager] Export timed out after 15 seconds (iOS)");
        }
#endif
        
        // ====== Android Coroutines ======
        
#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator CheckBufferingStarted_Android()
        {
            yield return new WaitForSeconds(2f); // Android needs a bit longer due to permission prompt
            
            if (screenRecorderBridge != null)
            {
                IsBuffering = screenRecorderBridge.Call<bool>("getIsBuffering");
                if (IsBuffering)
                {
                    Debug.Log("[ReplayManager] Clip buffering confirmed active (Android)");
                    OnBufferingStarted?.Invoke();
                    yield break;
                }
                
                // May still be waiting for user permission — retry
                Debug.LogWarning("[ReplayManager] Clip buffering may not have started (Android) - permission may be pending");
                yield return new WaitForSeconds(5f);
                
                if (screenRecorderBridge != null)
                {
                    IsBuffering = screenRecorderBridge.Call<bool>("getIsBuffering");
                    if (IsBuffering)
                    {
                        Debug.Log("[ReplayManager] Clip buffering confirmed active after retry (Android)");
                        OnBufferingStarted?.Invoke();
                        yield break;
                    }
                }
                
                // Permission was denied or recording failed to start
                string error = screenRecorderBridge.Call<string>("getErrorMessage");
                if (string.IsNullOrEmpty(error)) error = "Screen recording permission was denied";
                Debug.LogWarning($"[ReplayManager] Buffering failed (Android): {error}");
                OnBufferingFailed?.Invoke(error);
            }
        }
        
        private IEnumerator WaitForExport_Android()
        {
            float timeout = 15f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                if (screenRecorderBridge != null)
                {
                    int status = screenRecorderBridge.Call<int>("getExportStatus");
                    
                    if (status == 2) // Success
                    {
                        IsExporting = false;
                        string path = screenRecorderBridge.Call<string>("getExportedClipPath");
                        Debug.Log($"[ReplayManager] Replay exported (Android): {path}");
                        OnReplayReady?.Invoke(path);
                        yield break;
                    }
                    else if (status == 3) // Error
                    {
                        IsExporting = false;
                        string msg = screenRecorderBridge.Call<string>("getErrorMessage");
                        Debug.LogError($"[ReplayManager] Export error (Android): {msg}");
                        OnReplayError?.Invoke(msg);
                        yield break;
                    }
                }
                
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            
            IsExporting = false;
            OnReplayError?.Invoke("Export timed out");
            Debug.LogError("[ReplayManager] Export timed out after 15 seconds (Android)");
        }
#endif

        /// <summary>
        /// Handle Android activity result for MediaProjection permission.
        /// Call this from your main activity's OnActivityResult.
        /// </summary>
        public void HandleAndroidActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (screenRecorderBridge != null)
            {
                screenRecorderBridge.Call("onActivityResult", requestCode, resultCode, data);
            }
#endif
        }
        
        private void OnDestroy()
        {
            StopBuffering();
            
#if UNITY_ANDROID && !UNITY_EDITOR
            if (screenRecorderBridge != null)
            {
                screenRecorderBridge.Call("cleanup");
                screenRecorderBridge.Dispose();
                screenRecorderBridge = null;
            }
#endif
            
            if (instance == this) instance = null;
        }
    }
}
