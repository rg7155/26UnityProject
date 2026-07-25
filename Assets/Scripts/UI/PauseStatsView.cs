using UnityEngine;
using TMPro;

// Pause 오버레이 스텟 카드 — 자기등록. timeScale=0에서도 Update 실행됨.
public class PauseStatsView : MonoBehaviour
{
    [SerializeField] TMP_Text _damageText;
    [SerializeField] TMP_Text _fireRateText;
    [SerializeField] TMP_Text _moveSpeedText;
    [SerializeField] TMP_Text _maxHpText;
    [SerializeField] TMP_Text _rangeText;
    [SerializeField] TMP_Text _weaponCountText;

    PlayerController _player;
    WeaponManager _weapons;

    void Start()
    {
        _player = FindObjectOfType<PlayerController>();
        _weapons = FindObjectOfType<WeaponManager>();
    }

    void Update()
    {
        if (_weapons != null)
        {
            if (_damageText != null)      _damageText.text = $"{_weapons.DamageMult * 100f:0}%";
            if (_fireRateText != null)    _fireRateText.text = $"{_weapons.FireRateMult * 100f:0}%";
            if (_rangeText != null)       _rangeText.text = $"{_weapons.RangeMult * 100f:0}%";
            if (_weaponCountText != null) _weaponCountText.text = _weapons.WeaponCount.ToString();
        }

        if (_player != null)
        {
            if (_moveSpeedText != null) _moveSpeedText.text = $"{_player.Speed:0.0}";
            if (_maxHpText != null)     _maxHpText.text = _player.MaxHp.ToString();
        }
    }
}
