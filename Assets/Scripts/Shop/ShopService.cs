using System.Collections.Generic;
using UnityEngine;

// 메타 상점의 단일 접근점 — 항목 로드 · 구매 처리 · 런 시작 파생값 계산.
// 항목은 Resources/Shop/ 의 ShopItemData 에셋에서 로드 (하드코딩 없음).
public static class ShopService
{
    const int BaseMaxHp = 100;   // PlayerController._maxHp 기본값과 일치

    static ShopItemData[] _items;
    public static ShopItemData[] Items
    {
        get
        {
            if (_items == null) _items = Resources.LoadAll<ShopItemData>("Shop");
            return _items;
        }
    }

    static GameData D => Managers.Game.SaveData;

    // ── 구매 진행도 ──
    public static int TierOf(string id)
    {
        foreach (var p in D.Purchases)
            if (p.id == id) return p.tier;
        return 0;
    }

    public static int MaxTier(ShopItemData item) => item.costs.Length;
    public static bool IsMaxed(ShopItemData item) => TierOf(item.id) >= item.costs.Length;

    // 다음 티어 비용 — maxed면 -1
    public static int Cost(ShopItemData item)
    {
        int tier = TierOf(item.id);
        return tier >= item.costs.Length ? -1 : item.costs[tier];
    }

    public static bool CanBuy(ShopItemData item)
    {
        int cost = Cost(item);
        return cost >= 0 && Managers.Game.TotalGold >= cost;
    }

    public static bool Buy(ShopItemData item)
    {
        if (!CanBuy(item)) return false;
        Managers.Game.SpendGold(Cost(item));
        SetTier(item.id, TierOf(item.id) + 1);
        Managers.Game.SaveGame();
        return true;
    }

    static void SetTier(string id, int tier)
    {
        foreach (var p in D.Purchases)
            if (p.id == id) { p.tier = tier; return; }
        D.Purchases.Add(new ShopPurchase { id = id, tier = tier });
    }

    // ── 런 시작 적용 ──
    public static int BonusHp()
    {
        int bonus = 0;
        foreach (var item in Items)
            if (item.effect == ShopEffectType.StartHp)
                bonus += Mathf.RoundToInt(TierOf(item.id) * item.valuePerTier);
        return bonus;
    }

    public static float DamageMult()
    {
        float mult = 1f;
        foreach (var item in Items)
            if (item.effect == ShopEffectType.Damage)
                mult += TierOf(item.id) * item.valuePerTier;
        return mult;
    }

    // ── UI 표시용 파생값 ──
    public static int HpTotal(int tier, float valuePerTier) => BaseMaxHp + Mathf.RoundToInt(tier * valuePerTier);
    public static int DamagePct(int tier, float valuePerTier) => Mathf.RoundToInt(tier * valuePerTier * 100f);
}
