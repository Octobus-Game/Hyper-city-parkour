using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Unity.AI.Navigation;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Debug console that supports:
///  - Built-in commands (help, clear, ping, spawn_cube, etc.)
///  - Reflection-based invocation of any public method on any MonoBehaviour in the scene
///  - NavMesh, Input System, Addressables, UnityWebRequest, UI, UIElements commands
///  - Autocomplete dropdown (fixed: no more value = -1 loop)
/// </summary>
public class DebugConsole : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector references
    // ─────────────────────────────────────────────────────────────────────────
    [Header("UI References")]
    public TMP_InputField inputField;
    public UnityEngine.UI.Button submitButton;          // fully-qualified to avoid ambiguity with UIElements
    public TextMeshProUGUI resultText;
    public TMP_Dropdown suggestionDropdown;

    [Header("Optional UIElements root (assign a UIDocument)")]
    public UIDocument uiDocument;                       // drag a UIDocument here if you want UIElements commands

    [Header("Optional NavMeshSurface (assign for bake command)")]
    public NavMeshSurface navMeshSurface;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────────
    private List<string> builtinCommands = new List<string>
    {
        "help",
        "clear",
        "spawn_cube",
        "spawn_navagent",
        "set_time_day",
        "set_time_night",
        "add_health",
        "ping",
        "navmesh_bake",
        "input_devices",
        "addressables_load",
        "webrequest",
        "ui_toggle",
        "uielements_info",
        "reflect_list",        // list all discoverable public methods
        "reflect_call",        // reflect_call <TypeName> <MethodName> [args...]
    };

    // Combined list used by autocomplete (builtins + reflected methods injected at runtime)
    private List<string> availableCommands = new List<string>();

    // Cache of discovered public methods: "TypeName.MethodName" -> MethodInfo
    private Dictionary<string, (MonoBehaviour target, MethodInfo method)> reflectedMethods
        = new Dictionary<string, (MonoBehaviour, MethodInfo)>();

    // Guard flag: prevents OnInputChanged from firing when WE set inputField.text
    private bool suppressInputEvent = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Discover all public methods on MonoBehaviours in the scene
        RefreshReflectedMethods();

        // Build the combined command list
        RebuildCommandList();

        // Wire up UI events
        inputField.onValueChanged.AddListener(OnInputChanged);
        inputField.onSubmit.AddListener(_ => ExecuteCommand());
        submitButton.onClick.AddListener(ExecuteCommand);
        suggestionDropdown.onValueChanged.AddListener(OnSuggestionSelected);

        suggestionDropdown.ClearOptions();
        HideDropdown();
        resultText.text = "";

        LogMessage("Console ready. Type <color=#88CCFF>help</color> for a command list.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Reflection: discover public methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans every MonoBehaviour in the scene for public, non-Unity-internal methods.
    /// Call this again at runtime if you spawn new objects.
    /// </summary>
    private void RefreshReflectedMethods()
    {
        reflectedMethods.Clear();

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        // Types we want to skip (Unity internals + this console itself)
        HashSet<Type> skipTypes = new HashSet<Type> { typeof(DebugConsole) };

        foreach (MonoBehaviour mb in allBehaviours)
        {
            Type type = mb.GetType();
            if (skipTypes.Contains(type)) continue;

            // Namespace guard: skip built-in Unity types
            string ns = type.Namespace ?? "";
            if (ns.StartsWith("UnityEngine") || ns.StartsWith("TMPro") || ns.StartsWith("Unity.")) continue;

            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (MethodInfo mi in methods)
            {
                // Skip property accessors, Unity messages, coroutine helpers
                if (mi.IsSpecialName) continue;
                if (IsUnityMessage(mi.Name)) continue;

                string key = $"{type.Name}.{mi.Name}";
                // Overloads: append index if the key already exists
                if (reflectedMethods.ContainsKey(key))
                    key = $"{key}_{mi.GetParameters().Length}";

                reflectedMethods[key] = (mb, mi);
            }
        }
    }

    private static readonly HashSet<string> unityMessages = new HashSet<string>
    {
        "Awake","Start","OnEnable","OnDisable","OnDestroy",
        "Update","FixedUpdate","LateUpdate",
        "OnTriggerEnter","OnTriggerExit","OnTriggerStay",
        "OnCollisionEnter","OnCollisionExit","OnCollisionStay",
        "OnApplicationQuit","OnApplicationPause","OnApplicationFocus",
        "OnGUI","OnDrawGizmos","OnDrawGizmosSelected",
        "OnValidate","Reset"
    };

    private bool IsUnityMessage(string name) => unityMessages.Contains(name);

    private void RebuildCommandList()
    {
        availableCommands = new List<string>(builtinCommands);
        foreach (string key in reflectedMethods.Keys)
            availableCommands.Add($"reflect_call {key}");
        availableCommands.Sort();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Autocomplete dropdown (FIXED)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnInputChanged(string input)
    {
        // If we triggered this change ourselves (e.g. autocomplete fill), ignore it
        if (suppressInputEvent) return;

        if (string.IsNullOrEmpty(input))
        {
            HideDropdown();
            return;
        }

        string lower = input.ToLower();
        var matches = availableCommands
            .Where(c => c.StartsWith(lower, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count > 0)
        {
            suggestionDropdown.gameObject.SetActive(true);
            suggestionDropdown.ClearOptions();
            suggestionDropdown.AddOptions(matches);

            // FIX: Do NOT set value = -1 (TMP_Dropdown clamps to 0, fires onValueChanged → loop).
            // Instead refresh the shown text and unsubscribe/re-subscribe around the reset.
            suggestionDropdown.onValueChanged.RemoveListener(OnSuggestionSelected);
            suggestionDropdown.value = 0;
            suggestionDropdown.RefreshShownValue();
            suggestionDropdown.onValueChanged.AddListener(OnSuggestionSelected);
        }
        else
        {
            HideDropdown();
        }
    }

    private void OnSuggestionSelected(int index)
    {
        if (index < 0 || index >= suggestionDropdown.options.Count) return;

        string chosen = suggestionDropdown.options[index].text;

        // Guard: setting inputField.text will fire OnInputChanged — suppress it
        suppressInputEvent = true;
        inputField.text = chosen;
        inputField.caretPosition = chosen.Length;
        suppressInputEvent = false;

        HideDropdown();
        inputField.ActivateInputField();
    }

    private void HideDropdown()
    {
        suggestionDropdown.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Command execution
    // ─────────────────────────────────────────────────────────────────────────

    private void ExecuteCommand()
    {
        string raw = inputField.text.Trim();
        if (string.IsNullOrEmpty(raw)) return;

        LogMessage($"<color=#AAFFAA>> {raw}</color>");

        string lower = raw.ToLower();
        string[] tokens = raw.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
        string cmd = tokens[0].ToLower();

        switch (cmd)
        {
            // ── Built-ins ────────────────────────────────────────────────────

            case "help":
                CmdHelp();
                break;

            case "clear":
                resultText.text = "";
                break;

            case "ping":
                LogMessage("pong!");
                break;

            case "spawn_cube":
                CmdSpawnCube();
                break;

            case "spawn_navagent":
                CmdSpawnNavAgent(tokens);
                break;

            case "set_time_day":
                RenderSettings.ambientLight = Color.white;
                LogMessage("Time set to day (ambient = white).");
                break;

            case "set_time_night":
                RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.1f);
                LogMessage("Time set to night (ambient = near-black).");
                break;

            case "add_health":
                int hp = tokens.Length > 1 && int.TryParse(tokens[1], out int v) ? v : 100;
                LogMessage($"Health +{hp} applied (mock).");
                break;

            // ── NavMesh ──────────────────────────────────────────────────────

            case "navmesh_bake":
                CmdNavMeshBake();
                break;

            // ── Input System ─────────────────────────────────────────────────

            case "input_devices":
                CmdInputDevices();
                break;

            // ── Addressables ─────────────────────────────────────────────────

            case "addressables_load":
                if (tokens.Length < 2)
                    LogMessage("<color=red>Usage: addressables_load <address></color>");
                else
                    StartCoroutine(CmdAddressablesLoad(tokens[1]));
                break;

            // ── UnityWebRequest ──────────────────────────────────────────────

            case "webrequest":
                string url = tokens.Length > 1 ? tokens[1] : "https://httpbin.org/get";
                StartCoroutine(CmdWebRequest(url));
                break;

            // ── UI ───────────────────────────────────────────────────────────

            case "ui_toggle":
                CmdUiToggle(tokens);
                break;

            // ── UIElements ───────────────────────────────────────────────────

            case "uielements_info":
                CmdUIElementsInfo();
                break;

            // ── Reflection ───────────────────────────────────────────────────

            case "reflect_list":
                CmdReflectList();
                break;

            case "reflect_refresh":
                RefreshReflectedMethods();
                RebuildCommandList();
                LogMessage($"Reflection refresh: {reflectedMethods.Count} methods found.");
                break;

            case "reflect_call":
                CmdReflectCall(tokens);
                break;

            default:
                LogMessage($"<color=red>Unknown command: {cmd}. Type 'help' for the list.</color>");
                break;
        }

        // Reset input
        suppressInputEvent = true;
        inputField.text = "";
        suppressInputEvent = false;
        inputField.ActivateInputField();
        HideDropdown();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Command implementations
    // ─────────────────────────────────────────────────────────────────────────

    private void CmdHelp()
    {
        LogMessage("──── Built-in commands ────");
        LogMessage(string.Join("\n", builtinCommands.Select(c => $"  {c}")));
        LogMessage($"──── Reflected public methods ({reflectedMethods.Count}) ────");
        LogMessage("  Use <color=#88CCFF>reflect_list</color> to see them, or autocomplete <color=#88CCFF>reflect_call</color>.");
        LogMessage("  Syntax: reflect_call TypeName.MethodName [arg1] [arg2] ...");
        LogMessage("  Use <color=#88CCFF>reflect_refresh</color> to re-scan the scene after spawning objects.");
    }

    private void CmdSpawnCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(
            UnityEngine.Random.Range(-3f, 3f), 0.5f, UnityEngine.Random.Range(-3f, 3f));
        LogMessage($"Cube spawned at {cube.transform.position}.");
    }

    private void CmdSpawnNavAgent(string[] tokens)
    {
#if UNITY_AI_NAVIGATION
        GameObject go = new GameObject("NavAgent_" + System.DateTime.Now.Ticks);
        go.transform.position = Vector3.zero;
        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        float speed = tokens.Length > 1 && float.TryParse(tokens[1], out float s) ? s : 3.5f;
        agent.speed = speed;
        LogMessage($"NavMeshAgent spawned. Speed = {speed}.");
#else
        LogMessage("<color=yellow>NavMeshAgent component added (AI.Navigation package required for full API).</color>");
        GameObject go = new GameObject("NavAgent");
        go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        LogMessage("NavMeshAgent spawned at origin.");
#endif
    }

    private void CmdNavMeshBake()
    {
        if (navMeshSurface == null)
        {
            // Try to find one in the scene
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
        }

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            LogMessage("NavMesh baked successfully.");
        }
        else
        {
            LogMessage("<color=red>No NavMeshSurface found. Add one to the scene or assign it in the Inspector.</color>");
        }
    }

    private void CmdInputDevices()
    {
        var devices = InputSystem.devices;
        if (devices.Count == 0)
        {
            LogMessage("No Input System devices connected.");
            return;
        }
        LogMessage($"Input devices ({devices.Count}):");
        foreach (var d in devices)
            LogMessage($"  [{d.deviceId}] {d.displayName}  layout:{d.layout}  enabled:{d.enabled}");
    }

    private IEnumerator CmdAddressablesLoad(string address)
    {
        LogMessage($"Addressables: loading '{address}'…");
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefab = handle.Result;
            Instantiate(prefab, Vector3.zero, Quaternion.identity);
            LogMessage($"Addressables: '{address}' loaded and instantiated.");
        }
        else
        {
            LogMessage($"<color=red>Addressables: failed to load '{address}'. " +
                       $"Error: {handle.OperationException?.Message}</color>");
        }
    }

    private IEnumerator CmdWebRequest(string url)
    {
        LogMessage($"GET {url} …");
        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            LogMessage($"<color=green>HTTP {req.responseCode}</color> — {req.downloadedBytes} bytes");
        else
            LogMessage($"<color=red>Request failed: {req.error}</color>");
    }

    private void CmdUiToggle(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            LogMessage("<color=red>Usage: ui_toggle <GameObject name></color>");
            return;
        }
        string goName = string.Join(" ", tokens, 1, tokens.Length - 1);
        GameObject target = GameObject.Find(goName);
        if (target == null)
        {
            LogMessage($"<color=red>GameObject '{goName}' not found.</color>");
            return;
        }
        bool newState = !target.activeSelf;
        target.SetActive(newState);
        LogMessage($"'{goName}' set active = {newState}.");
    }

    private void CmdUIElementsInfo()
    {
        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();

        if (uiDocument == null)
        {
            LogMessage("<color=yellow>No UIDocument found in scene.</color>");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            LogMessage("<color=yellow>UIDocument has no root VisualElement.</color>");
            return;
        }

        LogMessage($"UIElements root: '{root.name}'  childCount={root.childCount}  " +
                   $"layout={root.layout.width:F0}×{root.layout.height:F0}");

        // Print first-level children
        foreach (VisualElement child in root.Children())
            LogMessage($"  child: '{child.name}'  class={string.Join(",", child.GetClasses())}");
    }

    private void CmdReflectList()
    {
        if (reflectedMethods.Count == 0)
        {
            LogMessage("No public methods found. Try <color=#88CCFF>reflect_refresh</color>.");
            return;
        }
        LogMessage($"Reflected public methods ({reflectedMethods.Count}):");
        foreach (var kvp in reflectedMethods)
        {
            var parms = kvp.Value.method.GetParameters();
            string sig = string.Join(", ", parms.Select(p => $"{p.ParameterType.Name} {p.Name}"));
            LogMessage($"  reflect_call {kvp.Key}({sig})");
        }
    }

    /// <summary>
    /// Syntax: reflect_call TypeName.MethodName [arg1] [arg2] ...
    /// Arguments are coerced to the target parameter types automatically.
    /// </summary>
    private void CmdReflectCall(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            LogMessage("<color=red>Usage: reflect_call TypeName.MethodName [args...]</color>");
            return;
        }

        string key = tokens[1];
        if (!reflectedMethods.TryGetValue(key, out var entry))
        {
            LogMessage($"<color=red>Method '{key}' not found. Use reflect_list to see options.</color>");
            return;
        }

        ParameterInfo[] parms = entry.method.GetParameters();
        string[] rawArgs = tokens.Skip(2).ToArray();

        if (rawArgs.Length != parms.Length)
        {
            LogMessage($"<color=red>'{key}' expects {parms.Length} arg(s), got {rawArgs.Length}.</color>");
            return;
        }

        // Coerce string arguments to the correct parameter types
        object[] args = new object[parms.Length];
        try
        {
            for (int i = 0; i < parms.Length; i++)
                args[i] = Convert.ChangeType(rawArgs[i], parms[i].ParameterType);
        }
        catch (Exception ex)
        {
            LogMessage($"<color=red>Argument conversion error: {ex.Message}</color>");
            return;
        }

        // Invoke
        try
        {
            object result = entry.method.Invoke(entry.target, args);
            string resultStr = result == null ? "void" : result.ToString();
            LogMessage($"<color=green>{key}() → {resultStr}</color>");
        }
        catch (TargetInvocationException ex)
        {
            LogMessage($"<color=red>Exception in {key}(): {ex.InnerException?.Message ?? ex.Message}</color>");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Logging helper
    // ─────────────────────────────────────────────────────────────────────────

    private void LogMessage(string message)
    {
        resultText.text += message + "\n";
    }
}