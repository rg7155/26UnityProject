using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 레벨업 시 표시되는 업그레이드 선택 팝업
// UpgradeManager를 스스로 찾아 구독 — Inspector 연결 불필요
public class UI_UpgradePanel : MonoBehaviour
{
    [SerializeField] Button[] _buttons;         // 3개 버튼
    [SerializeField] TMP_Text[] _nameTexts;     // 각 버튼의 업그레이드 이름
    [SerializeField] TMP_Text[] _descTexts;     // 각 버튼의 설명

    // 무기 언락 카드의 강조 연출. 이 로직은 켜고 끄기만 하고 생김새는 전부 생성기가 소유한다
    [SerializeField] GameObject[] _weaponHighlights;

    UpgradeManager _upgradeManager;
    UpgradeData[] _currentChoices;

    void Start()
    {
        _upgradeManager = FindObjectOfType<UpgradeManager>();
        if (_upgradeManager == null)
        {
            Debug.LogError("[UI_UpgradePanel] UpgradeManager not found");
            return;
        }

        _upgradeManager.OnUpgradeChoiceReady += Show;

        for (int i = 0; i < _buttons.Length; i++)
        {
            int index = i;  // 클로저 캡처용
            _buttons[i].onClick.AddListener(() => OnClickCard(index));
        }

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_upgradeManager != null)
            _upgradeManager.OnUpgradeChoiceReady -= Show;
    }

    void Show(UpgradeData[] choices)
    {
        _currentChoices = choices;
        gameObject.SetActive(true);

        for (int i = 0; i < _buttons.Length; i++)
        {
            bool valid = i < choices.Length;
            _buttons[i].gameObject.SetActive(valid);

            if (!valid) continue;
            if (_nameTexts != null && i < _nameTexts.Length)
                _nameTexts[i].text = choices[i].upgradeName;
            if (_descTexts != null && i < _descTexts.Length)
                _descTexts[i].text = choices[i].description;

            if (_weaponHighlights != null && i < _weaponHighlights.Length && _weaponHighlights[i] != null)
                _weaponHighlights[i].SetActive(choices[i].type == Define.UpgradeType.AcquireWeapon);
        }
    }

    void OnClickCard(int index)
    {
        if (_currentChoices == null || index >= _currentChoices.Length) return;

        _upgradeManager.ApplyUpgrade(_currentChoices[index]);
        if (Managers.Game.State == Define.GameState.Playing)
            gameObject.SetActive(false);  // 큐 소진되어 Resume된 경우에만 닫기 (큐 남으면 Show가 패널 유지)
    }
}
