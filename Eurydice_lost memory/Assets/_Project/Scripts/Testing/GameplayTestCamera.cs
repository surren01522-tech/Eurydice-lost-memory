using UnityEngine;

namespace Eurydice.Gameplay.Testing
{
    [DisallowMultipleComponent]
    public sealed class GameplayTestCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.5f, -6f);
        [SerializeField, Min(0f)] private float followSharpness = 12f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.TransformPoint(offset);
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * 1.2f - transform.position);
        }
    }
}
