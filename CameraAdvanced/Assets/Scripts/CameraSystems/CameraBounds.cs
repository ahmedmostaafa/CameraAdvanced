using UnityEngine;

namespace CameraSystems
{
    [RequireComponent(typeof(BoxCollider))]
    public class CameraBounds : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            var size = GetComponent<BoxCollider>().size;
            var center = transform.position;
            Gizmos.DrawWireCube(center, size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size+Vector3.one*25f);
        }
#endif
    }
}
