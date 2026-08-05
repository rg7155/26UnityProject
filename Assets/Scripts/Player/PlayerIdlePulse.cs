using UnityEngine;

// 플레이어 에너지 코어의 숨쉬기 연출 — 스프라이트 밝기를 사인파로 진동시킨다
// Player 프리팹의 SpriteRenderer와 같은 오브젝트에 붙인다
public class PlayerIdlePulse : MonoBehaviour
{
    [SerializeField] float _frequency = 2.2f;
    [SerializeField] float _amplitude = 0.25f;

    SpriteRenderer _sr;
    Color _baseColor;
    float _pulseTime;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color;
    }

    void Update()
    {
        _pulseTime += Time.deltaTime;   // Rewind/Pause 중 함께 멈추도록 Time.time 금지

        // 스프라이트 색은 1을 못 넘으므로 기준색을 최대치로 두고 아래로만 진동시킨다
        float dim = _amplitude * 0.5f * (1f - Mathf.Sin(_pulseTime * _frequency));
        _sr.color = new Color(
            _baseColor.r * (1f - dim),
            _baseColor.g * (1f - dim),
            _baseColor.b * (1f - dim),
            _baseColor.a);
    }
}
