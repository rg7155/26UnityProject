## 기능: 무한 스크롤 배경 (GL.LINES 그리드 → 타일 텍스처 2겹)

카메라에 붙인 쿼드 2장(베이스 + 디테일)의 머티리얼 UV 오프셋을 카메라 월드 좌표로 밀어
무한 스크롤 배경을 만든다. 타일 오브젝트 풀링·Tilemap 사용 안 함.

---

## 0. 코드베이스 조사 결과

| 확인한 것 | 결과 | 계획에 미치는 영향 |
|---|---|---|
| `Assets/Scripts/Utils/GridBackground.cs` | Main Camera에 붙은 컴포넌트. `RenderPipelineManager.endCameraRendering`에 구독해 `GL.LINES`로 매 프레임 격자를 직접 그림. 머티리얼은 `Hidden/Internal-Colored` 런타임 생성 | 교체 대상. **씬에서 컴포넌트만 Remove**(GameScene.unity L3917~3931에 인스턴스 존재), `.cs` 파일은 남긴다. 구독 해제는 `OnDisable`에 있으므로 컴포넌트 제거만으로 잔여 렌더 없음 |
| `Assets/Scripts/Controllers/CameraController.cs` | `LateUpdate`에서 `Vector3.Lerp` 추적 + 사인파 셰이크로 `transform.position` 확정 | 배경 UV 오프셋을 `LateUpdate`에서 계산하면 실행 순서에 따라 1프레임 밀림/떨림 발생. **아래 "실행 순서 보장" 항목 참조** |
| GameScene Main Camera | `orthographic: 1`, `orthographic size: 10`, position `(0,0,-10)`, `m_ClearFlags: 2`(Solid Color), 배경색 `#0A0A14`(a=0), near 0.3 / far 1000, **씬 루트에 배치**(`m_Father: 0`) | 세로 화면 기준 가시 영역 = 높이 20, 폭 20×aspect(1080×1920 → 11.25). 쿼드는 카메라 자식으로 두므로 새 루트 오브젝트가 생기지 않음 → AGENTS.md 계층 규칙과 충돌 없음. Clear Flags는 Solid Color 유지(쿼드가 화면을 다 덮으므로 무관, 여백 시 검정) |
| 적 렌더링 정렬 기준 (`EnemyInstanceRenderer.cs`) | `LateUpdate`에서 `Graphics.DrawMeshInstanced(_mesh, 0, _material, ...)`. 씬의 Renderer_Basic이 쓰는 머티리얼 = `Mat_Enemy_Basic_Sprite`(guid `cb39975…`), 셰이더 = **URP `Universal Render Pipeline/Unlit`**(guid `650dd952…`), `_SURFACE_TYPE_TRANSPARENT`, **`m_CustomRenderQueue: 3000`**. 메시는 빌트인 Quad(`fileID: 10210`) | `DrawMeshInstanced`는 SpriteRenderer가 아니므로 **sortingOrder가 통하지 않는다**(sortingLayer는 렌더러 컴포넌트 속성). 정렬은 **renderQueue + 깊이(z)** 로만 제어해야 함 → 아래 "그리기 순서" 항목 |
| 다른 게임플레이 오브젝트 | Player `m_SortingOrder: 10`, 보스 `-20/-19`, 보스탄 `8` — 전부 SpriteRenderer(기본 Transparent 큐 3000대) | 배경이 큐 3000 이전(불투명 2000대)에 있으면 sortingOrder 값과 무관하게 항상 뒤에 그려짐 |
| URP 설정 | `Assets/Settings/Renderer2D.asset` 사용(2D Renderer). 적 머티리얼이 이미 `URP/Unlit`을 2D Renderer에서 정상 사용 중 | 배경도 동일하게 **`Universal Render Pipeline/Unlit`** 사용. 프로젝트에 이미 쓰이는 셰이더라 검증 리스크 없음 |
| Global Light 2D | GameScene에 존재(`m_Name: Global Light 2D`) | `URP/Unlit`은 Light2D 영향을 **받지 않는다**. 배경 밝기가 라이팅 설정에 흔들리지 않으므로 이 조합을 택한다(Sprite-Lit 계열 사용 안 함) |
| RewindVolume | 씬에 `Volume` + `RewindVolumeController` | 포스트프로세스는 화면 전체에 적용되므로 배경도 자동으로 되감기 연출을 받는다. 배경 쪽에 별도 처리 불필요 |

