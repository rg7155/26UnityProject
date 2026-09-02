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
    const string PlayerMoveTexturePath = "Assets/Art/Sprites/Generated/PlayerEnergyCoreMove.png";

    // 프리팹과 Generated 텍스처 경로. art-gen 스킬의 애니메이션 대상과 짝을 이룬다.
    static readonly Dictionary<string, string> Targets = new Dictionary<string, string>
    {
        { PlayerPrefabPath, "Assets/Art/Sprites/Generated/PlayerEnergyCoreSymmetric.png" },
        { "Assets/Prefabs/Enemy/Boss_Monolith.prefab", "Assets/Art/Sprites/Generated/BossMonolithSymmetric.png" },
        { "Assets/Prefabs/Weapon/Orbiter.prefab", "Assets/Art/Sprites/Generated/OrbiterEnergyBlade.png" },
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
                SpriteRenderer sr = scope.prefabContentsRoot.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    Debug.LogWarning($"[SpriteAnimatorWiring] {path}: 루트에 SpriteRenderer가 없다");
                    continue;
                }

                Sprite[] frames = LoadFrames(texturePath);
                if (frames.Length < 2)
                {
                    Debug.LogWarning($"[SpriteAnimatorWiring] {path}: 시트에 프레임이 {frames.Length}개다. " +
                                     "art_import.py 출력과 GeneratedSpritePostprocessor 슬라이싱을 확인할 것");
                    continue;
                }

                Sprite[] moveFrames = null;
                if (path == PlayerPrefabPath)
                {
                    moveFrames = LoadFrames(PlayerMoveTexturePath);
                    if (moveFrames.Length < 2)
                    {
                        Debug.LogWarning($"[SpriteAnimatorWiring] {path}: 이동 시트에 프레임이 " +
                                         $"{moveFrames.Length}개다. art_import.py 출력과 슬라이싱을 확인할 것");
                        continue;
                    }
                }

                var anim = scope.prefabContentsRoot.GetComponent<SpriteFrameAnimator>()
                           ?? scope.prefabContentsRoot.AddComponent<SpriteFrameAnimator>();

                // [SerializeField] private 필드라 SerializedObject 로 넣는다
                var so = new SerializedObject(anim);
                var prop = so.FindProperty("_frames");
                prop.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];

                if (moveFrames != null)
                {
                    var moveProp = so.FindProperty("_moveFrames");
                    moveProp.arraySize = moveFrames.Length;
                    for (int i = 0; i < moveFrames.Length; i++)
                        moveProp.GetArrayElementAtIndex(i).objectReferenceValue = moveFrames[i];

                    so.FindProperty("_fps").floatValue = 4f;
                    so.FindProperty("_moveFps").floatValue = 8f;
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
