using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// SpriteFrameAnimator 의 프레임 배열을 스프라이트 시트에서 자동 배선한다.
// 실행: Tools/Art/Wire Sprite Animators
//
// 에이전트는 Unity 에디터를 드래그로 만질 수 없다(ui-kit SKILL.md 와 같은 전제).
// 프레임 배열을 손으로 끌어다 넣는 대신, 대상 텍스처에서 서브스프라이트를 이름 순으로 뽑아 채운다.
public static class SpriteAnimatorWiring
{
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string PlayerVisualName = "PlayerVisual";
    const string PlayerHoverThrusterTexturePath = "Assets/Art/Sprites/Generated/PlayerHoverThruster.png";
    const string PlayerHoverThrusterName = "HoverThruster";
    const int PlayerSortingOrder = 10;

    // 프리팹과 Generated 텍스처 경로. art-gen 스킬의 애니메이션 대상과 짝을 이룬다.
    static readonly Dictionary<string, string> Targets = new Dictionary<string, string>
    {
        { PlayerPrefabPath, "Assets/Art/Sprites/Generated/PlayerEnergyCoreSymmetric.png" },
        { "Assets/Prefabs/Enemy/Boss_Monolith.prefab", "Assets/Art/Sprites/Generated/BossMonolithSymmetric.png" },
        { "Assets/Prefabs/Weapon/Orbiter.prefab", "Assets/Art/Sprites/Generated/OrbiterEnergyBlade.png" },
    };

    static readonly Dictionary<string, string> InstancedEnemyMaterialTargets = new Dictionary<string, string>
    {
        { "Assets/Material/Mat_Enemy_Basic_Sprite.mat", "Assets/Art/Sprites/Generated/BasicEnemyDroneSymmetric.png" },
        { "Assets/Material/Mat_Enemy_Speeder_Sprite.mat", "Assets/Art/Sprites/Generated/FastEnemyDroneSymmetric.png" },
        { "Assets/Material/Mat_Enemy_Tanker_Sprite.mat", "Assets/Art/Sprites/Generated/TankEnemyDroneSymmetric.png" },
    };

    [MenuItem("Tools/Art/Wire Sprite Animators")]
    public static void Wire()
    {
        int wired = 0;

        foreach (var target in Targets)
        {
            string path = target.Key;
            string texturePath = target.Value;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[SpriteAnimatorWiring] 프리팹 없음: {path}");
                continue;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject renderRoot = path == PlayerPrefabPath
                    ? EnsurePlayerVisual(scope.prefabContentsRoot)
                    : scope.prefabContentsRoot;
                SpriteRenderer sr = renderRoot.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    Debug.LogWarning($"[SpriteAnimatorWiring] {path}: 루트에 SpriteRenderer가 없다");
                    continue;
                }

                Sprite[] frames = LoadFrames(texturePath);
                if (frames.Length < 1)
                {
                    Debug.LogWarning($"[SpriteAnimatorWiring] {path}: 시트에 프레임이 {frames.Length}개다. " +
                                     "art_import.py 출력과 GeneratedSpritePostprocessor 슬라이싱을 확인할 것");
                    continue;
                }

                if (path != PlayerPrefabPath && frames.Length < 2)
                {
                    Debug.LogWarning($"[SpriteAnimatorWiring] {path}: 시트에 프레임이 {frames.Length}개다. " +
                                     "art_import.py 출력과 GeneratedSpritePostprocessor 슬라이싱을 확인할 것");
                    continue;
                }

                SpriteFrameAnimator anim = renderRoot.GetComponent<SpriteFrameAnimator>();
                if (anim == null)
                    anim = renderRoot.AddComponent<SpriteFrameAnimator>();

                // [SerializeField] private 필드라 SerializedObject 로 넣는다
                var so = new SerializedObject(anim);
                var prop = so.FindProperty("_frames");
                prop.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];

