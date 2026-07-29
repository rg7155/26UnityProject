using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 이벤트 구독이 아니라 폴링 — 보스는 되감기 시 풀에서 부활할 수 있어(Case B2)
// 구독 해제/재구독 타이밍이 생긴다. 값 2개 폴링이 그 위험보다 훨씬 싸다.
// _root에는 이 오브젝트 자신이 아니라 자식을 연결해야 한다 — 자신을 끄면 Update가 멈춰 다시 켜지지 않는다
public class BossHpBar : MonoBehaviour
{
    [SerializeField] GameObject _root;
    [SerializeField] Slider _slider;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _hpText;

    void Update()
    {
        BossController boss = BossController.Instance;

        if (_root != null)
            _root.SetActive(boss != null);

        if (boss == null) return;

        int max = boss.MaxHp;

        if (_slider != null)
            _slider.value = (float)boss.Hp / max;

        if (_nameText != null)
            _nameText.text = boss.BossName;

        if (_hpText != null)
            _hpText.text = $"{boss.Hp} / {max}";
    }
}