### 그리기 순서 — 결론
- **sortingOrder는 쓰지 않는다.** 적이 `DrawMeshInstanced`로 그려져 sortingOrder/ sortingLayer가 적용되지 않기 때문.
- **베이스 쿼드**: `URP/Unlit`, Surface Type = **Opaque**(큐 2000). 불투명은 모든 Transparent(3000)보다 항상 먼저 그려지므로 어떤 게임플레이 오브젝트보다 확실히 뒤.
- **디테일 쿼드**: 알파/가산 합성이 필요하므로 Transparent. 머티리얼 인스펙터에서 **Render Queue를 `2900`으로 수동 지정**(Advanced > Sorting Priority가 아니라 Custom Render Queue). 큐 2900 < 3000 이므로 적·플레이어·탄보다 항상 먼저 그려짐.
- **z 위치는 보조 수단**: 쿼드를 카메라 자식으로 두고 localZ = **15**(월드 z = +5). 게임플레이는 z=0이므로 카메라(z=-10)에서 더 멀다. 불투명 베이스가 깊이를 기록하지만 게임플레이가 더 가까워 깊이 테스트를 통과한다.
- 근거 요약: **큐로 확정, z로 이중 안전.** sortingOrder는 이 파이프라인에서 신뢰할 수 없다.

### 실행 순서 보장 — 결론
- **쿼드 위치는 코드로 갱신하지 않는다.** 쿼드를 Main Camera의 자식으로 두면 카메라 트랜스폼 변경이 계층을 통해 즉시 반영되어 원천적으로 한 프레임 밀림이 없다(셰이크 포함).
- **UV 오프셋만** 매 프레임 카메라 월드 좌표에서 계산하며, 이때 `LateUpdate`를 쓰지 않고 **`RenderPipelineManager.beginCameraRendering`** 에 구독한다.
  - 근거: 이 콜백은 모든 `Update`/`LateUpdate`가 끝난 뒤, 해당 카메라를 실제로 렌더링하기 직전에 호출된다. 즉 `CameraController.LateUpdate`가 확정한 최종 위치를 읽는 것이 보장되며, Script Execution Order 설정이나 `LateUpdate` 호출 순서에 의존하지 않는다.
  - 기존 `GridBackground`가 같은 계열 훅(`endCameraRendering`)을 이미 쓰고 있어 프로젝트 내 검증된 패턴이다.
  - `cam != _cam` 이면 즉시 return (씬뷰 카메라 대응).

---

## 1. 파일별 변경·생성 목록

| 경로 | 종류 | 역할 | 예상 줄 수 |
|---|---|---|---|
| `Assets/Scripts/Utils/ScrollingBackground.cs` | **신규** | Main Camera에 붙는 컴포넌트. Awake에서 쿼드 스케일·타일링 계산, `beginCameraRendering`에서 두 머티리얼의 UV 오프셋 갱신 | **약 55줄** |
| `Assets/Scenes/GameScene.unity` | 수정(에디터 수동) | Main Camera에서 `GridBackground` 제거, `ScrollingBackground` 추가, 자식 쿼드 2개 생성 | — |
| `Assets/Material/Mat_BG_Base.mat` | 신규(에디터 수동) | `URP/Unlit`, Opaque, 베이스 타일 텍스처 | — |
| `Assets/Material/Mat_BG_Detail.mat` | 신규(에디터 수동) | `URP/Unlit`, Transparent + Additive, Render Queue 2900, 디테일 타일 텍스처 | — |
| `Assets/Art/BG/BG_Base.png` | 신규(사용자 GPT 생성) | 1024×1024 seamless 베이스 타일 | — |
| `Assets/Art/BG/BG_Detail.png` | 신규(사용자 GPT 생성) | 1024×1024 seamless 디테일 타일(검은 배경) | — |
| `Assets/Scripts/Utils/GridBackground.cs` | **손대지 않음** | 파일 유지, 씬에서 컴포넌트만 제거 | — |

