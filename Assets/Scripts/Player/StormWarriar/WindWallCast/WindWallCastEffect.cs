using UnityEngine;

public class WindWallCastEffect : MonoBehaviour
{
    private Transform followTarget;

    public void Init(Transform target)
    {
        followTarget = target;
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
