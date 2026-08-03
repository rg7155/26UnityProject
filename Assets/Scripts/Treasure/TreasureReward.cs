using UnityEngine;

public struct TreasureRollResult
{
    public int slotIndex;     // 0~15, 테두리 시계방향
    public int baseGold;
    public float multiplier;
    public int gold;
}

// 연출이 결과를 만들지 않는다 — 여기서 먼저 뽑고, 팝업은 그 칸에 멈추도록 이동량을 역산만 한다
public static class TreasureReward
{
    public const int SlotCount = 16;

    // 테두리 시계방향 순서와 1:1 고정 — 칸 위치가 매판 바뀌면 "잭팟이 어디 있는지 보는" 재미가 죽는다
    public static readonly int[] Layout = { 20, 40, 80, 40, 150, 20, 40, 80, 20, 40, 300, 20, 80, 40, 20, 20 };

    public static TreasureRollResult Roll(float gameTime)
    {
        int slot = Random.Range(0, SlotCount);
        int baseGold = Layout[slot];
        float multiplier = Multiplier(gameTime);

        return new TreasureRollResult
        {
            slotIndex = slot,
            baseGold = baseGold,
            multiplier = multiplier,
            gold = Mathf.RoundToInt(baseGold * multiplier)
        };
    }

    static float Multiplier(float gameTime)
    {
        if (gameTime >= 240f) return 2f;
        if (gameTime >= 120f) return 1.5f;
        return 1f;
    }
}
