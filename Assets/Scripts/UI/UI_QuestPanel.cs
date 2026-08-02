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

        FitContainerWidth();

        foreach (QuestData quest in QuestService.Quests)
        {
            UI_QuestCell cell = Instantiate(_cellPrefab, _cellContainer);
            cell.Bind(quest, RefreshAll);
            _cells.Add(cell);
        }
        RefreshAll();
    }

    // 리스트 컨테이너 폭을 뷰포트 폭에 고정. 씬에 직렬화된 sizeDelta 가 어긋나 있으면
    // (에디터 생성기 실행 후 씬을 저장하지 않으면 기본값 100 이 그대로 남는다) 컨테이너가
    // 뷰포트보다 넓어져 셀이 좌우로 잘린다. 세로 크기는 ContentSizeFitter 담당이라 건드리지 않는다.
    void FitContainerWidth()
    {
        RectTransform rt = _cellContainer as RectTransform;
        if (rt == null) return;

        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
        rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
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
