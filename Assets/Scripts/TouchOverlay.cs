using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Minimal on-screen touch controls: a drag-stick (bottom-left, movement) and a
// cluster of tap buttons (bottom-right, actions), built the same way the rest of
// this project's HUD is (runtime uGUI construction, no prefabs — see
// GreyspaceScene.BuildHUD() for the pattern this follows). Only becomes visible/
// active on a touch-capable device; a no-op on desktop.
//
// This is a functional scaffold, not a finished mobile UX pass — button layout,
// stick feel, and dead-zone tuning all still need real device playtesting.
public class TouchOverlay : MonoBehaviour
{
    const float StickRadius = 90f;
    RectTransform _stickBg, _stickKnob;
    int _stickTouchId = -1;
    Vector2 _stickOrigin;

    void Awake()
    {
        // Application.isMobilePlatform covers device builds; Input.touchSupported
        // also catches a touch-capable desktop/dev environment for testing.
        bool touch = Application.isMobilePlatform || Input.touchSupported;
        InputRouter.IsTouch = touch;
        if (!touch) { enabled = false; return; }

        BuildUI();
    }

    void BuildUI()
    {
        // EventTrigger needs an EventSystem to actually dispatch pointer/touch
        // callbacks — nothing in this project has ever needed one before now (the
        // existing HUD is display-only, no clickable elements), so one may not
        // exist. Without this, the buttons below would be visible but silently
        // inert — no error, just nothing happens on tap.
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject root = new GameObject("TouchOverlay");
        DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the regular HUD
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        // Movement stick — bottom-left.
        _stickBg = MakeCircle(root.transform, "StickBg", new Vector2(120, 120), StickRadius * 2,
            new Color(1, 1, 1, 0.15f));
        _stickKnob = MakeCircle(_stickBg.transform, "StickKnob", Vector2.zero, StickRadius,
            new Color(1, 1, 1, 0.35f));

        // Action buttons — bottom-right. Light/Heavy/Dash are the most-used, given
        // the biggest targets; Interact/Special/Swap/Retry are smaller secondary buttons.
        MakeButton(root.transform, "BtnLight",    new Vector2(-190, 150), 90, "LT", InputRouter.SignalLight);
        MakeButton(root.transform, "BtnHeavy",    new Vector2(-90,  90),  90, "HV", InputRouter.SignalHeavy);
        MakeButton(root.transform, "BtnDash",     new Vector2(-190, 260), 70, "DASH", InputRouter.SignalDash);
        MakeButton(root.transform, "BtnSpecial",  new Vector2(-90,  220), 70, "SPEC", InputRouter.SignalSpecial);
        MakeButton(root.transform, "BtnInteract", new Vector2(-300, 90),  60, "E", InputRouter.SignalInteract);
        MakeButton(root.transform, "BtnSwap",     new Vector2(-300, 200), 55, "TAB", InputRouter.SignalSwap);
        MakeButton(root.transform, "BtnRetry",    new Vector2(-300, 310), 55, "R", InputRouter.SignalRetry);
    }

    RectTransform MakeCircle(Transform parent, string name, Vector2 anchoredPos, float size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(size, size);
        return rt;
    }

    void MakeButton(Transform parent, string name, Vector2 anchoredPosFromRight, float size,
        string label, System.Action onTap)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.2f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0); // bottom-right anchored
        rt.anchoredPosition = anchoredPosFromRight;
        rt.sizeDelta = new Vector2(size, size);

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1, 1, 1, 0.8f);
        text.text = label;
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        EventTrigger trigger = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener((_) => onTap());
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        if (_stickBg == null) return;
        UpdateStick();
    }

    // Tracks one finger inside the stick background; ignores any touch already
    // claimed by a button (EventTrigger consumes those via the UI event system,
    // Input.GetTouch here is the raw touch list so we filter by position instead).
    void UpdateStick()
    {
        Vector2 bgScreenPos = RectTransformUtility.WorldToScreenPoint(null, _stickBg.position);

        if (_stickTouchId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (Vector2.Distance(t.position, bgScreenPos) <= StickRadius * 1.6f)
                {
                    _stickTouchId = t.fingerId;
                    _stickOrigin = t.position;
                    break;
                }
            }
        }

        if (_stickTouchId == -1)
        {
            InputRouter.TouchMoveAxis = Vector2.zero;
            _stickKnob.anchoredPosition = Vector2.zero;
            return;
        }

        bool stillDown = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId != _stickTouchId) continue;
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) break;
            stillDown = true;

            Vector2 delta = t.position - _stickOrigin;
            Vector2 clamped = Vector2.ClampMagnitude(delta, StickRadius);
            _stickKnob.anchoredPosition = clamped;
            InputRouter.TouchMoveAxis = clamped / StickRadius; // normalized -1..1 per axis
        }

        if (!stillDown)
        {
            _stickTouchId = -1;
            InputRouter.TouchMoveAxis = Vector2.zero;
            _stickKnob.anchoredPosition = Vector2.zero;
        }
    }
}
