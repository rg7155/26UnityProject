using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// URP 환경에서 동작하는 그리드 배경
// Main Camera에 붙여서 사용
public class GridBackground : MonoBehaviour
{
    [SerializeField] float _cellSize = 1f;
    [SerializeField] int _gridRange = 30;
    [SerializeField] Color _lineColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    Material _lineMaterial;
    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnDestroy()
    {
        if (_lineMaterial != null)
            Destroy(_lineMaterial);
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != _cam) return;
        DrawGrid();
    }

    void DrawGrid()
    {
        _lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadProjectionMatrix(_cam.projectionMatrix);
        GL.modelview = _cam.worldToCameraMatrix;

        GL.Begin(GL.LINES);
        GL.Color(_lineColor);

        Vector3 camPos = _cam.transform.position;
        float startX = Mathf.Floor((camPos.x - _gridRange) / _cellSize) * _cellSize;
        float startY = Mathf.Floor((camPos.y - _gridRange) / _cellSize) * _cellSize;

        for (float x = startX; x <= camPos.x + _gridRange; x += _cellSize)
        {
            GL.Vertex3(x, camPos.y - _gridRange, 0);
            GL.Vertex3(x, camPos.y + _gridRange, 0);
        }

        for (float y = startY; y <= camPos.y + _gridRange; y += _cellSize)
        {
            GL.Vertex3(camPos.x - _gridRange, y, 0);
            GL.Vertex3(camPos.x + _gridRange, y, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}
