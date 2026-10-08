using UnityEngine;

// Static facade over input: legacy Input class on desktop, touch-driven signals on
// mobile (set by TouchOverlay.cs). No Input System package exists in this project
// (Packages/manifest.json lists only URP + built-in modules) — adding one is a
// separate decision, not part of this pass, so this stays on legacy Input.
//
// Desktop behavior is UNCHANGED — every desktop branch below is the exact same
// Input.* call that used to sit directly in each caller, just centralized here so
// touch has one place to plug into instead of six.
public static class InputRouter
{
    // Set true by TouchOverlay.Awake() when running on a coarse-pointer device.
    // False (desktop/legacy behavior) until a TouchOverlay actually exists in the
    // scene, so this class is a no-op passthrough on any build that doesn't add one.
    public static bool IsTouch;

    // Continuous move axis. Desktop: WASD/arrows. Touch: set directly by the
    // on-screen stick each frame (TouchOverlay writes this, no edge-triggering
    // needed since it's a held/continuous value like a real analog stick).
    public static Vector2 TouchMoveAxis;

    // Bot / AI-test override (ComboBot, GreyspaceDebug). Null = normal behaviour.
    // Gameplay code never sets these; with both null, desktop and touch are unchanged.
    public static Vector2? SimMoveAxis;
    public static Vector3? SimAimPoint;

    public static Vector2 MoveAxis()
    {
        if (SimMoveAxis.HasValue) return SimMoveAxis.Value;
        if (IsTouch) return TouchMoveAxis;
        float h = 0, v = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h -= 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v -= 1;
        return new Vector2(h, v);
    }

    // Edge-triggered (GetKeyDown-style) signals. Touch buttons call Signal*() once
    // per tap; Consume() clears the flag on read so it behaves like GetKeyDown —
    // true for exactly one poll, not held.
    static bool _light, _heavy, _dash, _swap, _interact, _special, _retry;
    public static void SignalLight()     => _light = true;
    public static void SignalHeavy()     => _heavy = true;
    public static void SignalDash()      => _dash = true;
    public static void SignalSwap()      => _swap = true;
    public static void SignalInteract()  => _interact = true;
    public static void SignalSpecial()   => _special = true;
    public static void SignalRetry()     => _retry = true;

    static bool Consume(ref bool flag) { if (!flag) return false; flag = false; return true; }

    public static bool LightPressed()    => Consume(ref _light)    || (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0));
    public static bool HeavyPressed()    => Consume(ref _heavy)    || (Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1));
    public static bool DashPressed()     => Consume(ref _dash)     || (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space));
    public static bool SwapPressed()     => Consume(ref _swap)     || Input.GetKeyDown(KeyCode.Tab);
    public static bool InteractPressed() => Consume(ref _interact) || Input.GetKeyDown(KeyCode.E);
    public static bool SpecialPressed()  => Consume(ref _special)  || Input.GetKeyDown(KeyCode.F);
    public static bool RetryPressed()    => Consume(ref _retry)    || Input.GetKeyDown(KeyCode.R);

    // Aim: desktop = ground-plane raycast from the mouse (byte-identical to the old
    // inline code). Touch has no mouse and no equivalent yet — this is a genuine
    // design decision (right-stick look? nearest-enemy assist?) flagged here rather
    // than guessed silently, per the plan. Returns false on touch so callers keep
    // facing their last direction, same as the existing sqrMagnitude-skip guard
    // already does when the mouse ray doesn't move the aim point enough.
    public static bool TryAimWorldPoint(Camera cam, Vector3 planeOrigin, out Vector3 point)
    {
        if (SimAimPoint.HasValue) { point = SimAimPoint.Value; return true; }
        if (!IsTouch && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, planeOrigin);
            if (plane.Raycast(ray, out float dist))
            {
                point = ray.GetPoint(dist);
                return true;
            }
        }
        point = default;
        return false;
    }
}
