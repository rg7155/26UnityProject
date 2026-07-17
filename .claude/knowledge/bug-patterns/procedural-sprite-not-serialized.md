---
title: 코드로 생성한 Texture2D/Sprite를 프리팹·씬에 구우면 직렬화 안 돼 런타임에 null(흰 박스)
tags: [ui, ugui, sprite, texture2d, procedural, serialization, prefab, editor-script, ui-kit]
symptom: 에디터 스크립트에서 UISpriteFactory 등으로 만든 절차적 스프라이트를 Image.sprite에 굽고 프리팹 저장 → 런타임 Instantiate 시 흰 박스로 뜨고, 흰 라벨이 안 보이다가 호버/클릭해야 보임
severity: high
commit: f56efd4
files: [Assets/Scripts/UI/UIProceduralSprite.cs, Assets/Scripts/Editor/ShopUIGenerator.cs, Assets/Scripts/UI/UISpriteFactory.cs]
---

## 증상
상점 셀을 절차적 스프라이트로 리스킨(라운드 카드)했는데, 플레이로 상점을 열면
셀이 **흰색 박스**로 뜬다. 라벨은 흰색(`TextPrimary`)이라 흰 카드 위에서 안 보이고,
**마우스를 올리거나 클릭해야** 그제서야 텍스트가 나타난다.
씬에 배치된 팝업 본체는 같은 세션에선 멀쩡히 둥글게 보여서 더 헷갈린다.

## 근본 원인
`UISpriteFactory`가 `new Texture2D(...)` + `Sprite.Create`로 만든 스프라이트는
**메모리 상 임시 객체(디스크 에셋 아님)**다. 이걸 에디터 스크립트에서
`Image.sprite`에 직접 대입하고 `PrefabUtility.SaveAsPrefabAsset`으로 저장하면,
텍스처가 에셋이 아니라 **직렬화되지 못하고 sprite 참조가 null로 저장**된다.
→ 런타임 `Instantiate(prefab)`한 Image는 `sprite=null` → Unity 기본 **흰색 쿼드**.
→ 흰 카드 위 흰 라벨 = 안 보임. 호버 시 Button 상태전환이 캔버스를 강제 리빌드해
   그제서야 TMP 메시가 갱신되어 잠깐 보이는 것처럼 느껴진다.

씬 오브젝트가 같은 세션에서 멀쩡한 건 **살아있는 메모리 참조가 유지**돼서일 뿐,
씬을 닫았다 열거나 도메인 리로드하면 동일하게 깨진다.

## 해결
절차적 스프라이트를 프리팹/씬에 **굽지 말고, 런타임에 재생성해 적용**한다.
`UIProceduralSprite` 컴포넌트(`[ExecuteAlways]` + `[RequireComponent(typeof(Image))]`):
```csharp
[SerializeField] bool _outlined;
[SerializeField] int _radius, _outlineWidth;
[SerializeField] Color _fill, _line;

void OnEnable() => Apply();
void Apply()
{
    var img = GetComponent<Image>();
    img.sprite = _outlined
        ? UISpriteFactory.RoundedOutlined(_radius, _outlineWidth, _fill, _line)
        : UISpriteFactory.Rounded(_radius, _fill);
    img.type = Image.Type.Sliced;
}
```
저장되는 것은 **파라미터(radius/color 등)뿐**이고 스프라이트는 매 실행 재생성되므로
Instantiate·씬 재오픈·도메인 리로드 모두에서 산다. 에디터 생성기는 `Image.sprite`를
직접 세팅하는 대신 이 컴포넌트를 `GetOrAdd`하고 파라미터만 넣는다.

## 재인식 패턴 ⚠️
**"에디터/코드에서 만든 Texture2D·Sprite·Mesh 등 런타임 생성 UnityEngine.Object를
프리팹/씬에 대입해 저장"** 이 보이면 직렬화 안 됨을 의심.
→ 저장 시 참조가 null로 떨어져 런타임에 흰 박스/누락으로 나타난다.
→ 해결: **에셋으로 저장**(`AssetDatabase.CreateAsset`)하거나, 더 낫게는
   **런타임 컴포넌트가 `OnEnable`에서 재생성**하도록 한다(파라미터만 직렬화).
"흰 박스 + 호버해야 보임"은 sprite=null + 동색 텍스트 + 캔버스 리빌드 지연의 조합 신호.

## 관련
- ui-kit 스킬: 절차적 스프라이트는 런타임 적용이 원칙 (`references/ugui-recipes.md`).
- 매직값 대신 `UITheme` 토큰을 컴포넌트 파라미터로 주입.
