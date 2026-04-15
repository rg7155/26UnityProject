using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 플레이어를 스스로 찾아서 이벤트 구독 — Inspector 연결 불필요
public class ExpBar : MonoBehaviour
{
    [SerializeField] Slider _slider;
    [SerializeField] TMP_Text _levelText;  // 없으면 무시

    void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[ExpBar] Player not found");
            return;
        }

        UpdateExp(player.Exp, player.ExpToNextLevel);
        UpdateLevel(player.Level);

        player.OnExpChanged += UpdateExp;
        player.OnLevelUp   += (newLevel) => UpdateLevel(newLevel);
    }

    void UpdateExp(int current, int expToNext)
    {
        if (_slider != null)
            _slider.value = (float)current / expToNext;
    }

    void UpdateLevel(int level)
    {
        if (_levelText != null)
            _levelText.text = $"Lv.{level}";
    }
}
