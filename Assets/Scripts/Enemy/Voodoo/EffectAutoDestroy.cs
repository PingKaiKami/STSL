using UnityEngine;

public class EffectAutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}