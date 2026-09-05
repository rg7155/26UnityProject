using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

// 오프닝 SO 를 Resources/Story/ 에 생성한다. 재실행 시 기존 에셋의 값을 갱신한다(GUID 유지).
// QuestAssetGenerator 와 같은 방식 — 에이전트가 인스펙터를 못 만지므로 데이터도 코드로 만든다.
//
// 대사는 한글이다. NotoSansKR 폰트에 완성형 11,172자가 전부 구워져 있어 그대로 나온다.
// (QuestAssetGenerator 의 "한글 글리프가 없어 영어로 통일" 주석은 폰트 추가 전 기록이다)
public static class OpeningAssetGenerator
{
    const string FolderPath = "Assets/Resources/Story";
    const string AssetPath  = FolderPath + "/Opening.asset";
    const string BgFolder   = "Assets/Resources/UI/Opening";
    const string FontPath   = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Story/Create Opening Assets")]
    public static void CreateOpeningAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Story"))
            AssetDatabase.CreateFolder("Assets/Resources", "Story");

        var data = AssetDatabase.LoadAssetAtPath<OpeningData>(AssetPath);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<OpeningData>();

        data.pages = new[]
        {
            // 켄 번즈 방향은 각 장의 시선 유도와 같은 쪽으로 잡는다.
            // 배율은 1.06~1.09 — 이보다 크면 "움직인다"가 인식돼 대사에서 시선이 떨어진다.

            // 복도 안쪽 표시등으로 전진한다. 소실점이 화면 위쪽(y 67%)이라 이미지를 아래로 민다.
            Motion(1.00f, 1.08f, new Vector2(0f, 0f), new Vector2(0f, -0.012f),
            Blink(new Vector2(0.50f, 0.66f),
            Page("Opening01_DeadArchive",
                L(Speaker.ARC,      "아카이브 12구역, 신호 없음."),
                L(Speaker.ARC,      "살아 있는 건 프로세스뿐이다.")))),

            // 캡슐로 다가간다. 순수 줌 — 파일럿이 깨어나는 장이라 시선을 옮기지 않는다.
            Motion(1.00f, 1.07f, Vector2.zero, Vector2.zero,
            Page("Opening02_LastBackup",
                L(Speaker.ARC,      "마지막 백업을 꺼낸다."),
                L(Speaker.PILOT,    "…여긴 어디지."),
                L(Speaker.ARC,      "네가 죽은 곳이야."))),

            // 이 페이지가 오프닝의 전부다 — 핵심 기믹(5초 버퍼)과 주제(되감기=삭제)를
            // 동시에 설명한다. 나머지 네 페이지는 이 세 줄을 위한 조립이다.
            // 유일하게 뒤로 물러난다. 규칙을 설명하는 장이라 눈금 전체가 드러나야 한다.
            Motion(1.06f, 1.00f, Vector2.zero, Vector2.zero,
            Page("Opening03_FiveSeconds",
                L(Speaker.ARC,      "버퍼는 5초. 그만큼 되돌릴 수 있어."),
                L(Speaker.PILOT,    "5초. 그게 다야?"),
                L(Speaker.ARC,      "되돌린 5초는 기억에서도 지워져."))),

            // 요새로 압박해 들어간다. 다섯 장 중 가장 큰 배율 — 위협이 커지는 장이다.
            Motion(1.00f, 1.09f, Vector2.zero, new Vector2(0f, -0.010f),
            Page("Opening04_Monolith",
                L(Speaker.MONOLITH, "기록 불량. 덮어쓴다."),
                L(Speaker.ARC,      "관리자가 널 파일로 본다."),
                L(Speaker.ARC,      "지워지기 전에 지워."))),

            // 마지막 두 줄이 결론이다. 파일럿은 세지 못하고 ARC 가 대신 센다.
            // 이 약속은 타이틀 상태줄(LifetimeRewinds 표시)에서 회수된다.
            // 배율은 고정하고 위로만 훑는다. 여명이 드러나며 로고 자리로 시선이 올라간다.
            Motion(1.06f, 1.06f, new Vector2(0f, 0.020f), new Vector2(0f, -0.020f),
            Page("Opening05_Horizon",
                L(Speaker.ARC,      "너는 몇 번째인지 몰라도 돼."),
                L(Speaker.ARC,      "세는 건 내가 할게."))),
        };

