using UnityEngine;

// SpriteRenderer 의 스프라이트를 프레임 배열로 순환시킨다.
//
// Animator/AnimationClip 을 쓰지 않는 이유: 이 프로젝트에는 AnimationClip 이 0개이고,
// 연출은 전부 코드 기반이다(PlayerIdlePulse, UIBlink, TreasureChest, BossController).
// 4프레임 루프 하나에 Animator Controller 를 도입하면 에디터 드래그 작업이 늘어나고
// 에이전트가 저작할 수 없게 된다. 프레임 배열은 [MenuItem] 으로 자동 배선할 수 있다.
//
// 프레임 소스는 art_import.py 가 만든 가로 스트립이고, 슬라이싱은
// GeneratedSpritePostprocessor 가 자동으로 한다.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] Sprite[] _frames;
    [SerializeField] float _fps = 8f;
    [SerializeField] Sprite[] _moveFrames;
    [SerializeField] float _moveFps = 8f;

    // 같은 종류가 여러 개 있을 때 전부 같은 프레임으로 깜빡이면 기계처럼 보인다.
    // 시작 위상을 흩어 개체마다 다른 프레임에서 시작하게 한다.
    [SerializeField] bool _randomizePhase = true;

    SpriteRenderer _sr;
    float _time;
    bool _isMoving;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_randomizePhase && _frames != null && _frames.Length > 0)
            _time = Random.Range(0f, _frames.Length / Mathf.Max(0.01f, _fps));
    }

    void Update()
    {
        bool useMoveFrames = _isMoving && _moveFrames != null && _moveFrames.Length > 0;
        Sprite[] activeFrames = useMoveFrames ? _moveFrames : _frames;
        float activeFps = useMoveFrames ? _moveFps : _fps;
        if (activeFrames == null || activeFrames.Length < 2) return;

        // Time.time 이 아니라 deltaTime 누적 — Rewind/Pause 로 TimeScale 이 바뀌면
        // 애니메이션도 함께 느려지거나 멈춰야 한다 (PlayerIdlePulse 와 같은 이유)
        _time += Time.deltaTime;

        int index = (int)(_time * activeFps) % activeFrames.Length;
        if (_sr.sprite != activeFrames[index])
            _sr.sprite = activeFrames[index];
    }

    public void SetMoving(bool isMoving)
    {
        if (_isMoving == isMoving) return;

        _isMoving = isMoving;
        _time = 0f;
    }

    // 보스 페이즈 전환처럼 프레임 세트를 통째로 갈아끼울 때 쓴다.
    public void SetFrames(Sprite[] frames)
    {
        _frames = frames;
        _time = 0f;
    }
}
