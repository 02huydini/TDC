/*
    Small always-on-screen log (bottom-left corner) that shows the result of
    every tower build/merge/boon attempt, as it happens - unlike DevConsole,
    this needs no backtick toggle, it's just always there. Self-installing,
    same pattern as DevConsole/PathHighlighter: no scene wiring required.

    Call BuildResultLog.Show("message") from anywhere. Lines are color-coded
    (green = succeeded, red = failed/blocked) and the newest few stay on
    screen for a few seconds each before fading out of the list.
*/
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BuildResultLog : MonoBehaviour {
    private const int MaxLines = 5;
    private const float LineLifetime = 4f;

    private static BuildResultLog instance;

    private struct Entry {
        public string text;
        public float expiresAt;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private Text output;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<BuildResultLog>() != null) return;
        GameObject go = new GameObject("~BuildResultLog");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BuildResultLog>();
    }

    // message plain text, e.g. "Built Archer Tower." / "Not enough Wood for Frost Tower."
    // ok = true tints it green (succeeded), false tints it red (failed/blocked).
    public static void Show(string message, bool ok) {
        if (instance == null) return;
        instance.AddEntry(message, ok);
    }

    private void AddEntry(string message, bool ok) {
        string color = ok ? "#7CFC7C" : "#FF6B6B";
        entries.Add(new Entry {
            text = "<color=" + color + ">" + message + "</color>",
            expiresAt = Time.unscaledTime + LineLifetime
        });
        while (entries.Count > MaxLines) entries.RemoveAt(0);
        Refresh();
    }

    private void Update() {
        if (entries.Count == 0) return;
        bool changed = entries.RemoveAll(e => Time.unscaledTime >= e.expiresAt) > 0;
        if (changed) Refresh();
    }

    private void Refresh() {
        if (output == null) return;
        StringBuilder sb = new StringBuilder();
        foreach (Entry e in entries) sb.AppendLine(e.text);
        output.text = sb.ToString();
    }

    private void Awake() {
        BuildUi();
    }

    private void BuildUi() {
        GameObject canvasObj = new GameObject("BuildResultLogCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8000; // below DevConsole (10000), above normal gameplay UI
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - this is a passive display, it should never block clicks.

        GameObject textObj = new GameObject("Output");
        textObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(20f, 20f);
        rect.sizeDelta = new Vector2(600f, 140f);

        output = textObj.AddComponent<Text>();
        output.font = RuntimeUIFont.Get();
        output.fontSize = 22;
        output.color = Color.white;
        output.supportRichText = true;
        output.alignment = TextAnchor.LowerLeft;
        output.horizontalOverflow = HorizontalWrapMode.Overflow;
        output.verticalOverflow = VerticalWrapMode.Overflow;

        // Faint drop shadow so light-colored text still reads over bright grass tiles.
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }
}
