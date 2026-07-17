using System.Collections.Generic;
using UnityEngine;

// 레이어 1 — 외부 PNG 없이 라운드 사각형 + 두꺼운 외곽선 스프라이트를 코드로 생성.
// Sprite.Create 의 border 로 9-slice 지정 → 크기가 변해도 모서리·테두리 두께 유지.
// 파라미터 조합을 키로 static 캐시해 중복 생성 방지.
public static class UISpriteFactory
{
    static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    // 라운드 사각형 + 안쪽 외곽선. radius/outline 은 px.
    public static Sprite RoundedOutlined(int radius, int outline, Color fill, Color line)
    {
        string key = $"ro_{radius}_{outline}_{ColorUtility.ToHtmlStringRGBA(fill)}_{ColorUtility.ToHtmlStringRGBA(line)}";
        if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

        int b = radius + outline;          // 9-slice 고정 코너 크기(라운드+외곽선)
        int pad = 2;                        // 늘어나는 중앙 여유(안티에일리어싱 포함)
        int size = b * 2 + pad * 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = key,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float half = size * 0.5f;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = OuterDistance(x, y, half, b);          // 외곽 경계까지 부호거리(안=음수)
            float alpha = Mathf.Clamp01(0.5f - d);           // 바깥으로 1px AA
            float lineMix = outline <= 0 ? 0f : Mathf.Clamp01(d + outline + 0.5f); // 가장자리 outline px 는 line 색
            Color c = Color.Lerp(fill, line, lineMix);
            c.a *= alpha;
            px[y * size + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply();

        var rect = new Rect(0, 0, size, size);
        var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        sprite.name = key;
        _cache[key] = sprite;
        return sprite;
    }

    // 채움만(외곽선 없는 라운드) — 칩·리세스 카드 배경 등
    public static Sprite Rounded(int radius, Color fill) => RoundedOutlined(radius, 0, fill, fill);

    // 코너 반경 cornerRadius 를 가진 중앙 정렬 라운드 사각형의 부호거리(SDF).
    static float OuterDistance(int x, int y, float half, int cornerRadius)
    {
        float qx = Mathf.Abs(x + 0.5f - half) - (half - cornerRadius);
        float qy = Mathf.Abs(y + 0.5f - half) - (half - cornerRadius);
        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
        float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outside + inside - cornerRadius;
    }
}
