#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// 가상 조이스틱 비주얼 계층을 코드로 생성하고 VirtualJoystick 참조를 자동 배선. 실행: Tools/UI/Build Virtual Joystick
//
// 계층: Canvas > VirtualJoystick(풀스크린 스트레치 + CanvasGroup + VirtualJoystick.cs)
//         ├─ BaseRing (Image + UIProceduralSprite Ring, 반투명 도넛)
//         └─ Knob     (Image + UIProceduralSprite Circle, 노브)
// VirtualJoystick.cs/PlayerController.cs 본문은 건드리지 않고 필드명만 SerializedObject 로 참조.
// 절차 스프라이트는 UIProceduralSprite 로 런타임 재생성(프리팹/씬에 sprite 직접 굽지 않음 → 흰 박스 방지).
// in-place · idempotent(이름 재사용).
public static class JoystickUIGenerator
{
    const float RadiusScreenRatio = 0.12f; // maxRadius = Screen.width * 0.12 (VirtualJoystick 계약과 일치)
    const int TexRadius = 128;             // 절차 텍스처 해상도(표시 크기는 sizeDelta 가 결정)
    const int RingThicknessTex = 20;       // 링 두께(텍스처 px)
    const float KnobDiameterRatio = 0.45f; // 노브 지름 / 베이스 지름

    [MenuItem("Tools/UI/Build Virtual Joystick")]
    public static void BuildVirtualJoystick()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[JoystickUIGenerator] 씬에서 Canvas 를 찾지 못했습니다. GameScene 을 연 상태로 실행하세요.");
            return;
        }
        var canvasRT = (RectTransform)canvas.transform;

        // ── 루트: VirtualJoystick(풀스크린 스트레치) + CanvasGroup ──
        var joyRT = GetOrCreate(canvasRT, "VirtualJoystick");
        Stretch(joyRT);
        var cg = joyRT.GetComponent<CanvasGroup>();
        if (cg == null) cg = joyRT.gameObject.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false; // 조이스틱이 UI 클릭을 먹으면 안 됨(터치는 스크린 폴링)
        cg.alpha = 0f;             // 터치 전까지 숨김(VirtualJoystick 가 페이드 구동)

        var joy = joyRT.GetComponent<VirtualJoystick>();
        if (joy == null) joy = joyRT.gameObject.AddComponent<VirtualJoystick>();

        UILayerAssign.AssignLayer(joyRT.gameObject, UILayer.Hud);

        // ── 크기: 스크린 px 를 캔버스 스케일로 환산(스크린샷 후 미세조정) ──
        float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        float maxRadiusPx = Screen.width * RadiusScreenRatio;
        int ringDiameter = Mathf.Max(1, Mathf.RoundToInt(maxRadiusPx * 2f / scale));
        int knobDiameter = Mathf.Max(1, Mathf.RoundToInt(ringDiameter * KnobDiameterRatio));

        // ── BaseRing (반투명 도넛) ──
        var baseRing = GetOrCreate(joyRT, "BaseRing");
        var ringImg = EnsureImage(baseRing);
        ringImg.raycastTarget = false;
        ringImg.color = Color.white; // 알파는 스프라이트 _fill 에 반영됨
        Center(baseRing);
        baseRing.sizeDelta = new Vector2(ringDiameter, ringDiameter);
        EnsureProcedural(baseRing).ConfigureRing(TexRadius, RingThicknessTex, UITheme.JoystickBase);

        // ── Knob (중앙 노브) ──
        var knob = GetOrCreate(joyRT, "Knob");
        var knobImg = EnsureImage(knob);
        knobImg.raycastTarget = false;
        knobImg.color = Color.white;
        Center(knob);
        knob.sizeDelta = new Vector2(knobDiameter, knobDiameter);
        EnsureProcedural(knob).ConfigureCircle(TexRadius, UITheme.JoystickKnob);

        // ── 참조 자동 배선(드래그 대체) ──
        var so = new SerializedObject(joy);
        so.FindProperty("_baseRing").objectReferenceValue = baseRing;
        so.FindProperty("_knob").objectReferenceValue = knob;
        so.FindProperty("_canvasGroup").objectReferenceValue = cg;
        so.FindProperty("_canvasRect").objectReferenceValue = canvasRT;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(joy);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = joyRT.gameObject;
        Debug.Log("[JoystickUIGenerator] 가상 조이스틱 생성 + 참조 배선 완료.");
    }

    // ── 헬퍼 ──
    static RectTransform GetOrCreate(RectTransform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static Image EnsureImage(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        return img;
    }

    static UIProceduralSprite EnsureProcedural(RectTransform rt)
    {
        var ps = rt.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = rt.gameObject.AddComponent<UIProceduralSprite>();
        return ps;
    }

    static void Center(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
