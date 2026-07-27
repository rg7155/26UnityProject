using UnityEngine;

public enum ShopEffectType { StartHp, Damage, RewindCooldown, RewindDuration, FireRate, MoveSpeed }

// 상점 항목 정의 — 에디터에서 에셋을 추가하면 상점에 자동 등장 (WeaponData SO와 동일 철학).
// Resources/Shop/ 에 배치. ShopService가 LoadAll로 읽는다.
[CreateAssetMenu(fileName = "ShopItem", menuName = "Game/ShopItem")]
public class ShopItemData : ScriptableObject
{
    public string id;                 // 저장키 — 구매목록 조회에 사용. 에셋마다 고유
    public string displayName;
    public ShopEffectType effect;

    public int[] costs;               // 티어 n → n+1 비용. 길이 = 최대 티어
    public float valuePerTier;        // StartHp: +HP/티어, Damage: +비율/티어(0.08=8%)
}
