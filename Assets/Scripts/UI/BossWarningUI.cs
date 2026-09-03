using UnityEngine;

// WaveManager.BossWarningActive는 _gameTime에서 매 프레임 계산되는 프로퍼티라 폴링이 곧 정답이다.
// 점멸 연출은 비주얼(artist) 담당 — 로직 타이머를 두면 Playing 가드/되감기 재현 대상이 하나 늘어난다
public class BossWarningUI : MonoBehaviour
{
    [SerializeField] GameObject _root;

    WaveManager _wave;

    void Update()
    {
        if (_wave == null)
            _wave = FindObjectOfType<WaveManager>();

        if (_wave == null || _root == null) return;

        _root.SetActive(_wave.BossWarningActive);
    }
}
