using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 타이틀 상점 패널 — Resources/Shop/ 의 ShopItemData 에셋으로 셀을 동적 생성(데이터 주도).
// 항목 추가 = 에디터에서 SO 에셋 추가. 코드 변경 불필요.
// 자기등록 — Managers.Game / ShopService 직접 조회.
public class UI_ShopPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _goldText;
    [SerializeField] Button _closeButton;
    [SerializeField] UI_ShopItemCell _cellPrefab;
    [SerializeField] Transform _cellContainer;

    readonly List<UI_ShopItemCell> _cells = new List<UI_ShopItemCell>();

    void Start()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        foreach (ShopItemData item in ShopService.Items)
        {
            UI_ShopItemCell cell = Instantiate(_cellPrefab, _cellContainer);
            cell.Bind(item, RefreshAll);
            _cells.Add(cell);
        }
        RefreshAll();
    }

    void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
    }

    // 구매로 골드가 바뀌면 전 항목 구매가능 여부가 달라지므로 전체 갱신
    void RefreshAll()
    {
        if (_goldText != null) _goldText.text = $"Gold: {Managers.Game.TotalGold}";
        foreach (var cell in _cells) cell.Refresh();
    }
}
