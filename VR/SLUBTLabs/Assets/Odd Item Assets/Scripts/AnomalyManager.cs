using System.Collections;
using UnityEngine;

/// <summary>
/// SLUBT Labs — Anomaly Manager
/// Added at runtime by AttentionalBlindnessManager to spawned distractor items.
/// Do NOT attach this manually — the manager adds it automatically after spawning.
/// </summary>
public class AnomalyManager : MonoBehaviour
{
    [Header("Blink Settings")]
    public float visibleDuration = 0.3f;
    public float invisibleDuration = 0.15f;
    public float randomStartDelay = 1.5f;

    private Renderer[] _renderers;
    private Coroutine _blinkCoroutine;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
    }

    public void StartBlinking()
    {
        if (_blinkCoroutine != null)
            StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlinking()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        SetVisible(true);
    }

    private IEnumerator BlinkRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, randomStartDelay));
        while (true)
        {
            SetVisible(true);
            yield return new WaitForSeconds(visibleDuration);
            SetVisible(false);
            yield return new WaitForSeconds(invisibleDuration);
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in _renderers)
            r.enabled = visible;
    }
}