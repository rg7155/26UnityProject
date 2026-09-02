using UnityEngine;

// 오프닝 시퀀스의 화자. 색은 UITheme 토큰에 대응한다 —
// ARC = Cyan, PILOT = TextPrimary, MONOLITH = Danger.
// 이름을 영문 대문자로 두는 이유: 기존 UI 톤(BOSS RUSH / SLAYER)과 맞추기 위함.
// NotoSansKR 폰트에는 ASCII 글리프가 없지만 TMP_Settings.defaultFontAsset(LiberationSans)로
// 자동 폴백된다 — 기존 HUD의 영문·숫자가 이미 같은 경로를 쓴다.
public enum Speaker
{
    ARC,
    PILOT,
    MONOLITH,
}

// 대사 한 줄. 화면에는 한 번에 한 줄만 뜨고, 탭하거나 시간이 지나면 다음 줄로 넘어간다.
// 세로 540 폭 Body(24pt) 기준 한 줄 20~24자를 넘기지 않는 것이 기획 규격이다.
[System.Serializable]
public class OpeningLine
{
    public Speaker speaker;
    [TextArea(1, 3)] public string text;
}

// 배경 1장 + 그 위에 순차로 흐르는 대사들. 페이지가 넘어갈 때만 배경이 바뀐다.
[System.Serializable]
public class OpeningPage
{
    public string id;              // Opening01 등 — 배경 파일명과 짝을 이룬다
    public Sprite background;      // Assets/Resources/UI/Opening/ 아래 이미지
    public OpeningLine[] lines;
}
