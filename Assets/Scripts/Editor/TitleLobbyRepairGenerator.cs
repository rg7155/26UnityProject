#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 잘못된 Canvas 선택으로 MenuBackground 아래에 생성된 구형 TitleLobby를 안전하게 정리한다.
// 실행: Tools/UI/Repair Title Lobby Structure
public static class TitleLobbyRepairGenerator
{
    [MenuItem("Tools/UI/Repair Title Lobby Structure")]
    public static void RepairTitleLobbyStructure()
    {
        var title = Object.FindFirstObjectByType<TitleScene>(FindObjectsInactive.Include);
        if (title == null)
        {
            Debug.LogError("[TitleLobbyRepair] TitleScene을 찾지 못했습니다. Title 씬을 연 상태로 실행하세요.");
            return;
        }

        var canvas = UIGenScene.ResolveMainCanvas("TitleLobbyRepair");
        if (canvas == null) return;

        var canvasRT = canvas.transform as RectTransform;
        var canonical = canvasRT.Find("TitleLobby") as RectTransform;
        var menuBackground = canvasRT.Find("MenuBackground") as RectTransform;
        if (canonical == null || canonical.parent != canvasRT || menuBackground == null)
        {
            Debug.LogError("[TitleLobbyRepair] Canvas 직속 TitleLobby와 MenuBackground가 모두 필요합니다. 먼저 Build Title Lobby를 실행해 정본 로비를 만드세요.");
            return;
        }

        var staleLobbies = FindNestedLobbies(menuBackground, canonical);
        if (staleLobbies.Count == 0)
        {
            Debug.Log("[TitleLobbyRepair] MenuBackground 아래의 구형 TitleLobby가 없습니다. 정리할 항목이 없습니다.");
            return;
        }
        if (staleLobbies.Count != 1)
        {
            Debug.LogError($"[TitleLobbyRepair] MenuBackground 아래에서 구형 TitleLobby를 {staleLobbies.Count}개 찾았습니다. 자동 정리를 중단합니다.");
            return;
        }

        var stale = staleLobbies[0];
        var bottomBar = canonical.Find("BottomBar") as RectTransform;
        var recordCard = canonical.Find("CenterGroup/RecordCard") as RectTransform;
        if (bottomBar == null || recordCard == null)
        {
            Debug.LogError("[TitleLobbyRepair] 정본 TitleLobby의 BottomBar 또는 CenterGroup/RecordCard가 없습니다. 먼저 Build Title Lobby를 실행하세요.");
            return;
        }

        var titleSo = new SerializedObject(title);
        if (!MigrateButton(titleSo, "_startButton", "StartButton", canonical, stale, bottomBar) ||
            !MigrateText(titleSo, "_bestText", "BestText", canonical, stale, recordCard) ||
            !MigrateButton(titleSo, "_shopButton", "ShopButton", canonical, stale, bottomBar) ||
            !MigrateButton(titleSo, "_questButton", "QuestButton", canonical, stale, bottomBar) ||
            !MigrateButton(titleSo, "_bossRushButton", "BossRushButton", canonical, stale, bottomBar))
            return;

        titleSo.ApplyModifiedProperties();
        titleSo.Update();

        var remaining = FindReferencesInside(titleSo, stale);
        if (remaining.Count > 0)
        {
            Debug.LogError($"[TitleLobbyRepair] TitleScene 참조가 구형 Lobby 안에 남아 있어 삭제를 중단합니다: {string.Join(", ", remaining)}", stale);
            return;
        }

        Debug.Log($"[TitleLobbyRepair] 구형 중첩 Lobby 제거: {UIGenScene.Path(stale)}", stale);
        Object.DestroyImmediate(stale.gameObject);

        EditorUtility.SetDirty(title);
        EditorSceneManager.MarkSceneDirty(title.gameObject.scene);
        Selection.activeGameObject = canonical.gameObject;
        Debug.Log("[TitleLobbyRepair] TitleLobby 구조 정리 완료. Canvas 아래의 정본 TitleLobby만 남겼습니다.");
    }

    static List<RectTransform> FindNestedLobbies(RectTransform menuBackground, RectTransform canonical)
    {
        var matches = new List<RectTransform>();
        foreach (var rect in menuBackground.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect != menuBackground && rect != canonical && rect.name == "TitleLobby")
                matches.Add(rect);
        }
        return matches;
    }

    static bool MigrateButton(SerializedObject titleSo, string propertyName, string objectName, RectTransform canonical, RectTransform stale, RectTransform fallbackParent)
    {
        var property = titleSo.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[TitleLobbyRepair] TitleScene.{propertyName} 필드를 찾지 못해 정리를 중단합니다.");
            return false;
        }

        var current = property.objectReferenceValue as Button;
        if (current == null || !IsInside(current, stale)) return true;

        var replacement = FindComponent<Button>(canonical, objectName);
        if (replacement != null)
        {
            property.objectReferenceValue = replacement;
            return true;
        }

        current.transform.SetParent(fallbackParent, false);
        return true;
    }

    static bool MigrateText(SerializedObject titleSo, string propertyName, string objectName, RectTransform canonical, RectTransform stale, RectTransform fallbackParent)
    {
        var property = titleSo.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[TitleLobbyRepair] TitleScene.{propertyName} 필드를 찾지 못해 정리를 중단합니다.");
            return false;
        }

        var current = property.objectReferenceValue as TMP_Text;
        if (current == null || !IsInside(current, stale)) return true;

        var replacement = FindComponent<TMP_Text>(canonical, objectName);
        if (replacement != null)
        {
            property.objectReferenceValue = replacement;
            return true;
        }

        current.rectTransform.SetParent(fallbackParent, false);
        return true;
    }

    static T FindComponent<T>(RectTransform root, string objectName) where T : Component
    {
        foreach (var component in root.GetComponentsInChildren<T>(true))
            if (component.name == objectName) return component;
        return null;
    }

    static List<string> FindReferencesInside(SerializedObject serializedObject, Transform stale)
    {
        var paths = new List<string>();
        var property = serializedObject.GetIterator();
        var enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyType == SerializedPropertyType.ObjectReference && IsInside(property.objectReferenceValue, stale))
                paths.Add(property.propertyPath);
        }
        return paths;
    }

    static bool IsInside(Object value, Transform root)
    {
        if (value is Component component) return component.transform.IsChildOf(root);
        if (value is GameObject gameObject) return gameObject.transform.IsChildOf(root);
        return false;
    }
}
#endif
