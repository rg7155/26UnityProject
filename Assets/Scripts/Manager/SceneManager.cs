using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;

public class SceneManagerEx
{
    public SceneType CurrentSceneType { get; private set; } = SceneType.Unknown;

    public void ChangeScene(SceneType type)
    {
        CurrentSceneType = type;
        SceneManager.LoadScene(GetSceneName(type));
    }

    string GetSceneName(SceneType type)
    {
        return type.ToString();
    }
}
