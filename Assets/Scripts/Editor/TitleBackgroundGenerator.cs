#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Title 씬의 기존 MenuBackground 를 로비 전용 원화 배경으로 in-place 교체한다.
// 실행: Tools/UI/Build Title Background
public static class TitleBackgroundGenerator
{
    const string BackgroundPath = "Assets/Resources/Art/Title/TitleCorridorBackground.png";

    [MenuItem("Tools/UI/Build Title Background")]
    public static void BuildTitleBackground()
    {
        var title = Object.FindFirstObjectByType<TitleScene>(FindObjectsInactive.Include);
        if (title == null)
        {
            Debug.LogError("[TitleBackgroundGenerator] TitleScene 을 찾지 못했습니다. Title 씬을 연 상태로 실행하세요.");
            return;
        }

        var canvas = UIGenScene.ResolveMainCanvas("TitleBackgroundGenerator");
        if (canvas == null) return;

        var background = (RectTransform)canvas.transform.Find("MenuBackground");
        if (background == null)
        {
            var go = new GameObject("MenuBackground", typeof(RectTransform), typeof(Image));
            background = (RectTransform)go.transform;
            background.SetParent(canvas.transform, false);
        }

        var sprite = LoadBackgroundSprite();
        if (sprite == null)
        {
            Debug.LogError($"[TitleBackgroundGenerator] 배경 스프라이트를 불러오지 못했습니다: {BackgroundPath}");
            return;
        }

        var image = background.GetComponent<Image>();
        if (image == null) image = background.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;

        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        background.localScale = Vector3.one;
        background.anchoredPosition = Vector2.zero;
        background.SetAsFirstSibling();

        var motion = background.GetComponent<TitleBackgroundMotion>();
        if (motion == null) background.gameObject.AddComponent<TitleBackgroundMotion>();

        UILayerAssign.AssignLayer(background.gameObject, UILayer.Background);

        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = background.gameObject;
        Debug.Log("[TitleBackgroundGenerator] 타이틀 배경 원화와 저강도 모션을 적용했습니다.");
    }

    static Sprite LoadBackgroundSprite()
    {
        var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null) return null;

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
    }
}
#endif
