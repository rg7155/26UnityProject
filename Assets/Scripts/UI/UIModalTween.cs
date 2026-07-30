using UnityEngine;

// 레이어 2 — 모달이 켜질 때 살짝 커지며 나타나는 등장 연출.
// 로직 스크립트가 SetActive(true) 하는 순간 OnEnable 이 위상을 0 으로 되돌린다(UIBlink 와 같은 방식).
//
// 닫힘 연출은 없다. 패널을 닫는 쪽이 곧바로 SetActive(false) 를 호출해 오브젝트가 즉시 사라지므로,
// 나가는 연출을 넣으려면 기존 UI 로직 스크립트를 고쳐야 한다(무수정 원칙). 등장만으로 체감의 대부분이 온다.
//
// unscaledDeltaTime 을 쓰는 이유가 중요하다 — 이 프로젝트는 정지 경로가 둘이다.
// 업그레이드 패널은 GameState.Paused 만 걸고 timeScale 은 1 이지만, Pause 화면은 timeScale=0 까지 건다.
// deltaTime 을 쓰면 Pause 화면이 영원히 열리지 않는다.
[RequireComponent(typeof(CanvasGroup))]
public class UIModalTween : MonoBehaviour
{
    [SerializeField] float _duration  = UITheme.ModalTween;
    [SerializeField] float _fromScale = UITheme.ModalFromScale;

    CanvasGroup _group;
    float _elapsed;

    void OnEnable()
    {
        _group = GetComponent<CanvasGroup>();
        _elapsed = 0f;
        Apply(0f);
    }

    void Update()
    {
        if (_elapsed >= _duration) return;

        _elapsed += Time.unscaledDeltaTime;
        Apply(Mathf.Clamp01(_duration > 0f ? _elapsed / _duration : 1f));
    }

    void Apply(float t)
    {
        float eased = 1f - (1f - t) * (1f - t);   // ease-out — 빠르게 뜨고 부드럽게 멈춘다
        _group.alpha = eased;
        transform.localScale = Vector3.one * Mathf.Lerp(_fromScale, 1f, eased);
    }
}
