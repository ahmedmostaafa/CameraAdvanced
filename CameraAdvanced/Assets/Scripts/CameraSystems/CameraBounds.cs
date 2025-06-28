using UnityEngine;

namespace CameraSystems
{
    [RequireComponent(typeof(BoxCollider))]
    public class CameraBounds : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
        }
#endif
    }
}
