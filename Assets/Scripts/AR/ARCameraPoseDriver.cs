using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections.Generic;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Manual camera pose driver that:
    /// 1. Ensures the XR loader is initialized and running
    /// 2. Reads camera pose directly from XR InputDevice API
    /// 3. Falls back to XRInputSubsystem if InputDevice API fails
    /// </summary>
    public class ARCameraPoseDriver : MonoBehaviour
    {
        private List<InputDevice> devices = new List<InputDevice>();
        private bool hasLoggedTracking = false;
        private bool hasLoggedDiagnostics = false;
        private float logTimer = 0f;
        private float diagnosticTimer = 0f;
        private bool xrInitAttempted = false;
        
        private void Start()
        {
            Debug.Log("[ARCameraPoseDriver] Starting...");
            EnsureXRInitialized();
            LogDiagnostics();
        }
        
        private void EnsureXRInitialized()
        {
            if (xrInitAttempted) return;
            xrInitAttempted = true;
            
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings == null)
            {
                Debug.LogError("[ARCameraPoseDriver] XRGeneralSettings.Instance is NULL!");
                return;
            }
            
            Debug.Log($"[ARCameraPoseDriver] XRGeneralSettings found, InitManagerOnStart: {xrSettings.InitManagerOnStart}");
            
            var xrManager = xrSettings.Manager;
            if (xrManager == null)
            {
                Debug.LogError("[ARCameraPoseDriver] XR Manager is NULL!");
                return;
            }
            
            Debug.Log($"[ARCameraPoseDriver] XR Manager found, isInitializationComplete: {xrManager.isInitializationComplete}");
            
            // If the loader hasn't been initialized, do it now
            if (!xrManager.isInitializationComplete)
            {
                Debug.Log("[ARCameraPoseDriver] XR loader NOT initialized! Initializing manually...");
                xrManager.InitializeLoaderSync();
                
                if (xrManager.isInitializationComplete)
                {
                    Debug.Log("[ARCameraPoseDriver] XR loader initialized successfully!");
                }
                else
                {
                    Debug.LogError("[ARCameraPoseDriver] Failed to initialize XR loader!");
                    return;
                }
            }
            
            var loader = xrManager.activeLoader;
            if (loader == null)
            {
                Debug.LogError("[ARCameraPoseDriver] No active XR loader after initialization!");
                return;
            }
            
            Debug.Log($"[ARCameraPoseDriver] Active XR loader: {loader.name}");
            
            // Start subsystems if not running
            if (!xrManager.isInitializationComplete) return;
            
            // Check and start input subsystem
            var inputSubsystem = loader.GetLoadedSubsystem<XRInputSubsystem>();
            if (inputSubsystem != null)
            {
                Debug.Log($"[ARCameraPoseDriver] XRInputSubsystem found, running: {inputSubsystem.running}");
                if (!inputSubsystem.running)
                {
                    inputSubsystem.Start();
                    Debug.Log("[ARCameraPoseDriver] XRInputSubsystem started manually!");
                }
            }
            else
            {
                Debug.LogError("[ARCameraPoseDriver] XRInputSubsystem NOT found on loader!");
                
                // Try to start all subsystems
                Debug.Log("[ARCameraPoseDriver] Attempting to start all subsystems...");
                xrManager.StartSubsystems();
            }
        }
        
        private void LogDiagnostics()
        {
            if (hasLoggedDiagnostics) return;
            hasLoggedDiagnostics = true;
            
            var allDevices = new List<InputDevice>();
            InputDevices.GetDevices(allDevices);
            Debug.Log($"[ARCameraPoseDriver] XR input devices: {allDevices.Count}");
        }
        
        private void Update()
        {
            UpdateCameraPose();
        }
        
        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }
        
        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }
        
        [BeforeRenderOrder(100)]
        private void OnBeforeRender()
        {
            UpdateCameraPose();
        }
        
        private void UpdateCameraPose()
        {
            // Try CenterEye first
            if (TryApplyPoseFromNode(XRNode.CenterEye)) return;
            // Fallback: Head
            if (TryApplyPoseFromNode(XRNode.Head)) return;
            // Fallback: any HMD
            if (TryApplyPoseFromHMD()) return;
            // Fallback: any device with position
            if (TryApplyPoseFromAnyDevice()) return;
        }
        
        private bool TryApplyPoseFromNode(XRNode node)
        {
            InputDevices.GetDevicesAtXRNode(node, devices);
            if (devices.Count == 0) return false;
            
            var device = devices[0];
            bool applied = false;
            
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                transform.localPosition = position;
                applied = true;
            }
            
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                transform.localRotation = rotation;
                applied = true;
            }
            
            if (applied && !hasLoggedTracking)
            {
                hasLoggedTracking = true;
                Debug.Log($"[ARCameraPoseDriver] ✅ Tracking via {node}: pos={position}, rot={rotation.eulerAngles}");
            }
            
            LogPeriodic();
            return applied;
        }
        
        private bool TryApplyPoseFromHMD()
        {
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count == 0) return false;
            
            return ApplyFromDevice(devices[0], "HMD");
        }
        
        private bool TryApplyPoseFromAnyDevice()
        {
            devices.Clear();
            InputDevices.GetDevices(devices);
            
            foreach (var device in devices)
            {
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                {
                    transform.localPosition = pos;
                    
                    if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                        transform.localRotation = rot;
                    
                    if (!hasLoggedTracking)
                    {
                        hasLoggedTracking = true;
                        Debug.Log($"[ARCameraPoseDriver] ✅ Tracking via device '{device.name}': pos={pos}");
                    }
                    
                    LogPeriodic();
                    return true;
                }
            }
            
            return false;
        }
        
        private bool ApplyFromDevice(InputDevice device, string label)
        {
            bool applied = false;
            
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            {
                transform.localPosition = pos;
                applied = true;
            }
            
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                transform.localRotation = rot;
                applied = true;
            }
            
            if (applied && !hasLoggedTracking)
            {
                hasLoggedTracking = true;
                Debug.Log($"[ARCameraPoseDriver] ✅ Tracking via {label} '{device.name}': pos={pos}");
            }
            
            LogPeriodic();
            return applied;
        }
        
        private void LogPeriodic()
        {
            // Disabled: camera position logging removed to reduce log noise
        }
    }
}
