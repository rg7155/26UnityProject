using UnityEngine;
using UnityEngine.UI;

// 배경에 그려진 네온 표시등 위에 겹쳐 천천히 명멸하는 발광점.
// P1 의 「살아 있는 건 프로세스뿐이다」를 글자 없이 보여주는 장치다 —
// 죽은 서버실에서 유일하게 아직 동작하는 것이 눈에 보여야 한다.
//
// 배경(Background)의 자식이라 켄 번즈의 줌/팬을 같이 받는다. 그래야 화면이 움직여도
// 표시등이 그림 위 제자리에 붙어 있다.
//
// OpeningScrim 과 같은 규칙 — 절차 생성 Texture2D 는 에셋이 아니라서 씬에 구워 저장하면
// 참조가 null 로 직렬화된다. 파라미터만 직렬화하고 런타임에 다시 만든다.
[RequireComponent(typeof(Image))]
public class OpeningBlinkDot : MonoBehaviour
{
    // 한 주기. 2초면 "숨쉬는" 속도로 읽히고, 이보다 빠르면 경고등처럼 보여 톤이 어긋난다.
    [SerializeField] float _period = 2.0f;
    [SerializeField, Range(0f, 1f)] float _minAlpha = 0.15f;
    [SerializeField, Range(0f, 1f)] float _maxAlpha = 0.90f;

    // 페이지 전환 암전 때 배경과 같이 사라져야 한다. OpeningSequence 가 배경 알파를 그대로 넘긴다.
    public float MasterAlpha { get; set; } = 1f;

    const int TexSize = 64;

    Image _img;
    RectTransform _rt;
    float _elapsed;

    void OnEnable()
    {
        EnsureRefs();
        _elapsed = 0f;
    }

    // 배경 그림마다 표시등 위치가 다르므로 페이지 데이터가 좌표를 준다.
    public void Show(Vector2 anchor)
    {
        EnsureRefs();
        _rt.anchorMin = anchor;
        _rt.anchorMax = anchor;
        _rt.anchoredPosition = Vector2.zero;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    void Update()
    {
        if (_img == null) return;

        _elapsed += Time.unscaledDeltaTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_elapsed * Mathf.PI * 2f / Mathf.Max(0.01f, _period));

        Color c = _img.color;
        c.a = Mathf.Lerp(_minAlpha, _maxAlpha, pulse) * MasterAlpha;
        _img.color = c;
    }

    void EnsureRefs()
    {
        if (_rt == null) _rt = (RectTransform)transform;
        if (_img == null) _img = GetComponent<Image>();
        if (_img == null) return;
        if (_img.sprite == null) _img.sprite = BuildGlow();

        _img.type = Image.Type.Simple;
        _img.raycastTarget = false;   // 탭은 그 아래 전체 화면 히트 영역이 받는다
    }

    // 중심에서 밖으로 사그라지는 원형 발광. 단색 원은 배경 위에 스티커처럼 얹혀 보인다.
    static Sprite BuildGlow()
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };

        Color glow = UITheme.Cyan;
        float half = (TexSize - 1) * 0.5f;

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float d = new Vector2((x - half) / half, (y - half) / half).magnitude;
                // 4제곱 감쇠 — 중심의 코어는 또렷하고 바깥은 넓게 퍼진다
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a * a;
                tex.SetPixel(x, y, new Color(glow.r, glow.g, glow.b, a));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f);
    }
}
