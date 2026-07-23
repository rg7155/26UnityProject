using UnityEngine;
using UnityEngine.UI;

// 우하단 되감기 버튼 — 쿨다운 링 + Auto 차지 pip 갱신. 자기등록, Start에서 RewindManager 캐시
public class RewindButtonUI : MonoBehaviour
{
    [SerializeField] Button _button;         // onClick 배선
    [SerializeField] Image _cooldownRing;    // type=Filled, Radial360 — 쿨다운 소진될수록 채워짐
    [SerializeField] Image[] _autoPips;      // Auto 차지 표시

    RewindManager _rewind;

    void Start()
    {
        _rewind = FindObjectOfType<RewindManager>();
        if (_rewind == null)
        {
            Debug.LogError("[RewindButtonUI] RewindManager not found");
            return;
        }

        if (_button != null)
            _button.onClick.AddListener(_rewind.TryActiveRewind);
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (_rewind == null) return;

        if (_cooldownRing != null)
        {
            _cooldownRing.fillAmount = _rewind.CooldownMax > 0f
                ? 1f - Mathf.Clamp01(_rewind.CooldownRemaining / _rewind.CooldownMax)
                : 1f;
        }

        if (_button != null)
            _button.interactable = _rewind.CanActiveRewind;

        if (_autoPips != null)
        {
            for (int i = 0; i < _autoPips.Length; i++)
            {
                if (_autoPips[i] != null)
                    _autoPips[i].enabled = i < _rewind.AutoRewindCharges;
            }
        }
    }
}
