using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LaneDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Optional snap target. If null, the card becomes a child of this lane.")]
    public RectTransform snapTarget;

    [Header("Should the card stretch to fit the lane/target?")]
    public bool stretchToFit = true;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (!dragged) return;

        var draggable = dragged.GetComponent<CardDraggable>();
        if (!draggable) return;

        var parentForCard = (Transform)(snapTarget ? snapTarget : transform);

        dragged.transform.SetParent(parentForCard, false);

        var rt = dragged.transform as RectTransform;

        if (stretchToFit)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        else
        {
            // Center it inside the lane/target
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    // Optional: simple hover feedback (e.g., highlight via a CanvasGroup)
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}
