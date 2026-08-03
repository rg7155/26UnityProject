using UnityEngine;
using UnityEngine.UI;

// 피격 순간 화면 테두리를 붉게 번쩍이고 서서히 사라진다.
// 순수 연출이라 unscaledDeltaTime 을 쓴다 — 레벨업 정지·되감기 중에도 어색하게 멈추지 않고
// 되감기 스냅샷 재현 대상도 아니다(UIBlink 와 같은 판단).
// 스프라이트(붉은 9-slice 테두리)는 UIProceduralSprite 가 채우고, 이 컴포넌트는 알파만 소유한다.
[RequireComponent(typeof(Image))]
public class HitVignette : MonoBehaviour
{
    [SerializeField] float _peakAlpha = 0.75f;
    [SerializeField] float _holdDuration = 0.05f;
    [SerializeField] float _fadeDuration = 0.35f;

    Image _image;
    PlayerController _player;
    float _timer;

    void Start()
    {
        _image = GetComponent<Image>();
        SetAlpha(0f);

        _player = FindObjectOfType<PlayerController>();
        if (_player == null)
        {
            Debug.LogError("[HitVignette] PlayerController not found");
            return;
        }
        _player.OnHit += HandleHit;
    }

    void OnDestroy()
    {
        if (_player != null)
            _player.OnHit -= HandleHit;
    }

    void Update()
    {
        if (_timer <= 0f) return;

        _timer -= Time.unscaledDeltaTime;
        float fade = Mathf.Clamp01(_timer / _fadeDuration); // hold 구간은 1 로 포화 → 잠깐 머물다 페이드
        SetAlpha(_peakAlpha * fade);
    }

    void HandleHit()
    {
        _timer = _holdDuration + _fadeDuration;  // 연타 피격이면 그냥 다시 최대치에서 시작
        SetAlpha(_peakAlpha);
    }

    void SetAlpha(float a)
    {
        Color c = _image.color;
        c.a = a;
        _image.color = c;
    }
}
