using UnityEngine;

public class DisableFrustumCulling : MonoBehaviour
{
    private void Start()
    {
        // 获取所有子物体的渲染器
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (var rend in renderers)
        {
            // 直接将渲染边界扩展到极大（10000米），无论相机去哪都不会消失
            rend.localBounds = new Bounds(Vector3.zero, new Vector3(10000, 1, 10000));
        }
    }
}