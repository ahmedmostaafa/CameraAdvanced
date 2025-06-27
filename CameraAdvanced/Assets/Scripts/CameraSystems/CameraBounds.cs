using UnityEngine;

namespace CameraSystems
{
    [RequireComponent(typeof(BoxCollider))]
    public class CameraBounds : MonoBehaviour
    {
        private BoxCollider _collider;

        public Bounds Bounds => _collider.bounds;

        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
            FindFirstObjectByType<CameraController>().SetCameraBounds(_collider);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
        }
#endif
    }
}
