using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActionLogUI : MonoBehaviour
{
    public TMP_Text logText;
    public int maxLines = 30;

    private readonly List<string> _lines = new List<string>();

    private void OnEnable()
    {
        TovenaarGameManager.OnActionLog += HandleLog;
    }

    private void OnDisable()
    {
        TovenaarGameManager.OnActionLog -= HandleLog;
    }

    private void HandleLog(string msg)
    {
        _lines.Add(msg);
        while (_lines.Count > maxLines)
            _lines.RemoveAt(0);

        logText.text = string.Join("\n", _lines);
    }
}
