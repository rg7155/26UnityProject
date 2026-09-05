using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 오프닝은 최초 1회만 재생된다(GameData.SeenOpening). 한 번 보고 나면 다시 볼 방법이 없어
// 검수가 불가능해지므로, 그 플래그만 되돌리는 에디터 도구를 둔다.
//
// 세이브를 통째로 지우지 않는 이유: 골드·상점·퀘스트가 같이 날아가면
// 상태줄(LifetimeRewinds 0/30/150 구간)을 확인할 수 없다.
//
// 게임 코드에는 다시보기 진입점을 만들지 않는다 — 기획에서 명시적으로 뺀 항목이다.
// 여기는 Editor 폴더라 빌드에 포함되지 않는다.
public static class OpeningDebugMenu
{
    static string SavePath { get { return Application.persistentDataPath + "/SaveData.json"; } }

    [MenuItem("Tools/Story/오프닝 다시 보기")]
    public static void ResetSeenOpening()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log($"[OpeningDebug] 세이브가 없습니다 — 이미 처음 실행 상태입니다. {SavePath}");
            return;
        }

        string json = File.ReadAllText(SavePath);
        string next = Regex.Replace(json, "\"SeenOpening\":\s*true", "\"SeenOpening\":false");

        if (next == json)
        {
            Debug.Log($"[OpeningDebug] SeenOpening 이 이미 false 입니다. 다음 실행에서 오프닝이 나옵니다. {SavePath}");
            return;
        }

        File.WriteAllText(SavePath, next);
        Debug.Log($"[OpeningDebug] SeenOpening 을 해제했습니다. 다음 실행에서 오프닝이 재생됩니다. {SavePath}");
    }

    [MenuItem("Tools/Story/세이브 폴더 열기")]
    public static void RevealSave()
    {
        EditorUtility.RevealInFinder(SavePath);
    }
}
