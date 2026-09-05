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

    // --- 켄 번즈 (정지 이미지를 느리게 줌/팬해서 영상처럼 보이게 하는 기법) ---
    // 배경 5장이 전부 정지 일러스트라 아무 움직임이 없으면 슬라이드쇼로 읽힌다.
    // 이미지를 다시 만들지 않고 화면에 시간축을 주는 가장 싼 방법이다.

    [Header("켄 번즈")]
    // 배율. 1 보다 커야 팬으로 배경 바깥이 드러나지 않는다.
    public float zoomFrom = 1.00f;
    public float zoomTo   = 1.06f;

    // 화면 크기 대비 비율. 값은 "이미지가 움직이는 방향"이다 —
    // 위쪽을 보여주고 싶으면 이미지를 아래(-y)로 민다.
    // 확대로 생긴 여백((zoom-1)/2)을 넘는 값은 재생 시 잘린다.
    public Vector2 panFrom = Vector2.zero;
    public Vector2 panTo   = Vector2.zero;

    // --- 표시등 깜빡임 ---
    // 배경에 그려진 네온 점 위에 발광을 겹쳐 천천히 명멸시킨다.
    // P1 의 「살아 있는 건 프로세스뿐이다」를 글자 없이 보여주는 장치다.

    [Header("표시등")]
    public bool blink;
    // 화면 정규화 좌표(0~1, 좌하단 원점). 배경 그림의 표시등 위치에 맞춰 인스펙터에서 조정한다.
    public Vector2 blinkAnchor = new Vector2(0.5f, 0.66f);
}
