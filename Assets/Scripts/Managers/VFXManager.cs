using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("特效配置列表 (拖入创建的SO)")]
    public List<VFXDataSO> vfxDataList;

    private Dictionary<VFXType, VFXDataSO> vfxDict;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        vfxDict = new Dictionary<VFXType, VFXDataSO>();
        foreach (var data in vfxDataList)
        {
            if (!vfxDict.ContainsKey(data.vfxType))
                vfxDict.Add(data.vfxType, data);
        }
    }

    public GameObject PlayVFX(VFXType type, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!vfxDict.TryGetValue(type, out VFXDataSO data)) return null;

        GameObject vfxObj = ObjectPoolManager.Instance.GetObject(data.vfxPrefab, position, rotation);

        if (parent != null)
        {
            vfxObj.transform.SetParent(parent, false);
        }

        StartCoroutine(RecycleVFXRoutine(vfxObj, data.lifeTime));
        return vfxObj;
    }

    private IEnumerator RecycleVFXRoutine(GameObject vfxObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfxObj != null && vfxObj.activeInHierarchy)
        {
            ObjectPoolManager.Instance.ReturnObject(vfxObj);
        }
    }
}