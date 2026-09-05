using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 한글 폰트 아틀라스를 "실제로 쓰는 글자만" 담아 다시 굽는다.
// 실행: Tools/Font/한글 폰트 재생성
//
// 왜 필요했나 — 기존 에셋은 문자 테이블에 11,172자(완성형 전체)가 등록돼 있는데
// 아틀라스 텍스처는 512x512 한 장뿐이었다. 그 넓이에 11,172자가 들어갈 수 없어
// 대부분의 글자가 빈 영역을 가리켰고, 화면에서는 자리(폭)만 차지하고 아무것도 그려지지 않았다.
// 게임 UI 가 전부 영어인 것도 이 때문이다.
//
// 문자 집합은 코드에 나열하지 않고 데이터에서 수집한다. 대사를 고치고 폰트를 다시 굽지 않으면
// 새 글자가 빈칸으로 나오는데, 리터럴을 두 곳에 두면 그 사고가 반드시 난다.
//
// ASCII 는 일부러 넣지 않는다. 이 프로젝트의 영문 UI 는 TMP_Settings.defaultFontAsset
// (LiberationSans)로 폴백해 그려지고 있고, 여기에 ASCII 를 구우면 게임 전체의 영문 서체가
// 한 번에 바뀐다. 제출 직전에 만들 변화가 아니다.
public static class KoreanFontGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";
    const string TtfPath  = "Assets/Font/NotoSansKR-VariableFont_wght.ttf";
    const string DataPath = "Assets/Resources/Story/Opening.asset";

    const int AtlasSize    = 1024;
    const int SamplingSize = 64;   // 표시 크기 48pt 보다 크게 굽는다. SDF 라 축소는 깨끗하다
    const int Padding      = 6;

    [MenuItem("Tools/Font/한글 폰트 재생성")]
    public static void Rebuild()
    {
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (ttf == null) { Debug.LogError($"[KoreanFont] 원본 ttf 가 없습니다: {TtfPath}"); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) { Debug.LogError($"[KoreanFont] 폰트 에셋이 없습니다: {FontPath}"); return; }

        string chars = Collect();
        if (chars.Length == 0) { Debug.LogError("[KoreanFont] 수집된 글자가 없습니다"); return; }

        // 굽는 동안만 Dynamic 이어야 한다. 정적 에셋은 글리프를 추가할 수 없다.
        var so = new SerializedObject(font);
        so.FindProperty("m_SourceFontFile").objectReferenceValue = ttf;
        so.ApplyModifiedPropertiesWithoutUndo();

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.ClearFontAssetData(setAtlasSizeToZero: true);

        so.Update();
        so.FindProperty("m_AtlasWidth").intValue  = AtlasSize;
        so.FindProperty("m_AtlasHeight").intValue = AtlasSize;
        so.FindProperty("m_AtlasPadding").intValue = Padding;
        so.FindProperty("m_FaceInfo.m_PointSize").intValue = SamplingSize;
        so.ApplyModifiedPropertiesWithoutUndo();

        bool ok = font.TryAddCharacters(chars, out string missing);

        // 다시 정적으로. 런타임에 원본 ttf 를 들고 다니지 않아야 빌드가 커지지 않는다.
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        so.Update();
        so.FindProperty("m_SourceFontFile").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        font.ReadFontAssetDefinition();

        EditorUtility.SetDirty(font);
        if (font.atlasTextures != null)
            foreach (Texture2D tex in font.atlasTextures)
                if (tex != null) EditorUtility.SetDirty(tex);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[KoreanFont] {chars.Length}자 요청 / 아틀라스 {AtlasSize}x{AtlasSize} " +
                  $"{(font.atlasTextures != null ? font.atlasTextures.Length : 0)}장 / " +
                  $"등록 {font.characterTable.Count}자");

        if (!ok || !string.IsNullOrEmpty(missing))
            Debug.LogError($"[KoreanFont] 아틀라스에 못 들어간 글자가 있습니다: {missing}\n" +
                           $"AtlasSize({AtlasSize})를 키우거나 SamplingSize({SamplingSize})를 줄이세요");
    }

    // 실제로 화면에 뜨는 한글을 전부 모은다. 게임 데이터가 원본이고 이 목록이 파생물이다.
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

        var sb = new StringBuilder();
        var sorted = new List<char>(set);
        sorted.Sort();                       // 순서를 고정해야 다시 구울 때 결과가 같다
        foreach (char c in sorted) sb.Append(c);
        return sb.ToString();
    }

    // ASCII 는 제외한다. 영문 UI 의 서체를 바꾸지 않기 위함이다(파일 상단 주석 참고).
    static void Add(HashSet<char> set, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
            if (c > 127) set.Add(c);
    }
}
