using UnityEngine;

public class GridSlot : MonoBehaviour
{
    public bool isOccupied = false;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetSlotVisible(bool isVisible)
    {
        // 如果已經被佔用了，就絕對不要顯示
        if (isOccupied && isVisible) return;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = isVisible;
        }
    }
}