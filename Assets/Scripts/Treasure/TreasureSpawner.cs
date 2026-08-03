using System.Collections.Generic;
using UnityEngine;
using static Define;

// 보물상자 스폰 스케줄러 + 결과 롤 + 팝업 호출.
// 되감기 예외 규칙: 상자의 획득 여부는 되돌아가지 않는다. FrameSnapshot에 상자도 RunGold도 넣지 않으며,
// 스폰 타이머도 되돌리지 않는다 — 되돌리면 같은 상자가 두 번 스폰되는 경로가 생긴다.
public class TreasureSpawner : MonoBehaviour
{
    const float FirstSpawnTime = 40f;
    const float IntervalMin = 50f;
    const float IntervalMax = 70f;
    const float MinSpawnDist = 6f;
    const float MaxSpawnDist = 10f;
    const int MaxAlive = 2;

    GameObject _chestPrefab;
    List<TreasureChest> _alive = new List<TreasureChest>();

    Transform _player;
    WaveManager _wave;
    UI_TreasurePopup _popup;

    float _spawnTimer = FirstSpawnTime;

    void Start()
    {
        _chestPrefab = Resources.Load<GameObject>("Treasure/TreasureChest");
        if (_chestPrefab == null)
        {
            Debug.LogError("[TreasureSpawner] Resources/Treasure/TreasureChest 프리팹이 없습니다");
            return;
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) _player = player.transform;

        _wave = FindObjectOfType<WaveManager>();
        _popup = FindObjectOfType<UI_TreasurePopup>(true);
    }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing) return;
        if (_chestPrefab == null || _player == null) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;

        _spawnTimer = Random.Range(IntervalMin, IntervalMax);

        if (_alive.Count >= MaxAlive) return;
        if (_wave != null && _wave.BossWarningActive) return;

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(MinSpawnDist, MaxSpawnDist);
        SpawnAt(_player.position + (Vector3)offset);
    }

    // 보스 드롭·디버그 스폰용 — 동시 최대 개수를 무시한다. 보상이 조용히 사라지면 안 된다
    public void SpawnAt(Vector3 pos)
    {
        if (_chestPrefab == null) return;

        GameObject go = Instantiate(_chestPrefab, pos, Quaternion.identity);
        TreasureChest chest = go.GetComponent<TreasureChest>();
        if (chest == null)
        {
            Debug.LogError("[TreasureSpawner] 프리팹에 TreasureChest 컴포넌트가 없습니다");
            Destroy(go);
            return;
        }

        chest.Init(this, _player);
        _alive.Add(chest);
    }

    public void OnChestCollected(TreasureChest chest)
    {
        _alive.Remove(chest);

        Managers.Sound.PlayEffect(SoundManager.EnemyDeath);

        TreasureRollResult result = TreasureReward.Roll(_wave != null ? _wave.GameTime : 0f);

        if (_popup != null)
        {
            _popup.Open(result);
            return;
        }

        // 팝업이 씬에 없으면 보상이 증발하면 안 되므로 즉시 지급한다
        Managers.Game.RunGold += result.gold;
        Debug.LogWarning($"[TreasureSpawner] UI_TreasurePopup 없음 — 즉시 지급 slot={result.slotIndex} gold={result.gold}");
    }

    public void OnChestExpired(TreasureChest chest)
    {
        _alive.Remove(chest);
    }
}
