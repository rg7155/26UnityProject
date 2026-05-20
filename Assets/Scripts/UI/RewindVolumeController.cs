using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;

public class RewindVolumeController : MonoBehaviour
{
    [SerializeField] float _fadeDuration = 0.3f;

    Volume _volume;
    Coroutine _fadeCoroutine;

    void Awake()
    {
        _volume = GetComponent<Volume>();
    }

    void Start()
    {
        _volume.weight = 0f;
        Managers.Game.OnStateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        Managers.Game.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        float target = (state == GameState.Rewinding) ? 1f : 0f;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeWeight(target));
    }

    IEnumerator FadeWeight(float target)
    {
        float start = _volume.weight;
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;  // Rewind 중 TimeScale 변경 대비
            _volume.weight = Mathf.Lerp(start, target, elapsed / _fadeDuration);
            yield return null;
        }
        _volume.weight = target;
        _fadeCoroutine = null;
    }
}
