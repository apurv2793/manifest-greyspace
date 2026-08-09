// Single source of truth for the on-screen controls list (GreyspaceScene HUD, top-right panel).
// Append one line here whenever a new binding is added — HUD picks it up automatically.
public static class ControlsMap
{
    static readonly string[] Desktop =
    {
        "WASD / Arrows — Move",
        "Mouse — Aim",
        "J / LMB — Light attack",
        "K / RMB — Heavy attack",
        "Shift / Space — Dash",
        "Tab — Switch weapon",
        "E — Interact (pickup / portal)",
        "R — Retry / Replay",
        "F — Special attack",
    };

    static readonly string[] Touch =
    {
        "Left stick — Move",
        "LT / HV buttons — Light / Heavy attack",
        "DASH — Dash",
        "SPEC — Special attack",
        "E — Interact (pickup / portal)",
        "TAB — Switch weapon",
        "R — Retry / Replay",
    };

    // Read at access time, not construction time — InputRouter.IsTouch is set by
    // TouchOverlay.Awake() during scene Start(), before this is first read.
    public static string[] Bindings => InputRouter.IsTouch ? Touch : Desktop;
}
