using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    private Transform followTarget;
    private CharacterBase targetStats;

    public void Init(Transform target, float duration)
    {
        followTarget = target;
        targetStats  = target.GetComponent<CharacterBase>();
        Destroy(gameObject, duration);
    }

    private void Update()
    {
        if (followTarget == null || !followTarget.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        if (targetStats != null && targetStats.shield <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = followTarget.position;
    }
}
