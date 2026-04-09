using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 플레이어를 스스로 찾아서 이벤트 구독 — Inspector 연결 불필요
public class HpBar : MonoBehaviour
{
    [SerializeField] Slider _slider;
    [SerializeField] TMP_Text _hpText;  // 없으면 무시

    void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[HpBar] Player not found");
            return;
        }

        UpdateHp(player.Hp, player.MaxHp);
        player.OnHpChanged += UpdateHp;
    }

    void UpdateHp(int current, int max)
    {
        if (_slider != null)
            _slider.value = (float)current / max;

        if (_hpText != null)
            _hpText.text = $"{current} / {max}";
    }
}
