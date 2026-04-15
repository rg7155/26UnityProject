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
        _pools.Clear();
    }
}
