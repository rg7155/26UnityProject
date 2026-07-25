using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;

public class SceneManagerEx
{
    public SceneType CurrentSceneType { get; private set; } = SceneType.Unknown;

    public void ChangeScene(SceneType type)
    {
        Managers.Object.Clear();   // 풀(@Pool·DontDestroyOnLoad)의 잔존 오브젝트 정리 — 다음 씬 유령 콜백 방지
        EnemyBase.ClearRegistry(); // static 적 레지스트리 초기화 — 다음 씬에서 파괴된 적 참조(MissingReference) 방지
        CurrentSceneType = type;
        SceneManager.LoadScene(GetSceneName(type));
    }

    string GetSceneName(SceneType type)
    {
        return type.ToString();
    }
}
