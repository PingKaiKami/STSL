using UnityEngine;

public class WaterPrisonEffect : MonoBehaviour
{
    private Transform followTarget;

    public void Init(Transform target, float duration)
    {
        followTarget = target;
        Destroy(gameObject, duration);
    }

    private void Update()
    {
        if (followTarget == null || !followTarget.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = followTarget.position;
    }
}
