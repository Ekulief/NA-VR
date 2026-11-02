using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Background music for the simulator, with tempo (BPM) conditions and an
/// emergency mute button.
///
/// TEMPO CONDITION
///   Add one clip per tempo to "Tempo Tracks". The instructor picks a tempo in the
///   experiment config (Firestore field music_TempoBpm); the closest track is played.
///   If the field is missing or 0, the Hub Playlist / Default Tempo is used instead.
///
/// OPTIONAL CONFIG FIELDS (read automatically if you add them to ExperimentConfig):
///   int  music_TempoBpm              0 = use default
///   bool music_PlayDuringExperiments true = keep the music on during experiments
///   bool music_Enabled               false = no music at all
///
/// EMERGENCY MUTE
///   Assign "Mute Action" (an Input Action bound to a controller button).
///   Hold it for "Mute Hold Seconds" to silence the music. Hold again to bring it back.
///
/// SETUP
///   1. Put this on an empty GameObject in Main_VR_Scene (an AudioSource is added).
///   2. Do NOT add it to ExperimentLoader's "Main Scene Objects" list.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Serializable]
    public class TempoTrack
    {
        public int bpm = 100;
        public AudioClip clip;
    }

    [Serializable]
    public class SceneMusic
    {
        [Tooltip("Exact scene name, e.g. 'Odd Item Scene'.")]
        public string sceneName;
        [Tooltip("Music for this experiment. Leave empty for silence.")]
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.2f;
    }

    public enum MuteMode
    {
        [Tooltip("Hold the button to mute. It stays muted until held again.")]
        HoldToToggle,
        [Tooltip("Music is silent only while the button is held down.")]
        MuteWhileHeld
    }

    [Header("Tempo Tracks (one clip per BPM)")]
    public List<TempoTrack> tempoTracks = new();
    [Tooltip("Used when the config has no tempo set.")]
    public int defaultTempoBpm = 80;

    [Header("Hub Music")]
    [Tooltip("Optional generic playlist. Used only if the config sets no tempo.")]
    public List<AudioClip> hubPlaylist = new();
    [Range(0f, 1f)] public float hubVolume = 0.3f;
    public bool shuffle = false;
    public bool playOnStart = true;
    [Tooltip("How long to wait for the experiment config at startup before playing the default.")]
    public float configWaitSeconds = 10f;

    [Header("Fading")]
    public float fadeSeconds = 2f;

    [Header("During Experiments")]
    [Tooltip("Off (recommended): music fades out while an experiment runs. The config can override this.")]
    public bool playDuringExperiments = false;
    [Range(0f, 1f)] public float experimentVolume = 0.15f;
    [Tooltip("Optional: give a specific experiment its own music (or force silence).")]
    public List<SceneMusic> sceneOverrides = new();

    [Header("Emergency Mute")]
    [Tooltip("Input action for the mute button (e.g. the B or Y button on either controller).")]
    public InputActionReference muteAction;
    public MuteMode muteMode = MuteMode.HoldToToggle;
    public float muteHoldSeconds = 1.5f;
    [Tooltip("Mute fades out this fast (seconds) so it feels instant.")]
    public float muteFadeSeconds = 0.25f;

    public bool IsMuted => _muted;

    private AudioSource _src;
    private Coroutine _routine;

    private bool _muted;
    private bool _musicEnabled = true;
    private int _cfgTempo;
    private bool _playDuringEffective;

    private List<AudioClip> _lastClips;
    private float _lastVolume;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;
        _src.spatialBlend = 0f;      // 2D: not positioned in the room
        _src.volume = 0f;
        _playDuringEffective = playDuringExperiments;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private IEnumerator Start()
    {
        StartCoroutine(MuteButtonRoutine());

        if (!playOnStart) yield break;

        // Wait briefly for the config so the music starts at the right tempo
        float waited = 0f;
        while ((!ExperimentConfigLoader.IsReady || ExperimentConfigLoader.Current == null) &&
               waited < configWaitSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        ReadConfigSettings();
        PlayHubMusic();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlayHubMusic() => Play(ResolveMusic(), hubVolume);
    public void FadeOut() => Play(null, 0f);

    /// <summary>Mutes or unmutes the music (also used by the emergency button).</summary>
    public void SetMuted(bool muted, string source = "api")
    {
        if (_muted == muted) return;
        _muted = muted;

        LogSafe(muted ? "music_muted" : "music_unmuted", $"Music {(muted ? "muted" : "unmuted")} via {source}");

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(muted ? MuteRoutine() : PlayRoutine(_lastClips, _lastVolume));
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private void ReadConfigSettings()
    {
        _cfgTempo = TryReadConfig("music_TempoBpm", out int bpm) ? bpm : 0;
        _musicEnabled = !TryReadConfig("music_Enabled", out bool enabledFlag) || enabledFlag;
        _playDuringEffective = TryReadConfig("music_PlayDuringExperiments", out bool during)
            ? during
            : playDuringExperiments;
    }

    // Reads an optional field from ExperimentConfig without a compile-time dependency,
    // so this script works before you add the fields.
    private static bool TryReadConfig<T>(string name, out T value)
    {
        value = default;
        ExperimentConfig cfg = ExperimentConfigLoader.Current;
        if (cfg == null) return false;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        object raw = null;

        FieldInfo field = cfg.GetType().GetField(name, flags);
        if (field != null) raw = field.GetValue(cfg);
        else
        {
            PropertyInfo prop = cfg.GetType().GetProperty(name, flags);
            if (prop != null) raw = prop.GetValue(cfg);
        }

        if (raw is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }

    // ── Choosing what to play ─────────────────────────────────────────────────

    private List<AudioClip> ResolveMusic()
    {
        if (_cfgTempo > 0)
        {
            AudioClip c = NearestTempoClip(_cfgTempo);
            if (c != null) return new List<AudioClip> { c };
        }

        if (hubPlaylist != null && hubPlaylist.Exists(c => c != null))
            return hubPlaylist;

        AudioClip fallback = NearestTempoClip(defaultTempoBpm);
        return fallback != null ? new List<AudioClip> { fallback } : null;
    }

    private AudioClip NearestTempoClip(int bpm)
    {
        AudioClip best = null;
        int bestDiff = int.MaxValue;

        foreach (TempoTrack t in tempoTracks)
        {
            if (t == null || t.clip == null) continue;
            int diff = Mathf.Abs(t.bpm - bpm);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = t.clip;
            }
        }
        return best;
    }

    // ── Scene events ──────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only react to experiment scenes loaded on top of the hub
        if (mode != LoadSceneMode.Additive || scene == gameObject.scene) return;

        ReadConfigSettings();

        SceneMusic match = sceneOverrides.Find(s => s != null && s.sceneName == scene.name);
        bool playing;

        if (match != null)
        {
            playing = match.clip != null;
            Play(playing ? new List<AudioClip> { match.clip } : null, playing ? match.volume : 0f);
        }
        else if (_playDuringEffective)
        {
            playing = true;
            Play(ResolveMusic(), experimentVolume);
        }
        else
        {
            playing = false;
            Play(null, 0f);
        }

        // Record the condition so results can be compared by tempo
        LogSafe("music_condition", $"Music during '{scene.name}': playing={playing}, tempo={_cfgTempo}",
            new Dictionary<string, object>
            {
                { "scene", scene.name },
                { "playing", playing && _musicEnabled && !_muted },
                { "tempoBpm", _cfgTempo },
                { "muted", _muted }
            });
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene == gameObject.scene) return;
        ReadConfigSettings();
        Play(ResolveMusic(), hubVolume);
    }

    // ── Emergency mute button ─────────────────────────────────────────────────

    private IEnumerator MuteButtonRoutine()
    {
        if (muteAction == null || muteAction.action == null) yield break;
        if (!muteAction.action.enabled) muteAction.action.Enable();

        float held = 0f;
        bool latched = false;
        bool wasDown = false;

        while (true)
        {
            bool down = muteAction.action.IsPressed();

            if (muteMode == MuteMode.MuteWhileHeld)
            {
                if (down != wasDown) SetMuted(down, "button");
                wasDown = down;
            }
            else
            {
                held = down ? held + Mathf.Min(Time.unscaledDeltaTime, 0.1f) : 0f;

                if (held >= muteHoldSeconds && !latched)
                {
                    SetMuted(!_muted, "button");
                    latched = true;      // one toggle per hold
                }
                if (!down) latched = false;
            }

            yield return null;
        }
    }

    // ── Playback ──────────────────────────────────────────────────────────────

    private void Play(List<AudioClip> clips, float volume)
    {
        // Remember the request so it can resume after an unmute
        _lastClips = clips;
        _lastVolume = volume;

        if (_muted || !_musicEnabled) return;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(clips, volume));
    }

    private IEnumerator MuteRoutine()
    {
        yield return FadeTo(0f, muteFadeSeconds);
        _src.Stop();
    }

    private IEnumerator PlayRoutine(List<AudioClip> clips, float volume)
    {
        // Fade out whatever is playing now
        if (_src.isPlaying)
        {
            yield return FadeTo(0f, fadeSeconds);
            _src.Stop();
        }

        var valid = new List<AudioClip>();
        if (clips != null)
            foreach (AudioClip c in clips)
                if (c != null) valid.Add(c);

        if (valid.Count == 0 || volume <= 0f) yield break;

        int index = shuffle ? UnityEngine.Random.Range(0, valid.Count) : 0;

        while (true)
        {
            AudioClip clip = valid[index];
            _src.clip = clip;
            _src.loop = valid.Count == 1;   // a single track simply loops
            _src.volume = 0f;
            _src.Play();

            yield return FadeTo(volume, fadeSeconds);

            if (valid.Count == 1) yield break;

            // Wait until the track is about to end, then fade out and move on
            while (_src.isPlaying && _src.time < clip.length - fadeSeconds)
                yield return null;

            yield return FadeTo(0f, fadeSeconds);
            _src.Stop();

            if (shuffle)
            {
                int next = index;
                for (int tries = 0; tries < 5 && next == index; tries++)
                    next = UnityEngine.Random.Range(0, valid.Count);
                index = next;
            }
            else
            {
                index = (index + 1) % valid.Count;
            }
        }
    }

    private IEnumerator FadeTo(float target, float seconds)
    {
        float start = _src.volume;
        float t = 0f;
        float duration = Mathf.Max(0.01f, seconds);

        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);   // a loading hitch can't skip the fade
            _src.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        _src.volume = target;
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private static void LogSafe(string type, string message, Dictionary<string, object> data = null)
    {
        try
        {
            if (data == null) ExperimentLogger.Log(type, message);
            else ExperimentLogger.Log(type, message, data);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BackgroundMusic] Could not log '{type}': {ex.Message}");
        }
    }
}