using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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
            Page("Opening01_DeadArchive",
                L(Speaker.ARC,      "아카이브 12구역, 신호 없음."),
                L(Speaker.ARC,      "살아 있는 건 프로세스뿐이다.")),

            Page("Opening02_LastBackup",
                L(Speaker.ARC,      "마지막 백업을 꺼낸다."),
                L(Speaker.PILOT,    "…여긴 어디지."),
                L(Speaker.ARC,      "네가 죽은 곳이야.")),

            // 이 페이지가 오프닝의 전부다 — 핵심 기믹(5초 버퍼)과 주제(되감기=삭제)를
            // 동시에 설명한다. 나머지 네 페이지는 이 세 줄을 위한 조립이다.
            Page("Opening03_FiveSeconds",
                L(Speaker.ARC,      "버퍼는 5초. 그만큼 되돌릴 수 있어."),
                L(Speaker.PILOT,    "5초. 그게 다야?"),
                L(Speaker.ARC,      "되돌린 5초는 기억에서도 지워져.")),

            Page("Opening04_Monolith",
                L(Speaker.MONOLITH, "기록 불량. 덮어쓴다."),
                L(Speaker.ARC,      "관리자가 널 파일로 본다."),
                L(Speaker.ARC,      "지워지기 전에 지워.")),

            // 마지막 두 줄이 결론이다. 파일럿은 세지 못하고 ARC 가 대신 센다.
            // 이 약속은 타이틀 상태줄(LifetimeRewinds 표시)에서 회수된다.
            Page("Opening05_Horizon",
                L(Speaker.ARC,      "너는 몇 번째인지 몰라도 돼."),
                L(Speaker.ARC,      "세는 건 내가 할게.")),
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
    }
}
