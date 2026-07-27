using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 퀘스트 셀 프리팹 — QuestData 하나를 표시하고 수령 버튼을 처리. UI_QuestPanel 이 항목 수만큼 생성.
// UI_ShopItemCell 과 동형 패턴. 진행바는 Current vs NextTarget, 3상태(수령가능/진행중/MAX)는 버튼 라벨로 표시.
public class UI_QuestCell : MonoBehaviour
{
    [SerializeField] TMP_Text _nameLabel;
    [SerializeField] Image _progressFill;
    [SerializeField] TMP_Text _progressLabel;
    [SerializeField] TMP_Text _rewardLabel;
    [SerializeField] Button _claimButton;
    [SerializeField] TMP_Text _claimLabel;

    QuestData _quest;
    Action _onClaimed;

    public void Bind(QuestData quest, Action onClaimed)
    {
        _quest = quest;
        _onClaimed = onClaimed;
        if (_claimButton != null)
        {
            _claimButton.onClick.RemoveAllListeners();
            _claimButton.onClick.AddListener(OnClick);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (_quest == null) return;

        bool maxed = QuestService.IsMaxed(_quest);
        bool claimable = QuestService.Claimable(_quest);
        int current = QuestService.Current(_quest);
        int reachedTier = QuestService.ReachedTier(_quest);
        int nextTarget = QuestService.NextTarget(_quest);

        if (_nameLabel != null) _nameLabel.text = _quest.displayName;

        if (_progressLabel != null)
            _progressLabel.text = maxed ? $"{current} / MAX" : $"{current} / {nextTarget}";

        if (_progressFill != null)
            _progressFill.fillAmount = maxed ? 1f : Mathf.Clamp01(nextTarget <= 0 ? 0f : (float)current / nextTarget);

        if (_rewardLabel != null)
            _rewardLabel.text = maxed ? "" : $"{QuestService.RewardOf(_quest, reachedTier)}G";

        if (_claimLabel != null)
            _claimLabel.text = maxed ? "MAX" : claimable ? "CLAIM" : "LOCKED";

        if (_claimButton != null) _claimButton.interactable = claimable;
    }

    void OnClick()
    {
        if (QuestService.Claim(_quest)) _onClaimed?.Invoke();
    }

    void OnDestroy()
    {
        if (_claimButton != null) _claimButton.onClick.RemoveAllListeners();
    }
}
