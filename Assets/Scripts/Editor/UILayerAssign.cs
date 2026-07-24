#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

// 생성기 공통: 패널 루트에 UILayerCanvas 를 GetOrAdd 하고 레이어를 배정한다(idempotent).
// UILayer 는 값이 비연속(0/100/300)이라 SerializedObject.enumValueIndex(멤버 인덱스)로 세팅해야 한다.
public static class UILayerAssign
{
    public static void AssignLayer(GameObject root, UILayer layer)
    {
        var lc = root.GetComponent<UILayerCanvas>();
        if (lc == null) lc = root.AddComponent<UILayerCanvas>();
        var so = new SerializedObject(lc);
        var prop = so.FindProperty("_layer");
        if (prop != null)
        {
            prop.enumValueIndex = Array.IndexOf((UILayer[])Enum.GetValues(typeof(UILayer)), layer);
            so.ApplyModifiedProperties();
        }
        EditorUtility.SetDirty(lc);
    }
}
#endif