        if (isNew) AssetDatabase.CreateAsset(data, AssetPath);
        else EditorUtility.SetDirty(data);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Report(data);
    }

    static OpeningPage Page(string id, params OpeningLine[] lines)
    {
        return new OpeningPage
        {
            id = id,
            background = LoadBackground(id),
            lines = lines,
        };
    }

    // 페이지를 감싸 켄 번즈 값을 얹는다. 데이터가 코드에 있으므로 장면 의도를
    // 주석과 같은 자리에서 읽을 수 있다.
    static OpeningPage Motion(float zoomFrom, float zoomTo, Vector2 panFrom, Vector2 panTo, OpeningPage page)
    {
        page.zoomFrom = zoomFrom;
        page.zoomTo   = zoomTo;
        page.panFrom  = panFrom;
        page.panTo    = panTo;
        return page;
    }

    // 표시등 위치는 그림마다 다르다. 여기 값은 기획 구도 기준이고,
    // 실제 생성물과 어긋나면 인스펙터에서 blinkAnchor 만 옮기면 된다.
    static OpeningPage Blink(Vector2 anchor, OpeningPage page)
    {
        page.blink = true;
        page.blinkAnchor = anchor;
        return page;
    }

    static OpeningLine L(Speaker speaker, string text)
    {
        return new OpeningLine { speaker = speaker, text = text };
    }

    // 배경은 사람이 생성해 넣는다. 아직 없으면 null 로 두고 경고만 남긴다 —
    // 대사와 흐름은 배경 없이도 검증할 수 있어야 한다.
    static Sprite LoadBackground(string id)
    {
        foreach (string ext in new[] { ".png", ".jpg" })
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgFolder}/{id}{ext}");
            if (sprite != null) return sprite;
        }
        return null;
    }

    // 기획 규격(5페이지 / 13줄 / 176자 / 약 28초)에서 벗어났는지 즉시 알 수 있게 한다.
    static void Report(OpeningData data)
    {
        var missing = new List<string>();
        int chars = 0;
        int overLength = 0;

        foreach (OpeningPage page in data.pages)
        {
            if (page.background == null) missing.Add(page.id);
            foreach (OpeningLine line in page.lines)
            {
                chars += line.text.Length;
                if (line.text.Length > 24) overLength++;   // 세로 540 폭 Body 24pt 한 줄 상한
            }
        }

        float seconds = chars / data.charsPerSecond + data.TotalLines * data.autoAdvanceDelay;
        Debug.Log($"[OpeningAssetGenerator] {data.pages.Length}페이지 / {data.TotalLines}줄 / " +
                  $"{chars}자 / 약 {seconds:F0}초");

        if (overLength > 0)
            Debug.LogWarning($"[OpeningAssetGenerator] 24자를 넘는 줄이 {overLength}개다. 세로 화면에서 줄바꿈된다");
        if (missing.Count > 0)
            Debug.LogWarning($"[OpeningAssetGenerator] 배경 미연결: {string.Join(", ", missing)} " +
                             $"— {BgFolder}/ 에 넣고 다시 실행할 것");

        WarnMissingGlyphs(data);
    }

    // 대사를 고치고 폰트를 다시 굽지 않으면 새 글자가 화면에서 빈칸으로 나온다.
    // 예외도 로그도 없이 조용히 사라지므로, 대사를 만드는 이 자리에서 잡는다.
    static void WarnMissingGlyphs(OpeningData data)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) return;

        var have = new HashSet<uint>();
        foreach (TMP_Character c in font.characterTable) have.Add(c.unicode);

        var missing = new SortedSet<char>();
        foreach (OpeningPage page in data.pages)
            foreach (OpeningLine line in page.lines)
                foreach (char c in line.text)
                    if (c > 127 && !have.Contains(c)) missing.Add(c);

        foreach (char c in OpeningStatusLines.Low + OpeningStatusLines.Mid + OpeningStatusLines.High)
            if (c > 127 && !have.Contains(c)) missing.Add(c);

        if (missing.Count == 0) return;

        Debug.LogError($"[OpeningAssetGenerator] 폰트에 없는 글자 {missing.Count}개: " +
                       $"{string.Join("", missing)}
" +
                       "Tools/Font/한글 폰트 재생성 을 실행하세요 — 안 하면 화면에서 빈칸으로 나옵니다");
    }
}
