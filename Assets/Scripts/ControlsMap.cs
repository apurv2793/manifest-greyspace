// Single source of truth for the on-screen controls list (GreyspaceScene HUD, top-right panel).
// Append one line here whenever a new binding is added — HUD picks it up automatically.
public static class ControlsMap
{
    public static readonly string[] Bindings =
    {
        "WASD / Arrows — Move",
        "Mouse — Aim",
        "J / LMB — Light attack",
        "K / RMB — Heavy attack",
        "Shift / Space — Dash",
        "Tab — Switch weapon",
        "E — Interact (pickup / portal)",
        "R — Retry / Replay",
    };
}
