using UnityEditor;
using UnityEngine;

// 신규 상점 SO 에셋 3종을 Resources/Shop/ 에 생성. 재실행 시 기존 에셋도 값 갱신(GUID 유지).
// 이름은 영어 — 폰트 아틀라스에 한글 글리프가 없어 UI는 영어로 통일(프로젝트 규칙).
public static class ShopAssetGenerator
{
    const string FolderPath = "Assets/Resources/Shop";

    [MenuItem("Tools/Shop/Create Shop Assets")]
    public static void CreateShopAssets()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets/Resources", "Shop");

        // 파일명은 PascalCase, id 는 snake_case — 기존 6종(Damage/damage, RewindCooldown/rewind_cooldown) 관례
        Create("Range",        "range",         "Range",         ShopEffectType.Range,        new[] { 110, 220, 440 },   0.06f);
        Create("RewindCharge", "rewind_charge", "Rewind Charge", ShopEffectType.RewindCharge, new[] { 1500, 3000 },      1f);
        Create("RewindGrace",  "rewind_grace",  "Rewind Grace",  ShopEffectType.RewindGrace,  new[] { 500, 1000, 2000 }, 0.6f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ShopAssetGenerator] Shop assets ready.");
    }

    static void Create(string fileName, string id, string displayName, ShopEffectType effect, int[] costs, float valuePerTier)
    {
        string path = $"{FolderPath}/{fileName}.asset";
        var so = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
        bool isNew = so == null;
        if (isNew) so = ScriptableObject.CreateInstance<ShopItemData>();

        so.id = id;
        so.displayName = displayName;
        so.effect = effect;
        so.costs = costs;
        so.valuePerTier = valuePerTier;

        if (isNew) AssetDatabase.CreateAsset(so, path);
        else EditorUtility.SetDirty(so);
    }
}
