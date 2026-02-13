using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Automatically sets iOS build settings required for AR
/// </summary>
public class iOSBuildSettings : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS)
        {
            // Set Camera Usage Description (required for ARKit)
            PlayerSettings.iOS.cameraUsageDescription = "Required for AR tracking and shuttle detection";
            
            // Set other recommended iOS settings
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // ARM64
            
            UnityEngine.Debug.Log("iOS build settings configured automatically");
        }
    }
}
