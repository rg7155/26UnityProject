// 인게임 상단 HUD 밴드의 좌표 단일 소유자(StatusStrip → 보스 HP → WARNING).
//
// 상단 밴드 좌표는 여기서만 정의한다. 생성기에서 자체 계산·상수 복사 금지.
// 예전엔 생성기마다 "StatusStrip 아래"를 각자 계산해 같은 슬롯을 두 개가 차지했다.
// 슬롯은 고정값이다 — 보스 바/WARNING 이 토글될 때 다른 HUD 가 위아래로 튀면 안 되므로
// VerticalLayoutGroup 을 쓰지 않는다.
//
// y 는 HudRoot 상단 기준 아래 방향 음수(pivot=(0.5,1) 전제).
public static class UIHudLayout
{
    // ── 밴드 1: 상태 스트립(HP/XP/Score/Timer/Gold) ──
    public const float StatusStripH = 180f;

    // ── 밴드 2: 보스 HP 블록 ──
    public const float BossNameRowH = 30f;   // HpBarH 와 같은 무게감
    public const float BossBarH = 30f;
    public const float BossBandH = UITheme.S2 * 2f + BossNameRowH + UITheme.S1 + BossBarH;
    public const float BossBandY = -(StatusStripH + UITheme.S3);

    // ── 밴드 3: WARNING 배너 ──
    public const float WarnLabelH = 40f;     // Header(34) 한 줄
    public const float WarnSubH = 24f;       // Caption(20) 한 줄
    public const float WarnBandH = UITheme.S4 * 2f + WarnLabelH + UITheme.S1 + WarnSubH;
    public const float WarnBandY = BossBandY - BossBandH - UITheme.S4;
}