### 에디터 생성기를 만들지 않는 이유 (결정)
쿼드 스케일·타일링·localZ는 **런타임 스크립트가 Awake에서 직접 계산**하므로, 수동 작업은
"Quad 2개 생성 → 카메라 자식으로 → 머티리얼 드래그 → 스크립트에 MeshRenderer 2개 드래그"가 전부다.
1회성 60줄짜리 `[MenuItem]` 생성기를 추가하는 것은 AGENTS.md의 "요청하지 않은 것 구현 금지"에 어긋난다.
**수동 배치로 진행**한다(3-3 체크리스트).

### `ScrollingBackground.cs` 설계 (구현은 Coder 단계)
```
[SerializeField] MeshRenderer _baseQuad;      // 카메라 자식 → Inspector 드래그 허용
[SerializeField] MeshRenderer _detailQuad;
[SerializeField] float _baseTileSize   = 6f;  // 타일 1장이 덮는 월드 단위
[SerializeField] float _detailTileSize = 9f;
[SerializeField] float _detailScrollRate = 0.85f;  // 1보다 작으면 뒤로 밀리는 미세 패럴랙스
```
- `Awake`: `_cam = GetComponent<Camera>()`, 가시 높이 `h = _cam.orthographicSize * 2f * 1.1f`,
  폭 `w = h * _cam.aspect`, 두 쿼드의 `localPosition = (0,0,15)`, `localScale = (w,h,1)`,
  `_baseMat = _baseQuad.material` / `_detailMat = _detailQuad.material`(에셋 오염 방지 — `sharedMaterial` 금지),
  각 머티리얼 `mainTextureScale = (w/타일크기, h/타일크기)`.
- `OnEnable`/`OnDisable`: `RenderPipelineManager.beginCameraRendering` 구독/해제.
- 콜백: `if (cam != _cam) return;` → `Vector2 p = transform.position;`
  `_baseMat.mainTextureOffset   = p / _baseTileSize;`
  `_detailMat.mainTextureOffset = p * _detailScrollRate / _detailTileSize;`
- 1.1배 여유는 셰이크 여유가 아니라(쿼드가 카메라 자식이라 셰이크는 함께 움직임) 화면비 오차 대비.
- 필드는 `_camelCase`, 메서드는 `PascalCase`. 인터페이스·제네릭·설정 자산 없음.

---

## 2. 단계별 구현 순서

각 단계는 독립적으로 확인 가능하다. 앞 단계 검증 전에 다음 단계로 넘어가지 않는다.

### 1단계 — 임시 텍스처로 파이프라인 검증 (아트 없이)
- 사용자가 아직 GPT 이미지를 안 만들었어도 진행 가능. Unity 빌트인 흰 텍스처 대신
  임시로 아무 사각형 PNG(또는 텍스처 없이 `_BaseColor`만 다른 색)로 확인.
- `ScrollingBackground.cs` 작성 → Main Camera에 부착 → 쿼드 2개 생성 → 머티리얼 연결.
- **검증 기준(에디터):**
  - Play 시 화면 전체가 베이스 쿼드 색으로 덮인다(위/아래 검은 여백 없음 — 특히 게임뷰를 1080×1920으로 두고 확인).
  - Scene 뷰에서 쿼드가 Main Camera 자식이고 월드 z = +5.
  - 적·플레이어·투사체·데미지 텍스트가 **모두 배경 위에** 보인다(하나라도 가려지면 0장 "그리기 순서" 재확인).
  - 콘솔 에러 0.

### 2단계 — GridBackground 제거
- Main Camera Inspector에서 `GridBackground` 컴포넌트 Remove Component. 스크립트 파일은 삭제하지 않음.
- **검증 기준:** Play 시 회색 격자선이 더 이상 보이지 않는다. 배경 쿼드만 남는다. 콘솔 에러 0.

### 3단계 — 베이스 타일 텍스처 적용 + 스크롤 확인
- 3-1의 임포트 설정대로 `BG_Base.png` 임포트 → `Mat_BG_Base`의 Base Map에 연결.
- **검증 기준:**
  - 플레이어를 이동시키면 배경이 **반대 방향으로 정확히 같은 속도로** 흐른다(월드에 고정된 느낌).
  - 제자리에 서면 배경이 완전히 멈춘다(오프셋 누적 드리프트 없음).
  - 대각선으로 오래 이동해도 타일 이음매에 밝은 선/어긋남이 안 보인다 → 보이면 4장 seam 대책.
  - 카메라 셰이크(보스 등장) 시 배경이 화면과 **함께** 흔들리고 배경만 따로 떨리지 않는다.

