using UnityEditor;
using UnityEngine;

// 퀘스트 SO 에셋 4종을 Resources/Quests/ 에 생성. 재실행 시 기존 에셋도 값 갱신(GUID 유지).
// 이름은 영어 — 폰트 아틀라스에 한글 글리프가 없어 UI는 영어로 통일(프로젝트 규칙).
public static class QuestAssetGenerator
{
    const string FolderPath = "Assets/Resources/Quests";

    [MenuItem("Tools/Quests/Create Quest Assets")]
    public static void CreateQuestAssets()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets/Resources", "Quests");

        Create("kills_total",   "SLAYER",         QuestStat.Kills,
               new[] { 100, 1000, 10000 }, new[] { 200, 1000, 5000 });
        Create("survive_best",  "SURVIVOR",       QuestStat.SurviveTime,
               new[] { 60, 180, 300 },     new[] { 200, 1000, 5000 });
        Create("rewind_total",  "TIME TRAVELER",  QuestStat.Rewinds,
               new[] { 10, 100, 500 },     new[] { 200, 1000, 5000 });
        Create("gold_total",    "COLLECTOR",      QuestStat.Gold,
               new[] { 1000, 10000, 50000 }, new[] { 300, 1500, 7000 });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[QuestAssetGenerator] Quest assets ready.");
    }

    static void Create(string id, string displayName, QuestStat stat, int[] targets, int[] rewards)
    {
        string path = $"{FolderPath}/{id}.asset";
        var so = AssetDatabase.LoadAssetAtPath<QuestData>(path);
        bool isNew = so == null;
        if (isNew) so = ScriptableObject.CreateInstance<QuestData>();

        so.id = id;
        so.displayName = displayName;
        so.stat = stat;
        so.targets = targets;
        so.rewards = rewards;

        if (isNew) AssetDatabase.CreateAsset(so, path);
        else EditorUtility.SetDirty(so);
    }
}
