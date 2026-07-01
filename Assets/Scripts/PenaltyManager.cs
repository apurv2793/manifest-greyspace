// Empty subscriber on player death — design TBD with story. Wire real penalty logic here later.
// Instantiate once (e.g. `new PenaltyManager()` in GreyspaceScene.Start) to activate the subscription.
public class PenaltyManager
{
    public PenaltyManager()
    {
        GameEvents.OnPlayerDeath += OnPlayerDeath;
    }

    void OnPlayerDeath()
    {
        // ponytail: no penalty behaviour yet, design deferred to story pass
    }
}
