using UnityEngine;
using TMPro;

// 타이머/스코어/골드 매 프레임 갱신 — 자기등록.
// 타이머는 라이브로 째깍대는 WaveManager.GameTime 사용
// (Managers.Game.PlayTime 은 저장/되감기 시점에만 갱신되어 멈춰 보임).
public class HudStats : MonoBehaviour
{
    [SerializeField] TMP_Text _timerText;
    [SerializeField] TMP_Text _scoreText;
    [SerializeField] TMP_Text _goldText;

    WaveManager _wave;

    void Start()
    {
        _wave = FindObjectOfType<WaveManager>();
    }

    void Update()
    {
        var game = Managers.Game;
        if (game == null) return;

        if (_timerText != null && _wave != null)
        {
            int total = Mathf.FloorToInt(_wave.GameTime);
            _timerText.text = $"{total / 60:00}:{total % 60:00}";
        }

        if (_scoreText != null)
            _scoreText.text = game.Score.ToString();

        if (_goldText != null)
            _goldText.text = game.RunGold.ToString();
    }
}