                if (path == PlayerPrefabPath)
                {
                    var moveProp = so.FindProperty("_moveFrames");
                    moveProp.arraySize = 0;
                    so.FindProperty("_fps").floatValue = 4f;
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                sr.sprite = frames[0];
                wired++;
                Debug.Log($"[SpriteAnimatorWiring] {System.IO.Path.GetFileName(path)} — {frames.Length}프레임 배선");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SpriteAnimatorWiring] 완료 — {wired}/{Targets.Count}건");
    }

    [MenuItem("Tools/Art/Wire Instanced Enemy Static Art")]
    public static void WireInstancedEnemyStaticArt()
    {
        int wired = 0;

        foreach (var target in InstancedEnemyMaterialTargets)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(target.Key);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(target.Value);
            if (material == null || texture == null)
            {
                Debug.LogWarning($"[SpriteAnimatorWiring] 인스턴싱 적 배선 실패: {target.Key}");
                continue;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(0.25f, 1f));
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", new Vector2(0.25f, 1f));
            material.SetTextureOffset("_MainTex", Vector2.zero);
            EditorUtility.SetDirty(material);
            wired++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SpriteAnimatorWiring] 인스턴싱 적 정지 프레임 배선 완료 — {wired}/{InstancedEnemyMaterialTargets.Count}건");
    }

    static GameObject EnsurePlayerVisual(GameObject player)
    {
        Transform visual = player.transform.Find(PlayerVisualName);
        if (visual != null)
            return visual.gameObject;

        SpriteRenderer sourceRenderer = player.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null)
        {
            Debug.LogWarning("[SpriteAnimatorWiring] Player: 루트에 SpriteRenderer가 없다");
            return player;
        }

        GameObject visualObject = new GameObject(PlayerVisualName);
        visualObject.transform.SetParent(player.transform, false);

        SpriteRenderer visualRenderer = visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = sourceRenderer.sprite;
        visualRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        visualRenderer.color = sourceRenderer.color;
        visualRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        visualRenderer.sortingOrder = sourceRenderer.sortingOrder;
        visualRenderer.flipX = sourceRenderer.flipX;

        sourceRenderer.enabled = false;

        SpriteFrameAnimator rootAnimator = player.GetComponent<SpriteFrameAnimator>();
        if (rootAnimator != null)
            rootAnimator.enabled = false;

        PlayerIdlePulse rootPulse = player.GetComponent<PlayerIdlePulse>();
        if (rootPulse != null)
            rootPulse.enabled = false;

        visualObject.AddComponent<PlayerIdlePulse>();
        visualObject.AddComponent<PlayerHoverBob>();
        return visualObject;
    }

    static void WirePlayerHoverThruster(GameObject player)
    {
        Sprite[] frames = LoadFrames(PlayerHoverThrusterTexturePath);
        if (frames.Length < 2)
        {
            Debug.LogWarning($"[SpriteAnimatorWiring] Player: 추진 이펙트 시트에 프레임이 {frames.Length}개다. " +
                             "art_import.py 출력과 GeneratedSpritePostprocessor 슬라이싱을 확인할 것");
            return;
        }

        Transform child = player.transform.Find(PlayerHoverThrusterName);
        if (child == null)
        {
            child = new GameObject(PlayerHoverThrusterName).transform;
            child.SetParent(player.transform, false);
        }
        child.localPosition = new Vector3(0f, -0.8f, 0f);
        child.localScale = new Vector3(0.45f, 0.45f, 1f);

        SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = child.gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = PlayerSortingOrder - 1;
        sr.sprite = frames[0];

        SpriteFrameAnimator anim = child.GetComponent<SpriteFrameAnimator>();
        if (anim == null)
            anim = child.gameObject.AddComponent<SpriteFrameAnimator>();
        var so = new SerializedObject(anim);
        var prop = so.FindProperty("_frames");
        prop.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];

        so.FindProperty("_fps").floatValue = 8f;
        so.FindProperty("_moveFrames").arraySize = 0;
        so.FindProperty("_randomizePhase").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 지정한 텍스처의 서브스프라이트를 이름 순으로 모은다.
    // GeneratedSpritePostprocessor 가 <Name>_0 .. <Name>_N 으로 이름을 붙이므로 순서가 보장된다.
    static Sprite[] LoadFrames(string texturePath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
            .ToArray();
    }
}
