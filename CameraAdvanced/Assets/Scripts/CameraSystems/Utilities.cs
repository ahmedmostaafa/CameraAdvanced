using Unity.Cinemachine;
using UnityEngine;

namespace CameraSystems
{
    public static class Utilities
    {
        public static Rect CameraRectFor(this Transform t, CinemachineCamera c = null)
        {
            if (!t) return default;
            if (!c) return default;

            var mainCamera = CinemachineCore.FindPotentialTargetBrain(c).OutputCamera;
            if (!mainCamera) return default;
            if (mainCamera.orthographic)
            {
                var height = 2f * mainCamera.orthographicSize;
                var width = height * mainCamera.aspect;
                var pos = new Vector2(-width / 2, -height / 2);
                return new Rect(pos.x, pos.y, width, height);
            }

            var depth = Mathf.Abs(Vector3.Dot(t.position - mainCamera.transform.position, mainCamera.transform.forward));
            return GetCameraFrustumRectAtDepth(mainCamera, depth);
        }

        private static Rect GetCameraFrustumRectAtDepth(Camera c, float depth)
        {
            var frustumCorners = new Vector3[4];
            c.CalculateFrustumCorners(new Rect(0, 0, 1, 1), depth, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
            
            var minX = Mathf.Min(frustumCorners[0].x, frustumCorners[1].x, frustumCorners[2].x, frustumCorners[3].x);
            var maxX = Mathf.Max(frustumCorners[0].x, frustumCorners[1].x, frustumCorners[2].x, frustumCorners[3].x);
            var minY = Mathf.Min(frustumCorners[0].y, frustumCorners[1].y, frustumCorners[2].y, frustumCorners[3].y);
            var maxY = Mathf.Max(frustumCorners[0].y, frustumCorners[1].y, frustumCorners[2].y, frustumCorners[3].y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}