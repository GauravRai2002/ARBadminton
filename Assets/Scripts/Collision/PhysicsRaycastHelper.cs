using UnityEngine;

namespace ARBadmintonNet.Collision
{
    /// <summary>
    /// Utility functions for physics-based raycasting and geometric calculations
    /// Used primarily for collision detection with the virtual net
    /// </summary>
    public static class PhysicsRaycastHelper
    {
        /// <summary>
        /// Calculate line-plane intersection point
        /// Returns true if intersection exists, false if line is parallel to plane
        /// </summary>
        public static bool LinePlaneIntersection(Vector3 linePoint1, Vector3 linePoint2, 
            Plane plane, out Vector3 intersectionPoint)
        {
            intersectionPoint = Vector3.zero;
            
            Vector3 lineDirection = linePoint2 - linePoint1;
            float lineLength = lineDirection.magnitude;
            
            if (lineLength < 0.0001f)
                return false;
            
            lineDirection.Normalize();
            
            // Check if line is parallel to plane
            float dotProduct = Vector3.Dot(lineDirection, plane.normal);
            if (Mathf.Abs(dotProduct) < 0.0001f)
                return false;
            
            // Calculate intersection using parametric line equation
            Ray ray = new Ray(linePoint1, lineDirection);
            
            if (plane.Raycast(ray, out float enter))
            {
                // Check if intersection is within line segment
                if (enter >= 0 && enter <= lineLength)
                {
                    intersectionPoint = ray.GetPoint(enter);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a point is within a rectangular bounds in 3D space
        /// </summary>
        public static bool IsPointInBounds(Vector3 point, Vector3 boundsCenter, 
            Vector3 boundsSize, Quaternion boundsRotation)
        {
            // Transform point to local space of bounds
            Matrix4x4 matrix = Matrix4x4.TRS(boundsCenter, boundsRotation, Vector3.one);
            Vector3 localPoint = matrix.inverse.MultiplyPoint3x4(point);
            
            // Check if within bounds
            Vector3 halfSize = boundsSize * 0.5f;
            return Mathf.Abs(localPoint.x) <= halfSize.x &&
                   Mathf.Abs(localPoint.y) <= halfSize.y &&
                   Mathf.Abs(localPoint.z) <= halfSize.z;
        }
        
        /// <summary>
        /// Check if a point is within a rectangular bounds aligned with a plane
        /// More efficient for 2D checks in 3D space
        /// </summary>
        public static bool IsPointInPlaneBounds(Vector3 point, Vector3 planeCenter, 
            Vector3 planeNormal, float width, float height, Vector3 upDirection)
        {
            // Create coordinate system on the plane
            Vector3 right = Vector3.Cross(upDirection, planeNormal).normalized;
            Vector3 up = Vector3.Cross(planeNormal, right).normalized;
            
            // Project point onto plane
            Vector3 toPoint = point - planeCenter;
            float xOffset = Vector3.Dot(toPoint, right);
            float yOffset = Vector3.Dot(toPoint, up);
            
            // Check if within bounds
            return Mathf.Abs(xOffset) <= width * 0.5f && 
                   Mathf.Abs(yOffset) <= height * 0.5f;
        }
        
        /// <summary>
        /// Calculate the closest point on a line segment to another point
        /// </summary>
        public static Vector3 ClosestPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float lineLength = line.magnitude;
            
            if (lineLength < 0.0001f)
                return lineStart;
            
            line.Normalize();
            
            Vector3 toPoint = point - lineStart;
            float dot = Vector3.Dot(toPoint, line);
            
            // Clamp to line segment
            dot = Mathf.Clamp(dot, 0f, lineLength);
            
            return lineStart + line * dot;
        }
        
        /// <summary>
        /// Calculate distance from a point to a line segment
        /// </summary>
        public static float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 closestPoint = ClosestPointOnLineSegment(point, lineStart, lineEnd);
            return Vector3.Distance(point, closestPoint);
        }
        
        /// <summary>
        /// Check if two line segments intersect in 3D space
        /// Returns true if they are close enough (within threshold)
        /// </summary>
        public static bool LineSegmentsIntersect(Vector3 line1Start, Vector3 line1End,
            Vector3 line2Start, Vector3 line2End, float threshold = 0.01f)
        {
            // Find closest points on both line segments
            Vector3 closest1 = ClosestPointOnLineSegment(line2Start, line1Start, line1End);
            Vector3 closest2 = ClosestPointOnLineSegment(closest1, line2Start, line2End);
            
            float distance = Vector3.Distance(closest1, closest2);
            return distance <= threshold;
        }
        
        /// <summary>
        /// Perform a sphere cast between two points
        /// </summary>
        public static bool SphereCast(Vector3 startPoint, Vector3 endPoint, 
            float radius, out RaycastHit hit, int layerMask = Physics.DefaultRaycastLayers)
        {
            Vector3 direction = endPoint - startPoint;
            float distance = direction.magnitude;
            
            if (distance < 0.0001f)
            {
                hit = new RaycastHit();
                return false;
            }
            
            direction.Normalize();
            return Physics.SphereCast(startPoint, radius, direction, out hit, distance, layerMask);
        }
        
        /// <summary>
        /// Check if a mesh collider is hit by a line segment
        /// </summary>
        public static bool LinecastMesh(Vector3 startPoint, Vector3 endPoint, 
            MeshCollider meshCollider, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            
            if (meshCollider == null)
                return false;
            
            Vector3 direction = endPoint - startPoint;
            float distance = direction.magnitude;
            
            if (distance < 0.0001f)
                return false;
            
            direction.Normalize();
            Ray ray = new Ray(startPoint, direction);
            
            if (meshCollider.Raycast(ray, out RaycastHit hit, distance))
            {
                hitPoint = hit.point;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Determine which side of a plane a point is on
        /// Returns: 1 = front (same side as normal), -1 = back, 0 = on plane
        /// </summary>
        public static int GetSideOfPlane(Vector3 point, Plane plane, float threshold = 0.01f)
        {
            float distance = plane.GetDistanceToPoint(point);
            
            if (Mathf.Abs(distance) < threshold)
                return 0;
            
            return distance > 0 ? 1 : -1;
        }
        
        /// <summary>
        /// Create a plane from a transform (uses transform's XZ plane)
        /// </summary>
        public static Plane CreatePlaneFromTransform(Transform transform)
        {
            return new Plane(transform.forward, transform.position);
        }
        
        /// <summary>
        /// Debug visualization of a line segment
        /// </summary>
        public static void DrawLineSegment(Vector3 start, Vector3 end, Color color, float duration = 0f)
        {
            Debug.DrawLine(start, end, color, duration);
        }
        
        /// <summary>
        /// Debug visualization of a plane as a grid
        /// </summary>
        public static void DrawPlane(Vector3 position, Vector3 normal, Vector3 up, 
            float width, float height, Color color, float duration = 0f)
        {
            Vector3 right = Vector3.Cross(up, normal).normalized;
            up = Vector3.Cross(normal, right).normalized;
            
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            
            Vector3 topLeft = position + up * halfHeight - right * halfWidth;
            Vector3 topRight = position + up * halfHeight + right * halfWidth;
            Vector3 bottomLeft = position - up * halfHeight - right * halfWidth;
            Vector3 bottomRight = position - up * halfHeight + right * halfWidth;
            
            Debug.DrawLine(topLeft, topRight, color, duration);
            Debug.DrawLine(topRight, bottomRight, color, duration);
            Debug.DrawLine(bottomRight, bottomLeft, color, duration);
            Debug.DrawLine(bottomLeft, topLeft, color, duration);
            
            // Draw diagonals
            Debug.DrawLine(topLeft, bottomRight, color, duration);
            Debug.DrawLine(topRight, bottomLeft, color, duration);
            
            // Draw normal
            Debug.DrawRay(position, normal * 0.5f, Color.yellow, duration);
        }
    }
}
