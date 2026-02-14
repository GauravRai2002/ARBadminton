using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Linq;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Detection
{
    public class YOLODetector : MonoBehaviour
    {
        [Header("Model Settings")]
        [SerializeField] private NNModel modelSource;
        [SerializeField] private float confidenceThreshold = 0.5f;
        [SerializeField] private float iouThreshold = 0.4f;
        
        [Header("Input Settings")]
        [SerializeField] private int inputSize = 640; // YOLOv8 default is 640
        
        // Runtime
        private Model model;
        private IWorker worker;
        private string outputLayerName;
        
        // Cache
        private Texture2D inputTexture;
        private RenderTexture resizeRT;

        public delegate void ObjectsDetectedHandler(List<ShuttleData> objects); // Reusing ShuttleData for generic objects
        public event ObjectsDetectedHandler OnObjectsDetected;

        private void Start()
        {
            if (modelSource != null)
            {
                model = ModelLoader.Load(modelSource);
                // Use ComputePrecompiled for best performance on mobile
                worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
                outputLayerName = model.outputs[0];
                Debug.Log($"[YOLODetector] Model loaded. Output layer: {outputLayerName}");
            }
            else
            {
                Debug.LogError("[YOLODetector] No model assigned!");
            }
        }

        private void OnDestroy()
        {
            worker?.Dispose();
            if (resizeRT != null) resizeRT.Release();
            if (inputTexture != null) Destroy(inputTexture);
        }

        public void ProcessImage(Texture sourceTexture)
        {
            if (worker == null || sourceTexture == null) return;

            // 1. Preprocess: Resize to 640x640
            if (resizeRT == null || resizeRT.width != inputSize)
            {
                if (resizeRT != null) resizeRT.Release();
                resizeRT = RenderTexture.GetTemporary(inputSize, inputSize, 0, RenderTextureFormat.ARGB32);
            }
            
            // Blit to resize
            Graphics.Blit(sourceTexture, resizeRT);
            
            // 2. Create Tensor
            using (var tensor = new Tensor(resizeRT, channels: 3))
            {
                // 3. Execute
                worker.Execute(tensor);
                
                // 4. Parse Output
                using (var output = worker.PeekOutput(outputLayerName))
                {
                    ParseYOLOv8Output(output);
                }
            }
        }

        private void ParseYOLOv8Output(Tensor output)
        {
            // YOLOv8 Output Shape: [1, 8400, 4+Classes] or [1, 4+Classes, 8400]
            // Ultralytics export is usually [1, 4+Classes, 8400] -> Transposed in Barracuda?
            // Let's assume [1, Channels, Width] where Channels = 4+C, Width = 8400
            
            // Output layout:
            // 0: Center X
            // 1: Center Y
            // 2: Width
            // 3: Height
            // 4+C: Class Scores

            int numAnchors = output.width; // 8400
            int numClasses = output.channels - 4; 
            
            // NOTE: Barracuda might load it as [1, 1, Channels, Anchors] or [1, 1, Anchors, Channels]
            // We need to inspect dimensions at runtime or robustly handle it.
            // For standard YOLOv8n.onnx:
            // channels = 84 (4 box + 80 class)
            // width = 8400
            
            List<ShuttleData> detections = new List<ShuttleData>();

            for (int i = 0; i < numAnchors; i++)
            {
                // Find best class score
                float maxScore = 0;
                int bestClass = -1;
                
                // Iterate classes (starting at index 4)
                for (int c = 0; c < numClasses; c++)
                {
                    // Barracuda Tensor access fix:
                    // If shape is [1, 84, 8400], it should be [batch=0, height=c+4, width=i, channels=0] ?
                    // Or more likely [batch=0, channel=?, height=?, width=?].
                    
                    // SAFE, FLAT INDEX ACCESS:
                    // Index = batch*stride_n + height*stride_h + width*stride_w + channel*stride_c
                    // If we don't know the exact strides, let's use the explicit 4D overload if available, 
                    // or just output[batch, height, width, channels].
                    
                    // Assuming Output Shape is [1, C+4, 8400]
                    // Barracuda sees this as [1, C+4, 8400, 1] ? Or [1, 1, C+4, 8400]?
                    
                    // Let's try [0, c+4, i, 0] or similar.
                    // But wait, the error says "No overload takes 3 arguments". 
                    // That implies output[x,y,z] doesn't exist. It wants output[n,h,w,c] (4 args) or output[i] (1 arg).
                    
                    // Let's try explicit 4-arg access assuming standard NCHW or similar layout.
                    // If YOLO export is [1, 84, 8400], then:
                    // batch=0, height=c+4, width=i, channels=0
                    float score = output[0, c + 4, i, 0]; 

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestClass = c;
                    }
                }

                if (maxScore > confidenceThreshold)
                {
                    // Extract Box
                    // 4 args: [0, coord_index, anchor_index, 0]
                    float cx = output[0, 0, i, 0];
                    float cy = output[0, 1, i, 0];
                    float w = output[0, 2, i, 0];
                    float h = output[0, 3, i, 0];

                    // Convert to [0,1] UV space (YOLO outputs pixels relative to 640x640)
                    float imgScale = 1.0f / inputSize;
                    cx *= imgScale;
                    cy *= imgScale;
                    w *= imgScale;
                    h *= imgScale;

                    // Convert to Screen Rect (Top-Left origin for Unity GUI, but Bottom-Left for Shuttledata?)
                    // ShuttleData expects Screen Coords (pixels)
                    float screenX = (cx - w / 2) * Screen.width;
                    float screenY = (1.0f - (cy + h / 2)) * Screen.height; // Flip Y?
                    // YOLO Y is usually Top-Down. Unity Screen is Bottom-Up.
                    // Let's try:
                    screenY = (1.0f - cy - h/2) * Screen.height; // Wait, let's keep it simple.
                    
                    // Correct conversion:
                    // cx, cy are in 0..1 range (Top-Left 0,0)
                    // Unity Screen is Bottom-Left 0,0
                    
                    Vector2 centerScreen = new Vector2(cx * Screen.width, (1.0f - cy) * Screen.height);
                    
                    Rect rect = new Rect(
                        (cx - w/2) * Screen.width, 
                        (1.0f - (cy + h/2)) * Screen.height, 
                        w * Screen.width, 
                        h * Screen.height
                    );
                    
                    // Determine Type
                    // 0: Person, 1: Racket, 2: Shuttle (Use your dataset IDs!)
                    // Assuming standard dataset:
                    // We will classify loosely for now.
                    
                    ShuttleData data = new ShuttleData
                    {
                        ScreenPosition = centerScreen,
                        BoundingBox = rect,
                        Confidence = maxScore,
                        Method = DetectionMethod.ML, // You might need to add this enum value
                        // Tag the class ID somewhere or infer type?
                        // For now treat EVERYTHING as a "Shuttle" candidate for the event
                    };
                    
                    detections.Add(data);
                }
            }

            // NMS (Non-Maximum Suppression) - Simple Version
            var filtered = NonMaxSuppression(detections, iouThreshold);
            
            if (filtered.Count > 0)
            {
                OnObjectsDetected?.Invoke(filtered);
            }
        }

        private List<ShuttleData> NonMaxSuppression(List<ShuttleData> boxes, float limit)
        {
            List<ShuttleData> results = new List<ShuttleData>();
            var sorted = boxes.OrderByDescending(b => b.Confidence).ToList(); // Sort by confidence

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                results.Add(best);
                sorted.RemoveAt(0);

                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    if (GetIoU(best.BoundingBox, sorted[i].BoundingBox) > limit)
                    {
                        sorted.RemoveAt(i);
                    }
                }
            }
            return results;
        }

        private float GetIoU(Rect a, Rect b)
        {
            float intersection = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) * 
                                 Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            float union = a.width * a.height + b.width * b.height - intersection;
            return intersection / union;
        }
    }
}