### 4단계 — 디테일 레이어 추가 (미세 패럴랙스)
- `Mat_BG_Detail` 생성: Surface Type = Transparent, Blending Mode = **Additive**, Render Queue = **2900**,
  Base Color 밝기 0.3~0.5로 낮춤. `_detailTileSize`를 베이스와 다른 값(예: 9), `_detailScrollRate` 0.85.
- **검증 기준:**
  - 이동 시 디테일이 베이스보다 **느리게** 흘러 깊이감이 생긴다.
  - 디테일 텍스처의 검은 부분이 (가산 합성 덕에) 완전히 투명하게 보인다 — 사각형 경계가 안 보인다.
  - 디테일이 적/플레이어를 절대 가리지 않는다(가리면 Render Queue 2900 확인).
  - 두 레이어의 반복 주기가 서로 달라 "같은 무늬가 규칙적으로 돌아오는" 느낌이 완화됐는지 눈으로 확인.

### 5단계 — 밸런싱·마감
- `_baseTileSize` / `_detailTileSize` / 디테일 밝기를 인스펙터에서 조정(타일이 너무 작으면 촘촘해서 어지럽고, 너무 크면 반복이 눈에 띈다. 세로 화면 폭 11.25 기준 **타일 크기 5~9**가 적정).
- **검증 기준:** 실제 플레이 30초 동안 배경이 게임플레이 가독성을 해치지 않는다(적 실루엣이 배경에 묻히지 않음). 프로파일러에서 배경 추가로 인한 SetPass Call 증가가 2 이하.

---

## 3. 사용자 수동 작업 체크리스트

### 3-1. GPT 이미지 생성 프롬프트 (그대로 복사해 사용)

**중요:** GPT 이미지 생성기는 **완벽한 seamless를 보장하지 못한다.** 반드시 3-2의 검증을 거치고,
실패하면 4장의 우회책(Mirror 랩)으로 간다. 프롬프트에 "seamless"를 넣는 것은 성공률을 올릴 뿐이다.

**① 베이스 타일 (`BG_Base.png`)**
```
A seamless tileable square texture, 1024x1024, top-down flat view.
Dark cyber grid: near-black background (#10131C) with thin, evenly spaced
cyan (#35D9F5) grid lines at very low opacity, plus faint darker panel
seams. Perfectly flat lighting — no vignette, no glow hotspots, no shadows,
no gradient across the image, no border or frame. Uniform detail density
across the whole square so the pattern repeats invisibly. The pattern must
continue exactly across all four edges (left edge matches right edge, top
edge matches bottom edge). Very low contrast, subtle, meant to sit behind
gameplay characters. No text, no watermark, no logo, no perspective.
```

**② 디테일 타일 (`BG_Detail.png`) — 검은 배경 위에 밝은 요소만**
```
A seamless tileable square texture, 1024x1024, top-down flat view.
Pure black (#000000) background with sparse, small glowing cyan (#35D9F5)
circuit traces, tiny dots, and short hex fragments scattered irregularly.
Everything except the glowing elements must be pure black. Flat, no
vignette, no large blobs, no bright cluster in the center, no border or
frame. Elements must not touch or cross the image edges awkwardly — the
pattern must continue exactly across all four edges. Sparse: roughly 10%
coverage. No text, no watermark, no logo, no perspective.
```
- 배경이 순수 검정이면 **가산 합성(Additive)** 으로 알파 채널 없이도 검은 부분이 투명해진다.
  GPT가 알파(투명 배경) PNG를 안정적으로 못 만들기 때문에 이 방식을 택한다.
- 결과물이 1024가 아니면 이미지 편집기에서 **1024×1024로 리사이즈**(POT). 압축 포맷과 정확한 Repeat를 위해 필요.

### 3-2. seam(이음매) 검증 방법
아래 A를 먼저 하고, 의심되면 B로 확인한다.

