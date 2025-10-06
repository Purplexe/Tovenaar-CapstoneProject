using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    Canvas canvas;
    CanvasGroup cg;
    Card card;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        cg = GetComponent<CanvasGroup>();
        card = GetComponent<Card>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TurnManager.instance.IsPlayersTurn(card.Owner)) { eventData.pointerDrag = null; return; }
        originalParent = transform.parent;
        transform.SetParent(canvas.transform);
        if (cg) { cg.blocksRaycasts = false; cg.alpha = 0.8f; }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TurnManager.instance.IsPlayersTurn(card.Owner)) return;
        RectTransform rt = transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
        rt.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (cg) { cg.blocksRaycasts = true; cg.alpha = 1f; }
        if (transform.parent == canvas.transform) transform.SetParent(originalParent); // not dropped on a Lane
        var ui = GetComponent<CardUI>(); if (ui) ui.UpdateUI();
    }
}
