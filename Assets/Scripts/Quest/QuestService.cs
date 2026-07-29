using UnityEngine;

// 메타 도전과제의 단일 접근점 — 에셋 로드 · 누적 진행도 조회 · 수동 수령 처리.
// 에셋은 Resources/Quests/ 의 QuestData 에서 로드 (하드코딩 없음). ShopService와 동형.
public static class QuestService
{
    static QuestData[] _quests;
    public static QuestData[] Quests
    {
        get
        {
            if (_quests == null) _quests = Resources.LoadAll<QuestData>("Quests");
            return _quests;
        }
    }

    static GameData D => Managers.Game.SaveData;

    // stat별 평생 누적값 — 생존시간은 최고기록(BestTime) 사용
    public static int Current(QuestData quest)
    {
        switch (quest.stat)
        {
            case QuestStat.Kills:       return D.LifetimeKills;
            case QuestStat.Rewinds:     return D.LifetimeRewinds;
            case QuestStat.Gold:        return D.LifetimeGold;
            case QuestStat.BossKills:   return D.LifetimeBossKills;
            case QuestStat.SurviveTime: return Mathf.RoundToInt(D.BestTime);
            default:                    return 0;
        }
    }

    // 목표를 넘긴 티어 수
    public static int ReachedTier(QuestData quest)
    {
        int cur = Current(quest);
        int reached = 0;
        for (int i = 0; i < quest.targets.Length; i++)
            if (cur >= quest.targets[i]) reached++;
        return reached;
    }

    public static int ClaimedTier(string id)
    {
        foreach (var p in D.QuestClaims)
            if (p.id == id) return p.claimedTier;
        return 0;
    }

    public static bool Claimable(QuestData quest) => ReachedTier(quest) > ClaimedTier(quest.id);

    public static bool IsMaxed(QuestData quest) => ClaimedTier(quest.id) >= quest.targets.Length;

    // 현재 조준 중인 티어 목표치 — maxed면 -1
    public static int NextTarget(QuestData quest)
    {
        int reached = ReachedTier(quest);
        return reached >= quest.targets.Length ? -1 : quest.targets[reached];
    }

    // 특정 티어 보상 골드
    public static int RewardOf(QuestData quest, int tier)
    {
        if (tier < 0 || tier >= quest.rewards.Length) return 0;
        return quest.rewards[tier];
    }

    // 미수령 도달분 보상만 합산 지급 후 claimedTier 갱신 · 저장
    public static bool Claim(QuestData quest)
    {
        if (!Claimable(quest)) return false;

        int claimed = ClaimedTier(quest.id);
        int reached = ReachedTier(quest);
        int reward = 0;
        for (int tier = claimed; tier < reached; tier++)
            reward += RewardOf(quest, tier);

        Managers.Game.AddGold(reward);
        SetClaimedTier(quest.id, reached);
        Managers.Game.SaveGame();
        return true;
    }

    static void SetClaimedTier(string id, int tier)
    {
        foreach (var p in D.QuestClaims)
            if (p.id == id) { p.claimedTier = tier; return; }
        D.QuestClaims.Add(new QuestProgress { id = id, claimedTier = tier });
    }
}
