using UnityEngine;

public enum QuestStat { Kills, SurviveTime, Rewinds, Gold, BossKills }

// 메타 누적 도전과제 정의 — 에셋을 Resources/Quests/ 에 추가하면 자동 등장.
// QuestService가 LoadAll로 읽는다. targets/rewards 길이가 티어 수 (동일 길이).
[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    public string id;                 // 저장키 — 수령목록 조회에 사용. 에셋마다 고유
    public string displayName;
    public QuestStat stat;

    public int[] targets;             // 각 티어 목표치 (오름차순)
    public int[] rewards;             // 각 티어 수령 보상 골드
}
