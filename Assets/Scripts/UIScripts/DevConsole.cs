/*
    "Nhấn ` để hiển thị console. Console sẽ hiển thị lỗi nếu có, và hiển thị kết quả
    trả về kết quả của command đang thực thi." (toDo2.txt)

    Self-installing: no scene/prefab wiring needed. Builds its own Canvas/InputField/
    Text at runtime on first load and persists across scene loads, so it's available
    from any scene without dragging anything into the Hierarchy.

    Commands (case-insensitive, space-separated args):
      help                 - lists commands
      clear                - clears the output log
      wood <amount>        - adds Wood via WoodManager.main.AddWood (int, can be negative)
      ecto <amount>        - adds Ectos via EctoManager.main.AddEctos (int, can be negative)
      lives <amount>       - sets HealthBar.lives directly (0-5)
      timescale <value>    - sets Time.timeScale directly (debug only - bypasses
                              TimeHandler/FastForward's own speed tracking)
      skipwave             - force-clears every enemy currently alive, letting
                              RoundController's own "no enemies left" check end
                              the wave on its next FixedUpdate
      test                 - "Test: Hiển thị thông số chính xác lên thẳng entities" -
                              dumps exact current stats for every tower and enemy
                              in the scene to the console output
      burnhall [on|off]    - toggles (or explicitly sets) the Main Hall burning
                              test effect - see MainHallBurnTest.cs. Purely visual,
                              no effect on HealthBar.lives.
*/
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DevConsole : MonoBehaviour {
    private const int MaxLines = 200;

    private GameObject root;
    private InputField input;
    private Text output;
    private readonly List<string> lines = new List<string>();
    private bool isOpen = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<DevConsole>() != null) return; // already installed (e.g. scene reload)
        GameObject go = new GameObject("~Console");
        DontDestroyOnLoad(go);
        go.AddComponent<DevConsole>();
    }

    private void Awake() {
        BuildUi();
        Log("Console ready. Type 'help' for commands.");
        SetOpen(false);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            SetOpen(!isOpen);
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Return)) {
            SubmitCommand();
        }
    }

    private void SetOpen(bool open) {
        isOpen = open;
        root.SetActive(open);

        if (open) {
            if (input != null) {
                input.text = "";
                input.ActivateInputField();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(input.gameObject);
            }
        }
    }

    private void SubmitCommand() {
        if (input == null) return;
        string raw = input.text;
        input.text = "";
        input.ActivateInputField();

        if (string.IsNullOrWhiteSpace(raw)) return;

        Log("> " + raw);

        try {
            string result = Execute(raw.Trim());
            if (!string.IsNullOrEmpty(result)) Log(result);
        } catch (Exception e) {
            Log("ERROR: " + e.Message);
        }
    }

    private string Execute(string raw) {
        string[] parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string[] args = new string[parts.Length - 1];
        Array.Copy(parts, 1, args, 0, args.Length);

        switch (cmd) {
            case "help":
                return "Commands: help, clear, wood <amount>, ecto <amount>, lives <amount>, timescale <value>, skipwave, test, burnhall [on|off]";

            case "clear":
                lines.Clear();
                RefreshOutput();
                return null;

            case "wood": {
                int amount = RequireInt(args, 0, "amount");
                if (WoodManager.main == null) return "ERROR: no WoodManager in scene.";
                WoodManager.main.AddWood(amount);
                return "Wood adjusted by " + amount + ".";
            }

            case "ecto": {
                int amount = RequireInt(args, 0, "amount");
                if (EctoManager.main == null) return "ERROR: no EctoManager in scene.";
                EctoManager.main.AddEctos(amount);
                return "Ectos adjusted by " + amount + ".";
            }

            case "lives": {
                float amount = RequireFloat(args, 0, "amount");
                HealthBar.lives = Mathf.Max(0f, amount);
                return "Lives set to " + HealthBar.lives + ".";
            }

            case "timescale": {
                float value = RequireFloat(args, 0, "value");
                Time.timeScale = Mathf.Max(0f, value);
                return "Time.timeScale set to " + Time.timeScale + ".";
            }

            case "skipwave": {
                int count = Counter.enemies.Count;
                foreach (GameObject enemy in new List<GameObject>(Counter.enemies)) {
                    if (enemy != null) Destroy(enemy);
                }
                Counter.enemies.Clear();
                return "Cleared " + count + " enemies - wave will end on the next tick.";
            }

            case "test":
                return DumpEntityStats();

            case "burnhall": {
                if (args.Length == 0) {
                    MainHallBurnTest.Toggle();
                } else {
                    string mode = args[0].ToLowerInvariant();
                    if (mode == "on") MainHallBurnTest.SetActiveState(true);
                    else if (mode == "off") MainHallBurnTest.SetActiveState(false);
                    else throw new Exception("expected 'on' or 'off' (or no argument to toggle).");
                }
                return "Main Hall burning test effect is now " + (MainHallBurnTest.IsBurning ? "ON" : "OFF") + ".";
            }

            default:
                throw new Exception("unknown command '" + cmd + "' (try 'help')");
        }
    }

    private string DumpEntityStats() {
        StringBuilder sb = new StringBuilder();
        sb.Append("Towers (").Append(Counter.towers.Count).Append("):\n");
        foreach (GameObject towerObj in Counter.towers) {
            if (towerObj == null) continue;
            Towers t = towerObj.GetComponent<Towers>();
            if (t == null) continue;
            sb.Append("  ").Append(t.getName())
              .Append(" | dmg=").Append(t.getDamage())
              .Append(" | target=").Append(t.GetTargetModeLabel())
              .Append(" | skillReady=").Append(t.IsSkillReady())
              .Append('\n');
        }

        sb.Append("Enemies (").Append(Counter.enemies.Count).Append("):\n");
        foreach (GameObject enemyObj in Counter.enemies) {
            if (enemyObj == null) continue;
            Enemy e = enemyObj.GetComponent<Enemy>();
            if (e == null) continue;
            sb.Append("  ").Append(enemyObj.name)
              .Append(" | hp=").Append(e.enemyHealth)
              .Append(" | hidden=").Append(e.IsHidden)
              .Append('\n');
        }

        return sb.ToString();
    }

    private static int RequireInt(string[] args, int index, string argName) {
        if (args.Length <= index || !int.TryParse(args[index], out int value))
            throw new Exception("expected an integer for '" + argName + "'.");
        return value;
    }

    private static float RequireFloat(string[] args, int index, string argName) {
        if (args.Length <= index || !float.TryParse(args[index], out float value))
            throw new Exception("expected a number for '" + argName + "'.");
        return value;
    }

    private void Log(string message) {
        lines.Add(message);
        while (lines.Count > MaxLines) lines.RemoveAt(0);
        RefreshOutput();
    }

    private void RefreshOutput() {
        if (output != null) output.text = string.Join("\n", lines);
    }

    // --- Runtime UI construction -------------------------------------------------

    private void BuildUi() {
        // Canvas
        GameObject canvasObj = new GameObject("ConsoleCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000; // always on top
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // FindObjectOfType, not EventSystem.current: this runs at AfterSceneLoad, and
        // relying on .current risked a race against the scene's own EventSystem's
        // OnEnable, which would spawn a *second* EventSystem. Two active EventSystems
        // in one scene makes UI input across the whole game unreliable (this was why
        // clicking into the console's own input field didn't work) - Unity effectively
        // fights over which one owns focus. FindObjectOfType is a direct hierarchy
        // search, not timing-dependent.
        if (FindObjectOfType<EventSystem>() == null) {
            GameObject esObj = new GameObject("EventSystem");
            esObj.transform.SetParent(transform);
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Root panel (top third of the screen, semi-transparent black background)
        root = new GameObject("ConsoleRoot");
        root.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0.65f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Output text
        GameObject outputObj = new GameObject("Output");
        outputObj.transform.SetParent(root.transform, false);
        RectTransform outputRect = outputObj.AddComponent<RectTransform>();
        outputRect.anchorMin = new Vector2(0f, 0.12f);
        outputRect.anchorMax = new Vector2(1f, 1f);
        outputRect.offsetMin = new Vector2(10f, 0f);
        outputRect.offsetMax = new Vector2(-10f, -5f);
        output = outputObj.AddComponent<Text>();
        output.font = RuntimeUIFont.Get();
        output.fontSize = 18;
        output.color = Color.white;
        output.alignment = TextAnchor.LowerLeft;
        output.verticalOverflow = VerticalWrapMode.Truncate;
        output.horizontalOverflow = HorizontalWrapMode.Wrap;

        // Input field
        GameObject inputObj = new GameObject("Input");
        inputObj.transform.SetParent(root.transform, false);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(1f, 0.12f);
        inputRect.offsetMin = new Vector2(10f, 2f);
        inputRect.offsetMax = new Vector2(-10f, -2f);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(1f, 1f, 1f, 0.15f);

        GameObject textAreaObj = new GameObject("Text");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(8f, 4f);
        textAreaRect.offsetMax = new Vector2(-8f, -4f);
        Text inputText = textAreaObj.AddComponent<Text>();
        inputText.font = RuntimeUIFont.Get();
        inputText.fontSize = 18;
        inputText.color = Color.white;
        inputText.supportRichText = false;

        input = inputObj.AddComponent<InputField>();
        input.targetGraphic = inputBg;
        input.textComponent = inputText;
        input.lineType = InputField.LineType.SingleLine;
    }
}