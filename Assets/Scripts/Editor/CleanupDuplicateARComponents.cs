using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

/// <summary>
/// Editor script to find and remove duplicate AR components from GameManager.
/// The GameManager accidentally has XROrigin, ARPlaneManager, and ARSession
/// components that conflict with the proper XR Origin GameObject.
/// 
/// Run via Unity menu: Tools > Cleanup Duplicate AR Components
/// </summary>
public class CleanupDuplicateARComponents
{
    [MenuItem("Tools/Cleanup Duplicate AR Components")]
    public static void Cleanup()
    {
        // Find the GameManager object
        var gameManagerObj = GameObject.Find("GameManager ");  // Note: has trailing space in scene
        if (gameManagerObj == null)
        {
            gameManagerObj = GameObject.Find("GameManager");
        }
        
        if (gameManagerObj == null)
        {
            Debug.LogError("GameManager not found in scene!");
            return;
        }
        
        int removedCount = 0;
        
        // Remove duplicate XROrigin from GameManager
        var xrOrigins = gameManagerObj.GetComponents<XROrigin>();
        foreach (var xrOrigin in xrOrigins)
        {
            Debug.Log($"Removing duplicate XROrigin from {gameManagerObj.name}");
            Undo.DestroyObjectImmediate(xrOrigin);
            removedCount++;
        }
        
        // Remove duplicate ARPlaneManager from GameManager
        var planeManagers = gameManagerObj.GetComponents<ARPlaneManager>();
        foreach (var pm in planeManagers)
        {
            Debug.Log($"Removing duplicate ARPlaneManager from {gameManagerObj.name}");
            Undo.DestroyObjectImmediate(pm);
            removedCount++;
        }
        
        // Remove duplicate ARSession from GameManager (it should be on AR Session object)
        var sessions = gameManagerObj.GetComponents<ARSession>();
        foreach (var session in sessions)
        {
            Debug.Log($"Removing duplicate ARSession from {gameManagerObj.name}");
            Undo.DestroyObjectImmediate(session);
            removedCount++;
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"CLEANUP COMPLETE: Removed {removedCount} duplicate AR components from GameManager");
            Debug.Log("IMPORTANT: Save the scene (Ctrl+S) after this cleanup!");
            EditorUtility.SetDirty(gameManagerObj);
            
            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
        }
        else
        {
            Debug.Log("No duplicate AR components found on GameManager");
        }
    }
}
