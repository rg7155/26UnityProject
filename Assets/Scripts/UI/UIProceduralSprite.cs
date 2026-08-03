using UnityEngine;
using UnityEngine.UI;

// 절차적 스프라이트를 런타임에 재생성해 Image 에 꽂는 컴포넌트.
// UISpriteFactory 가 만드는 Texture2D 는 에셋이 아니라 메모리 임시 객체라, 프리팹/씬에 sprite 를
// 직접 구워 저장하면 참조가 null 로 직렬화된다(→ 흰 박스). 파라미터만 직렬화하고 sprite 는
// OnEnable 때 매번 팩토리에서 다시 만들어 채운다. 모든 스타일 Image 가 공유하므로 추상화가 정당하다.
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class UIProceduralSprite : MonoBehaviour
{
    public enum Shape { Rounded, Circle, Ring, EdgeGlow } // 새 값은 항상 뒤에 추가 — 기존 씬의 직렬화 인덱스 보존

    [SerializeField] Shape _shape = Shape.Rounded;
    [SerializeField] bool _outlined;
    [SerializeField] int _radius;
    [SerializeField] int _outlineWidth;
    [SerializeField] int _thickness;            // Ring 두께(_shape == Ring 일 때만)
    [SerializeField] Color _fill = Color.white;
    [SerializeField] Color _line = Color.black; // _outlined 가 false 면 무시
    [SerializeField] bool _preserveType;        // true 면 Image.type 을 덮지 않음(예: Filled 링 유지)

    void OnEnable() => Apply();

    // 라운드 사각형(상점/업그레이드 패널). 기존 시그니처 유지.
    public void Configure(bool outlined, int radius, int outlineWidth, Color fill, Color line)
    {
        _shape = Shape.Rounded;
        _outlined = outlined;
        _radius = radius;
        _outlineWidth = outlineWidth;
        _fill = fill;
        _line = line;
        Apply();
    }

    // 꽉 찬 원(노브).
    public void ConfigureCircle(int radius, Color fill)
    {
        _shape = Shape.Circle;
        _radius = radius;
        _fill = fill;
        Apply();
    }

    // 도넛(베이스 링).
    public void ConfigureRing(int outerRadius, int thickness, Color fill)
        => ConfigureRing(outerRadius, thickness, fill, false);

    // preserveType=true: Image.type 을 건드리지 않는다(생성기가 Filled/Radial360 등을 유지하려는 경우).
    public void ConfigureRing(int outerRadius, int thickness, Color fill, bool preserveType)
    {
        _shape = Shape.Ring;
        _radius = outerRadius;
        _thickness = thickness;
        _fill = fill;
        _preserveType = preserveType;
        Apply();
    }

    // 화면 테두리 발광(피격 비네트). band 는 테두리 두께 px — _thickness 를 재사용한다.
    public void ConfigureEdgeGlow(int band, Color fill)
    {
        _shape = Shape.EdgeGlow;
        _thickness = band;
        _fill = fill;
        Apply();
    }

    void Apply()
    {
        var img = GetComponent<Image>();
        if (img == null) return;
        Image.Type type;
        switch (_shape)
        {
            case Shape.EdgeGlow:
                img.sprite = UISpriteFactory.EdgeGlow(_thickness, _fill);
                type = Image.Type.Sliced;
                break;
            case Shape.Circle:
                img.sprite = UISpriteFactory.Circle(_radius, _fill);
                type = Image.Type.Simple;
                break;
            case Shape.Ring:
                img.sprite = UISpriteFactory.Ring(_radius, _thickness, _fill);
                type = Image.Type.Simple;
                break;
            default:
                img.sprite = _outlined
                    ? UISpriteFactory.RoundedOutlined(_radius, _outlineWidth, _fill, _line)
                    : UISpriteFactory.Rounded(_radius, _fill);
                type = Image.Type.Sliced;
                break;
        }
        if (!_preserveType) img.type = type; // Filled 등 커스텀 type 을 생성기가 세팅한 경우 유지
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 변경 시 프리뷰 갱신. OnValidate 프레임에 직접 sprite 를 건드리면 경고가 날 수 있어 지연 적용.
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
