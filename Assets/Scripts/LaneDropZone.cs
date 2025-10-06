using UnityEngine;
using UnityEngine.EventSystems;

public class SlotDropZone : MonoBehaviour, IDropHandler
{
    RectTransform slot;
   

    void Awake() { slot = transform as RectTransform; }

    public void OnDrop(PointerEventData eventData)
    {
        var go = eventData.pointerDrag;
        if (go == null) return;

        var rt = go.transform as RectTransform;
        go.transform.SetParent(transform, false);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, slot.rect.width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, slot.rect.height);
    }
}
