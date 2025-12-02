using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public class pool_generic : MonoBehaviour
{
    public IObjectPool<GameObject> pool;
    [SerializeField] private List<GameObject> obj_reference = new List<GameObject>();
    public GameObject prefab;
    [Header("DO NOT EDIT AT RUNTIME")]
    public int MaxCapacity = 50;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        pool = new ObjectPool<GameObject>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,   // helps catch double-release mistakes
            defaultCapacity: 10,
            maxSize: MaxCapacity
        );
    }



    // Creates a new pooled GameObject the first time (and whenever the pool needs more).

    public GameObject CreateItem()
    {
        GameObject gameObject = Instantiate(prefab);
        obj_reference.Add(gameObject);
        gameObject.name = prefab.name;
        gameObject.SetActive(false);
        return gameObject;
    }

    // Called when an item is taken from the pool.
    public void OnGet(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    // Called when an item is returned to the pool.
    public void OnRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    public void OnDestroyItem(GameObject gameObject)
    {
        Destroy(gameObject);
    }

    public System.Collections.IEnumerator ReturnAfter(GameObject gameObject, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        // Give it back to the pool.
        pool.Release(gameObject);
    }
}
