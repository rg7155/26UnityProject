using System.Collections.Generic;
using UnityEngine;

public class ObjectManager
{
    Transform _root;
    Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    public void Init()
    {
        GameObject go = new GameObject { name = "@Pool" };
        _root = go.transform;
        Object.DontDestroyOnLoad(go);
    }

    public GameObject Get(GameObject prefab)
    {
        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        Queue<GameObject> pool = _pools[prefab];

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = Object.Instantiate(prefab, _root);
            obj.name = prefab.name;
        }

        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj, GameObject prefab)
    {
        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        obj.SetActive(false);
        obj.transform.SetParent(_root);
        _pools[prefab].Enqueue(obj);
    }

    public void Clear()
    {
        // @Pool 은 DontDestroyOnLoad — 활성 상태로 날아다니던 발사체/적(모두 @Pool 자식)이
        // 씬 전환 후에도 살아남아 다음 씬에서 Update/물리 콜백을 쏘는 것을 막기 위해 실제 파괴
        if (_root != null)
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
                Object.Destroy(_root.GetChild(i).gameObject);
        }
        _pools.Clear();
    }
}