- **A. Unity 안에서 (권장, 가장 빠름)**
  1. `Mat_BG_Base`의 Base Map Tiling을 임시로 `(4, 4)`로 설정.
  2. Scene 뷰에서 쿼드를 확대해 본다.
  3. 4×4 격자 경계에 **밝기 차이가 있는 직선**이나 무늬 끊김이 보이면 seam 실패.
  4. 확인 후 Tiling은 되돌린다(런타임에 스크립트가 다시 계산하므로 값 자체는 무관).
- **B. 이미지 편집기에서 (정밀)**
  - GIMP: `Filters > Map > Offset`에서 X, Y를 각각 이미지 절반(512)으로 오프셋(Wrap 옵션).
    원래 가장자리였던 부분이 이미지 **중앙에 십자로** 오게 되는데, 여기서 선이 보이면 seam 실패.
  - Photoshop: `Filter > Other > Offset`, Horizontal/Vertical 512, Wrap Around. 판정 동일.
- **판정 기준:** 게임 화면(1080×1920, 타일 크기 6 월드유닛 ≈ 화면 폭에 2장)에서 30cm 거리로 봤을 때
  seam이 인지되지 않으면 통과. 픽셀 단위로 완벽할 필요 없다.

### 3-3. 텍스처 임포트 설정 (두 PNG 모두)
| 항목 | 값 | 이유 |
|---|---|---|
| Texture Type | **Default** | Sprite로 두면 아틀라스/9-slice 경로를 타서 타일링이 깨진다 |
| Texture Shape | 2D | |
| Wrap Mode | **Repeat** (seam 실패 시 **Mirror**) | UV 오프셋 무한 스크롤의 전제 |
| Filter Mode | Bilinear | |
| Generate Mip Maps | **On** | 세로 화면에서 타일이 축소될 때 지글거림(shimmering) 방지 |
| Aniso Level | 1 | 탑다운 2D라 비스듬한 시야 없음 |
| sRGB (Color Texture) | On | |
| Alpha Is Transparency | Off (가산 합성이라 알파 미사용) | |
| Non-Power of 2 | ToNearest (이미 1024면 무관) | 압축 포맷 요구사항 |
| Max Size | 1024 | |
| Compression / Format | Default(Android: ASTC 6x6) | 1024 POT라 압축 가능 |

### 3-4. 머티리얼 생성
1. `Assets/Material/Mat_BG_Base` 생성 → Shader **`Universal Render Pipeline/Unlit`**
   → Surface Type **Opaque** → Base Map에 `BG_Base` 연결 → Base Color 흰색.
2. `Assets/Material/Mat_BG_Detail` 생성 → 같은 셰이더
   → Surface Type **Transparent**, Blending Mode **Additive**
   → 인스펙터 우측 상단 톱니 > **Custom Render Queue = 2900** (Advanced에서 Render Queue를 Custom으로)
   → Base Map에 `BG_Detail` 연결 → Base Color 밝기 0.4 정도로 낮춤.
3. 두 머티리얼 모두 **Enable GPU Instancing은 불필요**(각 1장).

### 3-5. 씬 배치 (GameScene)
1. Hierarchy에서 **Main Camera** 선택 → Inspector에서 `GridBackground` **Remove Component**.
2. Main Camera 우클릭 > 3D Object > **Quad** 2개 생성(자동으로 카메라 자식이 된다).
   이름 `BG_Base`, `BG_Detail`.
3. 두 쿼드의 **MeshCollider 컴포넌트를 제거**(Quad 기본 포함 — 물리 오염 방지).
4. 두 쿼드의 MeshRenderer에서 **Cast Shadows = Off, Receive Shadows 해제**.
5. 각 쿼드 Material 슬롯에 `Mat_BG_Base` / `Mat_BG_Detail` 연결.
6. Main Camera에 `ScrollingBackground` 컴포넌트 추가 → `_baseQuad`, `_detailQuad` 슬롯에
   방금 만든 두 쿼드를 드래그(같은 오브젝트 계층 내부이므로 AGENTS.md의 Inspector 드래그 허용 범위).
7. localPosition/localScale은 손대지 않아도 된다 — Awake에서 스크립트가 덮어쓴다.
8. 씬 저장. 새 루트 오브젝트를 만들지 않으므로 `--- Managers ---` / `--- World ---` / `--- UI ---` 구조는 그대로다.

