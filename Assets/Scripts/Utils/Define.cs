using UnityEngine;

public class Define
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
    }

    public enum CreatureState
    {
        Idle,
        Moving,
        Dead,
    }

    public enum SceneType
    {
        Unknown,
        GameScene,
    }

    public enum Sound
    {
        Bgm,
        SubBgm,
        Effect,
        Max,
    }

    public enum UIEvent
    {
        Click,
        Press,
    }
}
