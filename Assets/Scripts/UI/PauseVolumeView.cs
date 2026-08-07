using UnityEngine;
using UnityEngine.UI;

// Pause 오버레이 사운드 카드 — 자기등록. 슬라이더는 같은 프리팹 내부 자식이라 생성기가 Inspector 배선.
public class PauseVolumeView : MonoBehaviour
{
    [SerializeField] Slider _bgmSlider;
    [SerializeField] Slider _effectSlider;

    bool _wired;

    void Start()
    {
        // value 세팅이 먼저 — 순서를 뒤집으면 초기값 대입이 리스너를 발화시켜 세이브를 덮어쓴다
        if (_bgmSlider != null)
        {
            _bgmSlider.value = Managers.Game.BgmVolume;
            _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }

        if (_effectSlider != null)
        {
            _effectSlider.value = Managers.Game.EffectVolume;
            _effectSlider.onValueChanged.AddListener(OnEffectChanged);
        }

        _wired = true;
    }

    void OnBgmChanged(float value)
    {
        Managers.Game.BgmVolume = value;
        Managers.Sound.ApplyVolume();
    }

    void OnEffectChanged(float value)
    {
        Managers.Game.EffectVolume = value;
        Managers.Sound.ApplyVolume();
    }

    // 드래그마다 저장하면 초당 60회 IO — 패널이 닫힐 때 1회만.
    // PauseController.Start()가 패널을 끄는데 스크립트 실행 순서상 이 컴포넌트의 Start()보다 먼저일 수 있어 가드 필수
    void OnDisable()
    {
        if (_wired == false) return;
        Managers.Game.SaveGame();
    }

    void OnDestroy()
    {
        if (_bgmSlider != null) _bgmSlider.onValueChanged.RemoveAllListeners();
        if (_effectSlider != null) _effectSlider.onValueChanged.RemoveAllListeners();
    }
}
