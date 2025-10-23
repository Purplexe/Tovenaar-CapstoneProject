using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Optional: Assign, or will auto-find")]
    public Canvas rootCanvas;
    public RectTransform dragLayer;

    RectTransform rt;
    CanvasGroup cg;
    Transform originalParent;
    int originalSibling;
    LayoutElement layoutElement;
    GameObject placeholder;
    Vector2 dragOffset; // world-space offset to kill cursor offset issues

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (!dragLayer && rootCanvas)
        {
            var found = rootCanvas.transform.Find("DragLayer") as RectTransform;
            if (!found)
            {
                var go = new GameObject("DragLayer", typeof(RectTransform));
                found = go.GetComponent<RectTransform>();
                found.SetParent(rootCanvas.transform, false);
                found.anchorMin = Vector2.zero;
                found.anchorMax = Vector2.one;
                found.offsetMin = Vector2.zero;
                found.offsetMax = Vector2.zero;
            }
            dragLayer = found;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSibling = transform.GetSiblingIndex();

        placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(LayoutElement));
        var phRT = placeholder.GetComponent<RectTransform>();
        phRT.SetParent(originalParent, false);
        phRT.SetSiblingIndex(originalSibling);
        var srcLE = layoutElement;
        var dstLE = placeholder.GetComponent<LayoutElement>();
        dstLE.preferredWidth = srcLE.preferredWidth;
        dstLE.preferredHeight = srcLE.preferredHeight;
        dstLE.flexibleWidth = srcLE.flexibleWidth;
        dstLE.flexibleHeight = srcLE.flexibleHeight;
        dstLE.minWidth = srcLE.minWidth;
        dstLE.minHeight = srcLE.minHeight;

        transform.SetParent(dragLayer, true);

        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;
        layoutElement.ignoreLayout = true;

        dragOffset = (Vector2)rt.position - eventData.position;
        rt.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rt.position = eventData.position + (Vector2)dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;
        cg.alpha = 1f;
        layoutElement.ignoreLayout = false;

        // If not dropped onto a DropLane (which will reparent us), return to inventory
        if (transform.parent == dragLayer)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSibling);
        }

        if (placeholder) Destroy(placeholder);
    }
}
