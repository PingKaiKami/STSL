using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro tmp;
    private Color baseColor;
    private float elapsed;

    private const float Lifetime   = 1f;
    private const float RiseSpeed  = 1.5f;

    public static void Create(Vector3 worldPos, float damage, bool isMiss = false)
    {
        GameObject obj = new GameObject("DamagePopup");
        obj.transform.position = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), 0.5f, 0f);
        obj.AddComponent<DamagePopup>().Init(damage, isMiss);
    }

    private void Init(float damage, bool isMiss)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text      = isMiss ? "Miss!" : Mathf.RoundToInt(damage).ToString();
        tmp.fontSize  = isMiss ? 2.5f : 3.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 20;

        baseColor  = isMiss ? Color.white : new Color(1f, 0.25f, 0.25f);
        tmp.color  = baseColor;

        GetComponent<RectTransform>().sizeDelta = new Vector2(3f, 1.5f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * RiseSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, elapsed / Lifetime);
        tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }
}
