using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Addressables 대신 Resources.Load 사용 (심플, 포트폴리오 규모에 충분)
// 추후 필요 시 Addressables로 교체 가능한 구조로 설계
public class ResourceManager
{
    Dictionary<string, Object> _resources = new Dictionary<string, Object>();

    public T Load<T>(string path) where T : Object
    {
        if (_resources.TryGetValue(path, out Object resource))
            return resource as T;

        T loaded = Resources.Load<T>(path);
        if (loaded != null)
            _resources[path] = loaded;

        return loaded;
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>($"Prefabs/{path}");
        if (prefab == null)
        {
            Debug.LogError($"[ResourceManager] Prefab not found: {path}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = prefab.name;
        return go;
    }

    public void Destroy(GameObject go, float delay = 0f)
    {
        if (go == null) return;
        Object.Destroy(go, delay);
    }

    public void Clear()
    {
        _resources.Clear();
        Resources.UnloadUnusedAssets();
    }
}
