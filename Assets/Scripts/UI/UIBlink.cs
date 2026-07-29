using UnityEngine;

// 레이어 2 — CanvasGroup 알파 점멸 연출. 로직 스크립트가 _root 를 SetActive(true) 하는 순간
// OnEnable 이 위상을 0 으로 되돌리므로 경고가 뜰 때마다 항상 같은 밝기에서 시작한다(연출 타이머를 로직에 두지 않는 이유).
// unscaledTime 을 쓰는 건 게임플레이 상태가 아니라 순수 연출이라서다 — 정지/되감기 재현 대상이 아니다.
[RequireComponent(typeof(CanvasGroup))]
public class UIBlink : MonoBehaviour
{
    [SerializeField] float _period = 0.5f;
    [SerializeField] float _minAlpha = 0.35f;

    CanvasGroup _group;
    float _startTime;

    void OnEnable()
    {
        _group = GetComponent<CanvasGroup>();
        _startTime = Time.unscaledTime;
    }

    void Update()
    {
        float t = Mathf.PingPong((Time.unscaledTime - _startTime) / _period, 1f);
        _group.alpha = Mathf.Lerp(1f, _minAlpha, t);
    }
}
