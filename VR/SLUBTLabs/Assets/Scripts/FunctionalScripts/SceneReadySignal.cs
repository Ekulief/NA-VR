/// <summary>
/// Experiment scenes call MarkReady() once they finish setting up.
/// ExperimentLoader waits for this (with a timeout) before revealing the scene.
/// </summary>
public static class SceneReadySignal
{
    public static bool IsReady { get; private set; }

    public static void Reset() => IsReady = false;
    public static void MarkReady() => IsReady = true;
}