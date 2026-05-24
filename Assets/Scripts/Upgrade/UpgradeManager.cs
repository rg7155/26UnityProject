using System.Collections.Generic;
using UnityEngine;

// 레벨업 이벤트 수신 → 랜덤 업그레이드 3개 선택 → UI에 전달
// Resources/Upgrades/ 폴더에서 UpgradeData 자동 로드
public class UpgradeManager : MonoBehaviour
{
    UpgradeData[] _allUpgrades;
    PlayerController _player;
    PlayerWeapon _weapon;

    static readonly HashSet<Define.UpgradeType> _oneTimeTypes = new HashSet<Define.UpgradeType>
    {
        Define.UpgradeType.UnlockBomb,
    };
    HashSet<Define.UpgradeType> _consumedOneTime = new HashSet<Define.UpgradeType>();

    public System.Action<UpgradeData[]> OnUpgradeChoiceReady;  // UI가 구독

    void Start()
    {
        _allUpgrades = Resources.LoadAll<UpgradeData>("Upgrades");
        if (_allUpgrades == null || _allUpgrades.Length == 0)
            Debug.LogError("[UpgradeManager] Resources/Upgrades/ 에 UpgradeData 에셋이 없습니다");

        _player = FindObjectOfType<PlayerController>();
        _weapon = FindObjectOfType<PlayerWeapon>();

        if (_player != null)
            _player.OnLevelUp += HandleLevelUp;
    }

    void OnDestroy()
    {
        if (_player != null)
            _player.OnLevelUp -= HandleLevelUp;
    }

    void HandleLevelUp(int newLevel)
    {
        Managers.Game.State = Define.GameState.Paused;  // 여기서 Pause
        UpgradeData[] choices = PickRandom(3);
        OnUpgradeChoiceReady?.Invoke(choices);
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        upgrade.Apply(_player, _weapon);

        if (_oneTimeTypes.Contains(upgrade.type))
            _consumedOneTime.Add(upgrade.type);

        Managers.Game.State = Define.GameState.Playing;  // 여기서 Resume
    }

    UpgradeData[] PickRandom(int count)
    {
        List<UpgradeData> pool = new List<UpgradeData>(_allUpgrades.Length);
        foreach (var up in _allUpgrades)
        {
            if (_consumedOneTime.Contains(up.type)) continue;
            pool.Add(up);
        }
        UpgradeData[] result = new UpgradeData[Mathf.Min(count, pool.Count)];

        for (int i = 0; i < result.Length; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result[i] = pool[idx];
            pool.RemoveAt(idx);
        }

        return result;
    }
}
