using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 오프닝용 한글 폰트 에셋을 "실제로 쓰는 글자만" 담아 새로 만든다.
// 실행: Tools/Font/한글 폰트 재생성
//
// 왜 필요했나 — 기존 NotoSansKR SDF 에셋은 문자 테이블에 완성형 11,172자가 등록돼 있는데
// m_PointSize 가 3 이었다. 3pt 로 구웠기 때문에 512x512 한 장에 11,172자가 들어간 것이고,
// 글리프 하나가 3x4 픽셀이라 화면에서는 자리만 차지하고 아무것도 보이지 않았다.
// 게임 UI 가 전부 영어인 것도 이 때문이다.
//
// 기존 에셋을 고쳐 쓰지 않는 이유: PointSize 만 바꿔도 LineHeight·Ascender 등 FaceInfo 메트릭이
// 전부 3pt 기준으로 남는다. 이 값들은 폰트 페이스를 그 크기로 다시 로드해야 나오므로,
// TMP_FontAsset.CreateFontAsset 으로 새로 만드는 것이 유일하게 맞는 방법이다.
// 기존 에셋은 그대로 둔다 — 영문 UI 는 어차피 TMP_Settings 폴백으로 그려지고 있어 영향이 없다.
//
// 문자 집합은 코드에 나열하지 않고 데이터에서 수집한다. 대사를 고치고 폰트를 다시 굽지 않으면
// 새 글자가 조용히 빈칸이 되는데, 리터럴을 두 곳에 두면 그 사고가 반드시 난다.
//
// ASCII 는 일부러 넣지 않는다. 이 프로젝트의 영문 UI 는 TMP_Settings.defaultFontAsset
// (LiberationSans) 폴백으로 그려지고 있고, 여기에 ASCII 를 구우면 오프닝의 숫자·영문만
// 서체가 달라져 튄다.
public static class KoreanFontGenerator
{
    public const string OutputPath = "Assets/Font/OpeningKR SDF.asset";

    const string TtfPath  = "Assets/Font/NotoSansKR-VariableFont_wght.ttf";
    const string DataPath = "Assets/Resources/Story/Opening.asset";

    // 표시 크기는 48pt(캔버스 1080 기준). SDF 라 축소는 깨끗하므로 넉넉히 크게 굽는다.
    const int SamplingSize = 64;
    const int Padding      = 6;
    const int AtlasSize    = 1024;

    [MenuItem("Tools/Font/한글 폰트 재생성")]
    public static void Rebuild()
    {
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (ttf == null) { Debug.LogError($"[KoreanFont] 원본 ttf 가 없습니다: {TtfPath}"); return; }

        string chars = Collect();
        if (chars.Length == 0) { Debug.LogError("[KoreanFont] 수집된 글자가 없습니다"); return; }

        // 굽는 동안은 Dynamic 이어야 한다. 정적 에셋에는 글리프를 추가할 수 없다.
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
            ttf, SamplingSize, Padding, GlyphRenderMode.SDFAA,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);

        if (font == null) { Debug.LogError("[KoreanFont] 폰트 에셋 생성 실패"); return; }

        font.name = System.IO.Path.GetFileNameWithoutExtension(OutputPath);
        bool ok = font.TryAddCharacters(chars, out string missing);

        // 정적으로 굳힌다.
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        // 재실행이면 기존 에셋을 지우고 다시 만든다. GUID 가 바뀌므로
        // Tools/UI/Build Opening Overlay 를 다시 돌려 참조를 새로 걸어야 한다.
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath) != null)
            AssetDatabase.DeleteAsset(OutputPath);

        AssetDatabase.CreateAsset(font, OutputPath);

        // 아틀라스 텍스처와 머티리얼은 에셋이 아니라 메모리 객체다. 서브에셋으로 붙이지 않으면
        // 저장 후 참조가 끊겨 글자가 사라진다(UIProceduralSprite 와 같은 계열의 함정).
        if (font.atlasTextures != null)
        {
            for (int i = 0; i < font.atlasTextures.Length; i++)
            {
                Texture2D tex = font.atlasTextures[i];
                if (tex == null) continue;
                tex.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(tex, font);
            }
        }
        if (font.material != null)
        {
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
        }

        // 원본 ttf 참조를 끊는다 — 두면 10MB 짜리 ttf 가 빌드에 딸려온다.
        // 정적 에셋은 런타임에 원본을 필요로 하지 않는다.
        var so = new SerializedObject(font);
        var src = so.FindProperty("m_SourceFontFile");
        if (src != null) src.objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[KoreanFont] {OutputPath} — {chars.Length}자 요청 / " +
                  $"PointSize {font.faceInfo.pointSize} / 아틀라스 {AtlasSize}x{AtlasSize} / " +
                  $"등록 {font.characterTable.Count}자. " +
                  "Tools/UI/Build Opening Overlay 를 다시 실행해 폰트 참조를 갱신하세요");

        if (!ok || !string.IsNullOrEmpty(missing))
            Debug.LogError($"[KoreanFont] 아틀라스에 못 들어간 글자: {missing} — " +
                           $"AtlasSize({AtlasSize})를 키우거나 SamplingSize({SamplingSize})를 줄이세요");
    }

    // 실제로 화면에 뜨는 한글을 전부 모은다. 게임 데이터가 원본이고 이 목록은 파생물이다.
    static string Collect()
    {
        var set = new HashSet<char>();

        var data = AssetDatabase.LoadAssetAtPath<OpeningData>(DataPath);
        if (data != null && data.pages != null)
        {
            foreach (OpeningPage page in data.pages)
            {
                if (page == null || page.lines == null) continue;
                foreach (OpeningLine line in page.lines)
                    Add(set, line.text);
            }
        }
        else
        {
            Debug.LogWarning($"[KoreanFont] {DataPath} 를 못 찾았습니다. " +
                             "Tools/Story/Create Opening Assets 를 먼저 실행하세요");
        }

        Add(set, OpeningStatusLines.Low);
        Add(set, OpeningStatusLines.Mid);
        Add(set, OpeningStatusLines.High);

        var sorted = new List<char>(set);
        sorted.Sort();                       // 순서를 고정해야 다시 구울 때 결과가 같다

        var sb = new StringBuilder();
        foreach (char c in sorted) sb.Append(c);
        return sb.ToString();
    }

    // ASCII 는 제외한다(파일 상단 주석 참고).
    static void Add(HashSet<char> set, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
            if (c > 127) set.Add(c);
    }
}
