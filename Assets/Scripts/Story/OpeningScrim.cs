using UnityEngine;
using UnityEngine.UI;

// 오프닝 배경 위에 깔리는 세로 그라디언트. 위쪽은 완전 투명, 아래로 갈수록 어두워진다.
// 배경 일러스트 하단이 밝게 나와도 텍스트가 읽히게 하는 2차 방어선이다
// (1차는 이미지 생성 단계에서 하단 40%를 저정보 구역으로 명세하는 것, 3차는 TMP Underlay).
//
// UIProceduralSprite 와 같은 규칙을 따른다 — 절차 생성 Texture2D 는 에셋이 아니라서
// 씬에 sprite 를 구워 저장하면 참조가 null 로 직렬화된다(흰 박스). 파라미터만 직렬화하고
// OnEnable 에서 다시 만든다.
//
// UISpriteFactory 에 넣지 않은 이유: 이 워크트리는 master 소유 파일을 건드리지 않는다는 규약이 있고,
// 세로 그라디언트는 지금 오프닝 한 곳에서만 쓴다(AGENTS.md — 한 곳에서만 쓰는 건 추상화하지 않는다).
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class OpeningScrim : MonoBehaviour
{
    // 그라디언트가 시작되는 지점. 0 = 화면 최상단, 1 = 최하단.
    // 기본 0.5 는 기획 레이아웃(화면 하단 절반부터 어두워짐)과 맞춘다.
    [SerializeField, Range(0f, 1f)] float _start = 0.5f;

    // 화면 맨 아래에서의 불투명도.
    [SerializeField, Range(0f, 1f)] float _maxAlpha = 0.88f;

    // 세로 방향으로만 변하므로 가로 1px 이면 충분하다.
    const int Height = 64;

    void OnEnable() => Apply();

    public void Configure(float start, float maxAlpha)
    {
        _start = start;
        _maxAlpha = maxAlpha;
        Apply();
    }

    void Apply()
    {
        var img = GetComponent<Image>();
        if (img == null) return;

        var tex = new Texture2D(1, Height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };

        // UITheme.Backdrop 의 색만 쓰고 알파는 직접 계산한다 —
        // Backdrop 은 0.65 알파를 이미 갖고 있어 그대로 쓰면 최상단이 투명해지지 않는다.
        Color baseColor = UITheme.Backdrop;

        for (int y = 0; y < Height; y++)
        {
            // 텍스처 y=0 이 아래쪽이다. 화면 좌표(위 0 → 아래 1)로 환산한다.
            float screenT = 1f - (y / (float)(Height - 1));
            float t = Mathf.InverseLerp(_start, 1f, screenT);
            // 선형보다 부드럽게 — 경계선이 눈에 띄지 않는다
            float alpha = _maxAlpha * t * t;
            tex.SetPixel(0, y, new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
        }
        tex.Apply();

        img.sprite = Sprite.Create(tex, new Rect(0, 0, 1, Height), new Vector2(0.5f, 0.5f), 100f);
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;   // 탭은 그 아래 전체 화면 히트 영역이 받는다
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            Apply();
        };
    }
#endif
}
