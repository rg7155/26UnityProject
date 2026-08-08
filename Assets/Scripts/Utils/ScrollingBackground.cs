using UnityEngine;
using UnityEngine.Rendering;

// 카메라 자식 쿼드 2장의 UV 오프셋을 카메라 월드 좌표로 밀어 무한 스크롤 배경을 만든다
// Main Camera에 붙여서 사용
public class ScrollingBackground : MonoBehaviour
{
    [SerializeField] MeshRenderer _baseQuad;
    [SerializeField] MeshRenderer _detailQuad;
    // 두 값은 서로소여야 한다 — 공약수가 생기면 두 레이어가 같은 주기로 겹쳐 반복이 도드라진다
    [SerializeField] float _baseTileSize = 12f;
    [SerializeField] float _detailTileSize = 17f;

    Camera _cam;
    Material _baseMat;
    Material _detailMat;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        float h = _cam.orthographicSize * 2f * 1.1f;  // 1.1배는 화면비 오차 여유
        float w = h * _cam.aspect;

        // localZ 15 → 월드 z +5. 게임플레이(z=0)보다 카메라에서 멀어 깊이로도 뒤에 깔린다
        foreach (MeshRenderer quad in new[] { _baseQuad, _detailQuad })
        {
            quad.transform.localPosition = new Vector3(0f, 0f, 15f);
            quad.transform.localScale = new Vector3(w, h, 1f);
        }

        // sortingOrder 는 깊이보다 우선한다 — 기본값 0 이면 음수 Order 인 보스 예고원(-20/-19)을
        // 통째로 가린다. MeshRenderer 는 Inspector 에 이 필드가 없어 코드로만 지정할 수 있다.
        _baseQuad.sortingOrder   = -100;
        _detailQuad.sortingOrder = -99;

        // sharedMaterial을 쓰면 에디터에서 .mat 에셋이 오염된다
        _baseMat = _baseQuad.material;
        _detailMat = _detailQuad.material;
        _baseMat.mainTextureScale = new Vector2(w / _baseTileSize, h / _baseTileSize);
        _detailMat.mainTextureScale = new Vector2(w / _detailTileSize, h / _detailTileSize);
    }

    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

    void OnDestroy()
    {
        if (_baseMat != null) Destroy(_baseMat);
        if (_detailMat != null) Destroy(_detailMat);
    }

    // LateUpdate 실행 순서에 의존하지 않도록, 카메라 위치가 확정된 렌더 직전에 갱신한다
    void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != _cam) return;

        Vector3 p = transform.position;
        _baseMat.mainTextureOffset = WrapOffset(p, _baseTileSize);
        _detailMat.mainTextureOffset = WrapOffset(p, _detailTileSize);
    }

    // 장시간 플레이 시 오프셋이 커져 float 정밀도가 무너지는 것을 막는다
    Vector2 WrapOffset(Vector3 p, float tileSize)
    {
        return new Vector2(
            Mathf.Repeat(p.x / tileSize, 1f),
            Mathf.Repeat(p.y / tileSize, 1f));
    }
}
