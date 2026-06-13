using UnityEngine;

public class EndureEffect : MonoBehaviour
{
    public float scale = 1f;

    private Transform followTarget;

    public void Init(Transform target)
    {
        followTarget = target;
        transform.localScale = Vector3.one * scale;
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
