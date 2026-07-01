// No-op until AudioClip assets are wired. All call sites already exist across the engine;
// this stub just keeps them safe to call and gives a single place to plug clips in later.
public static class AudioManager
{
    public static void Play(string clipName)
    {
        // ponytail: no clips wired yet, add AudioSource + AudioClip lookup when assets exist
    }
}
