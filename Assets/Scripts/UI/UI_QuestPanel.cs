using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 타이틀 퀘스트 패널 — Resources/Quests/ 의 QuestData 에셋으로 셀을 동적 생성(데이터 주도).
// 항목 추가 = 에디터에서 SO 에셋 추가. 코드 변경 불필요.
// 자기등록 — QuestService 직접 조회. UI_ShopPanel 과 동형 패턴.
public class UI_QuestPanel : MonoBehaviour
{
    [SerializeField] Button _closeButton;
    [SerializeField] UI_QuestCell _cellPrefab;
    [SerializeField] Transform _cellContainer;

    readonly List<UI_QuestCell> _cells = new List<UI_QuestCell>();

    void Start()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        foreach (QuestData quest in QuestService.Quests)
        {
            UI_QuestCell cell = Instantiate(_cellPrefab, _cellContainer);
            cell.Bind(quest, RefreshAll);
            _cells.Add(cell);
        }
        RefreshAll();
    }

    void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
    }

    // 수령으로 claimedTier 가 바뀌어도 다른 항목엔 영향 없지만 상점과 동일하게 전체 갱신
    void RefreshAll()
    {
        foreach (var cell in _cells) cell.Refresh();
    }
}
