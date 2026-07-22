using UnityEngine;
using TMPro;

// 타이틀 로비 상단 재화바의 골드칩. 자기등록: Start 에서 TotalGold 를 읽어 표시.
// 타이틀에서 골드는 변하지 않으므로(구매는 ShopPanel 이 자체 갱신) 실시간 갱신 없음.
public class TitleGoldDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text _goldText;

    void Start()
    {
        if (_goldText != null)
            _goldText.text = Managers.Game.TotalGold.ToString();
    }
}
