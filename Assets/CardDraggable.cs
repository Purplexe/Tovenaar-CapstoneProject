using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rect;
    private CanvasGroup cg;
    private LayoutElement le;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform dragLayer;
    private Vector2 pointerOffset;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        le = GetComponent<LayoutElement>();

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;

        var t = canvas.transform.Find("DragLayer");
        if (t == null)
        {
            var go = new GameObject("DragLayer", typeof(RectTransform));
            dragLayer = go.GetComponent<RectTransform>();
            dragLayer.SetParent(canvas.transform, false);
            dragLayer.anchorMin = Vector2.zero;
            dragLayer.anchorMax = Vector2.one;
            dragLayer.offsetMin = Vector2.zero;
            dragLayer.offsetMax = Vector2.zero;
        }
        else dragLayer = t as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalAnchoredPos = rect.anchoredPosition;

        le.ignoreLayout = true;
        cg.blocksRaycasts = false;

        transform.SetParent(dragLayer, true);
        transform.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, canvas.worldCamera, out var p);
        pointerOffset = rect.anchoredPosition - p;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, canvas.worldCamera, out var p);
        rect.anchoredPosition = p + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        if (transform.parent == dragLayer)
        {
            transform.SetParent(originalParent, false);
            rect.anchoredPosition = originalAnchoredPos;
        }

        le.ignoreLayout = false;
    }
}
