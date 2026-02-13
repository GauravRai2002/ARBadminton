using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace ARBadmintonNet.Replay
{
    /// <summary>
    /// Manages iOS ReplayKit clip buffering for instant replay.
    /// Continuously buffers the screen and exports the last N seconds on demand.
    /// </summary>
    public class ReplayManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float replayDuration = 30f;
        
        public event System.Action<string> OnReplayReady;
        public event System.Action<string> OnReplayError;
        
        public bool IsBuffering { get; private set; }
        public bool IsExporting { get; private set; }
        
        private static ReplayManager instance;
        public static ReplayManager Instance => instance;
        
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
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        /// <summary>
        /// Start continuous clip buffering. Call when AR session begins.
        /// </summary>
        public void StartBuffering()
        {
            if (IsBuffering) return;
            
#if UNITY_IOS && !UNITY_EDITOR
            ReplayKit_StartClipBuffering();
            // Check after a short delay
            StartCoroutine(CheckBufferingStarted());
#else
            Debug.Log("[ReplayManager] Clip buffering only available on iOS device");
#endif
        }
        
        /// <summary>
        /// Stop clip buffering. Call when returning to startup screen.
        /// </summary>
        public void StopBuffering()
        {
            if (!IsBuffering) return;
            
#if UNITY_IOS && !UNITY_EDITOR
            ReplayKit_StopClipBuffering();
            IsBuffering = false;
            Debug.Log("[ReplayManager] Buffering stopped");
#endif
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
            
#if UNITY_IOS && !UNITY_EDITOR
            if (!IsBuffering)
            {
                OnReplayError?.Invoke("Replay buffering is not active");
                return;
            }
            
            IsExporting = true;
            ReplayKit_ResetExportStatus();
            ReplayKit_ExportClip(duration);
            StartCoroutine(WaitForExport());
#else
            Debug.Log($"[ReplayManager] ExportReplay({duration}s) — only works on iOS device");
            OnReplayError?.Invoke("Replay only available on iOS device");
#endif
        }
        
#if UNITY_IOS && !UNITY_EDITOR
        private IEnumerator CheckBufferingStarted()
        {
            yield return new WaitForSeconds(1f);
            IsBuffering = ReplayKit_GetIsBuffering();
            if (IsBuffering)
            {
                Debug.Log("[ReplayManager] Clip buffering confirmed active");
            }
            else
            {
                Debug.LogWarning("[ReplayManager] Clip buffering may not have started");
            }
        }
        
        private IEnumerator WaitForExport()
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
                    Debug.Log($"[ReplayManager] Replay exported: {path}");
                    OnReplayReady?.Invoke(path);
                    yield break;
                }
                else if (status == 3) // Error
                {
                    IsExporting = false;
                    System.IntPtr msgPtr = ReplayKit_GetErrorMessage();
                    string msg = Marshal.PtrToStringAnsi(msgPtr);
                    Debug.LogError($"[ReplayManager] Export error: {msg}");
                    OnReplayError?.Invoke(msg);
                    yield break;
                }
                
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            
            IsExporting = false;
            OnReplayError?.Invoke("Export timed out");
            Debug.LogError("[ReplayManager] Export timed out after 15 seconds");
        }
#endif
        
        private void OnDestroy()
        {
            StopBuffering();
            if (instance == this) instance = null;
        }
    }
}
