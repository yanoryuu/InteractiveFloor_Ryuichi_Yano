using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RenderTextureヘルパー
/// </summary>
public static class RenderTextureHelper
{
    /// <summary>
    /// RenderTextureを削除
    /// </summary>
    /// <param name="rt">RenderTexture</param>
    public static void DeleteRenderTexture(RenderTexture rt)
    {
        if (Application.isEditor)
            RenderTexture.DestroyImmediate(rt);
        else
            RenderTexture.Destroy(rt);
        rt = null;
    }
}
