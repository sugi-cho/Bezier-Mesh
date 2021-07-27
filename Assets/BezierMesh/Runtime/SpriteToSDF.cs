using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;

public class SpriteToSDF : MonoBehaviour
{
    [SerializeField] Texture2D texture;
    [SerializeField] Material drawMat;
    [SerializeField] bool expandEdges;
    [SerializeField] Renderer quad;

    public void GenerateSDF(Sprite source)
    {
        var rect = source.rect;
        if (texture != null)
            DestroyImmediate(texture);
        texture = VectorUtils.RenderSpriteToTexture2D(source, Mathf.RoundToInt(rect.width* 64), Mathf.RoundToInt(rect.width * 64), drawMat, 1, expandEdges);
        texture.wrapMode = TextureWrapMode.Clamp;
        var mpb = new MaterialPropertyBlock();
        if (quad != null)
        {
            quad.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", texture);
            quad.SetPropertyBlock(mpb);
            quad.transform.localPosition = rect.center;
            quad.transform.localScale = new Vector3(rect.size.x, rect.size.y, 1f);
        }
    }
}
