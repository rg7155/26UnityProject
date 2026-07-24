using UnityEngine;
using UnityEngine.UI;

// 패널 루트에 붙어 자체 Canvas(overrideSorting) 로 렌더 순서를 상수 레이어로 고정한다.
// nested Canvas + overrideSorting 은 자체 GraphicRaycaster 가 있어야 입력이 동작한다.
[RequireComponent(typeof(RectTransform))]
public class UILayerCanvas : MonoBehaviour
{
    [SerializeField] UILayer _layer;

    void Awake() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = (int)_layer;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }
}
