#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 현재 열린 씬의 모달 루트에 UIModalTween 을 붙인다. 실행: Tools/UI/Add Modal Tween
//
// 기존 생성기에 끼워 넣지 않고 별도 메뉴로 둔 이유: 모달 루트는 씬마다 다른 생성기가 만들고
// (Upgrade/Pause=GameScene, Shop/Quest=Title) 생성기는 in-place 라 한 번 붙인 컴포넌트가 유지된다.
// 생성기 4개를 각각 건드리는 것보다 회귀 위험이 작다.
public static class ModalTweenSetup
{
    [MenuItem("Tools/UI/Add Modal Tween")]
    public static void AddModalTween()
    {
        int count = 0;
        count += Attach(FindRoot<UI_UpgradePanel>());
        count += Attach(FindRoot<UI_ShopPanel>());
        count += Attach(FindRoot<UI_QuestPanel>());
        count += Attach(FindPausePanel());

        if (count == 0)
        {
            Debug.LogWarning("[ModalTweenSetup] 이 씬에서 모달 루트를 찾지 못했습니다. GameScene(업그레이드·Pause) 또는 Title(상점·퀘스트)을 연 상태로 실행하세요.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[ModalTweenSetup] 모달 {count}개에 UIModalTween 적용 완료. 씬을 저장하세요.");
    }

    static GameObject FindRoot<T>() where T : MonoBehaviour
    {
        var panel = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        return panel != null ? panel.gameObject : null;
    }

    // PauseController 가 토글하는 대상은 자기 자신이 아니라 자식 _panelRoot 다(입력 폴링을 유지하려고 분리돼 있다)
    static GameObject FindPausePanel()
    {
        var controller = Object.FindFirstObjectByType<PauseController>(FindObjectsInactive.Include);
        if (controller == null) return null;

        var so = new SerializedObject(controller);
        return so.FindProperty("_panelRoot").objectReferenceValue as GameObject;
    }

    static int Attach(GameObject go)
    {
        if (go == null) return 0;

        if (go.GetComponent<CanvasGroup>() == null)
            Undo.AddComponent<CanvasGroup>(go);

        if (go.GetComponent<UIModalTween>() == null)
            Undo.AddComponent<UIModalTween>(go);

        EditorUtility.SetDirty(go);
        Debug.Log($"[ModalTweenSetup] {go.name}", go);
        return 1;
    }
}
#endif
