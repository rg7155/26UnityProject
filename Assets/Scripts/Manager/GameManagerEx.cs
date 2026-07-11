using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Define;

// 상점 항목별 구매 진행도 — id로 ShopItemData와 연결. JsonUtility가 List<[Serializable]> 직렬화 가능
[Serializable]
public class ShopPurchase
{
    public string id;
    public int tier;
}

[Serializable]
public class GameData
{
    public int Score;
    public float PlayTime;
    public int BestScore;
    public float BestTime;
    public bool BGMOn = true;
    public bool EffectSoundOn = true;

    public int TotalGold;
    public List<ShopPurchase> Purchases = new List<ShopPurchase>();
}

public class GameManagerEx
{
    GameData _gameData = new GameData();
    public GameData SaveData { get { return _gameData; } set { _gameData = value; } }

    GameState _state = GameState.Playing;
    public event Action<GameState> OnStateChanged;
    public GameState State
    {
        get { return _state; }
        set { _state = value; OnStateChanged?.Invoke(value); }
    }

    public int Score
    {
        get { return _gameData.Score; }
        set { _gameData.Score = value; }
    }

    public float PlayTime
    {
        get { return _gameData.PlayTime; }
        set { _gameData.PlayTime = value; }
    }

    public int BestScore { get { return _gameData.BestScore; } }
    public float BestTime { get { return _gameData.BestTime; } }

    public int TotalGold { get { return _gameData.TotalGold; } }
    public int RunGold { get; set; }   // 이번 판 획득분 — 비직렬화, CommitResult에서 뱅킹

    public void SpendGold(int amount) { _gameData.TotalGold -= amount; }
    public void AddGold(int amount) { _gameData.TotalGold += amount; }

    public bool BGMOn
    {
        get { return _gameData.BGMOn; }
        set { _gameData.BGMOn = value; }
    }

    public bool EffectSoundOn
    {
        get { return _gameData.EffectSoundOn; }
        set { _gameData.EffectSoundOn = value; }
    }

    string _path;

    public void Init()
    {
        _path = Application.persistentDataPath + "/SaveData.json";
        LoadGame();
    }

    public void SaveGame()
    {
        string jsonStr = JsonUtility.ToJson(_gameData);
        File.WriteAllText(_path, jsonStr);
    }

    public void CommitResult()
    {
        if (Score > _gameData.BestScore) _gameData.BestScore = Score;
        if (PlayTime > _gameData.BestTime) _gameData.BestTime = PlayTime;
        _gameData.TotalGold += RunGold;
        SaveGame();
    }

    public bool LoadGame()
    {
        if (File.Exists(_path) == false)
            return false;

        string fileStr = File.ReadAllText(_path);
        GameData data = JsonUtility.FromJson<GameData>(fileStr);
        if (data != null)
            _gameData = data;

        return true;
    }
}