---

## 4. 리스크와 대안

| 리스크 | 증상 | 대안 |
|---|---|---|
| **GPT 이미지가 seamless가 아님** | 이동 시 일정 간격으로 밝은 선/무늬 끊김 | **1순위: Wrap Mode를 `Mirror`로 변경.** 미러 타일링은 가장자리를 반사해 이음매가 수학적으로 항상 맞는다. 대칭 무늬가 생기지만 어두운 저대비 배경이라 거의 안 보인다. **2순위:** 이미지를 중앙에서 크롭(가장자리 20% 버림) 후 다시 검증. **3순위:** 베이스 레이어를 GPT 대신 **절차적 그리드 텍스처**(수학적으로 완벽한 seamless)로 대체하고, GPT 이미지는 디테일 레이어에만 사용 |
| **반복감이 여전히 눈에 띔** | 같은 무늬가 규칙적으로 돌아옴 | ① `_baseTileSize`와 `_detailTileSize`를 **서로 나누어떨어지지 않는 값**으로(예: 6 / 9.7) → 두 레이어의 겹침 주기가 길어짐. ② `_detailScrollRate`를 1에서 더 떨어뜨림(0.7). ③ 디테일 텍스처를 더 sparse하게 재생성 — 밀도가 낮을수록 반복 인지가 늦다. ④ 그래도 부족하면 디테일 쿼드를 회전(z축 30도)시켜 격자 방향을 어긋나게 함 |
| **성능 — 풀스크린 오버드로우 2장** | 저사양 모바일에서 프레임 하락 | 베이스가 Opaque라 실질 비용은 디테일 1장의 알파 블렌딩뿐. 문제가 되면 ① 디테일 쿼드를 화면의 일부만 덮게 하거나, ② 두 텍스처를 한 셰이더에서 샘플링하는 커스텀 Unlit 셰이더로 통합(드로우콜 2→1). **선제적으로 하지 않는다** — 프로파일러에서 실제 병목으로 확인된 뒤에만 |
| **배경이 게임플레이를 가림** | 적/투사체가 배경 뒤로 사라짐 | Render Queue 확인(베이스 2000 Opaque, 디테일 **2900**). 이 프로젝트의 적은 `DrawMeshInstanced`라 **sortingOrder로는 해결되지 않는다** — 반드시 큐로 잡을 것 |
| **적 실루엣이 배경에 묻힘** | 가독성 저하 | 디테일 Base Color 밝기를 낮추고(0.4 → 0.25), 베이스 그리드 대비를 낮춘다. 배경은 눈에 띄면 실패다 |
| **게임뷰 화면비를 바꾸면 여백** | 위/아래 검은 띠 | 스케일을 `Awake`에서만 계산하므로 화면비 변경 후 Play 재진입 필요. 실기기는 화면비가 고정이라 문제되지 않음. 에디터에서 자주 바꾼다면 여유 배율 1.1 → 1.3으로 올리는 것으로 충분 |
| **머티리얼 에셋 오염** | 에디터에서 Play 종료 후 `.mat` 파일이 변경됨 | 스크립트는 반드시 `renderer.material`(런타임 인스턴스)을 쓰고 `sharedMaterial`은 쓰지 않는다 |

---

## 주의사항 (AGENTS.md 제약)

- 필드 `_camelCase`, `[SerializeField] float _baseTileSize` 형태, 메서드 `PascalCase`.
- 인터페이스/제네릭 추상화 없음. `BackgroundLayer` 같은 클래스 분리 금지 — 레이어 2개는 필드 2개로 충분.
- ScriptableObject 설정 자산 만들지 않음. 값은 인스펙터 직접 노출.
- Script Execution Order 프로젝트 설정을 건드리지 않는다(`beginCameraRendering`로 해결).
- `GridBackground.cs` 파일은 **삭제하지 않는다**(사용자 판단). 씬에서 컴포넌트만 제거.
- 이번 작업 범위는 **GameScene 한정**. Title/Result 씬 배경은 건드리지 않는다.
- 커밋 시 `개발_진행상황.md` 갱신 필요(배경 시스템 항목). 아키텍처 변경 수준은 아니므로 `아키텍처.md`는 선택.
