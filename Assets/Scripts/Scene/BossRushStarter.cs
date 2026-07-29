using UnityEngine;

// BOSS RUSH 데모 모드 — 3분 생존 없이 보스전을 즉시 보여준다 (심사·면접·GIF 촬영용).
// [DefaultExecutionOrder(100)] 인 이유: WeaponManager.Start가 시작 무기와 메타 상점 배율을 적용한 "뒤"에
// 프리셋을 얹어야 한다. GameScene.Start에 끼워 넣으면 두 Start의 순서가 미보장이라 회피했다.
[DefaultExecutionOrder(100)]
public class BossRushStarter : MonoBehaviour
{
    [SerializeField] WeaponData[] _presetWeapons;
    [SerializeField] float _damageMult = 2f;
    [SerializeField] float _fireRateMult = 1.5f;
    [SerializeField] int _bonusMaxHp = 100;

    // 3:00 보스 4초 전. Inspector에서 356으로 바꾸면 그대로 SWARMLORD 데모가 된다
    [SerializeField] float _startTime = 176f;

    void Start()
    {
        if (!GameScene.BossRush)
        {
            enabled = false;
            return;
        }

        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null && _presetWeapons != null)
        {
            foreach (WeaponData data in _presetWeapons)
            {
                if (data == null || weaponManager.HasWeapon(data)) continue;
                weaponManager.AddWeapon(data);
            }

            weaponManager.UpgradeDamage(_damageMult);
            weaponManager.UpgradeFireRate(_fireRateMult);
        }

        FindObjectOfType<PlayerController>()?.UpgradeMaxHp(_bonusMaxHp);
        FindObjectOfType<WaveManager>()?.JumpTo(_startTime);
    }
}
