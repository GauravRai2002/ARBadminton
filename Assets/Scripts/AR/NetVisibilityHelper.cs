using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Hides AR plane visualization when the net is placed.
    /// The ProceduralNetMesh component now handles the net's appearance.
    /// </summary>
    public class NetVisibilityHelper : MonoBehaviour
    {
        private void Start()
        {
            // Hide AR plane visualizations
            HideARPlanes();
        }
        
        private void HideARPlanes()
        {
            // Find and disable AR plane mesh renderers
            ARPlaneManager planeManager = FindObjectOfType<ARPlaneManager>();
            if (planeManager != null)
            {
                // Disable plane visualization for all existing planes
                foreach (var plane in planeManager.trackables)
                {
                    var meshRenderer = plane.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        meshRenderer.enabled = false;
                    }
                    
                    var lineRenderer = plane.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = false;
                    }
                }
                
                Debug.Log("AR plane visualizations hidden");
            }
        }
    }
}
