using UnityEngine;

// 부착된 RectTransform 을 Screen.safeArea(노치/상태바/홈 인디케이터 제외 영역)로 인셋.
// 안전 영역 비율로 anchorMin/Max 를 세팅하고 offset 을 0 으로 맞춰 안전 영역을 꽉 채운다.
// 해상도/노치가 바뀌면(회전·시뮬레이터 전환) 다시 적용. 최소 구현 — 별도 설정/유연성 없음.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    Rect _lastSafeArea = Rect.zero;
    Vector2Int _lastScreen = Vector2Int.zero;

    void OnEnable() => Apply();

    void Update()
    {
        if (Screen.safeArea != _lastSafeArea || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        int w = Screen.width, h = Screen.height;
        if (w <= 0 || h <= 0) return;

        Rect safe = Screen.safeArea;
        Vector2 anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
        Vector2 anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);

        var rt = (RectTransform)transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _lastSafeArea = safe;
        _lastScreen = new Vector2Int(w, h);
    }
}
