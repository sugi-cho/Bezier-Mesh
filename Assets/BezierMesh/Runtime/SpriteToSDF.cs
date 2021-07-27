using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;

public class SpriteToSDF : MonoBehaviour
{
    [SerializeField] Texture2D texture;
    [SerializeField] Material drawMat;
    [SerializeField] bool expandEdges;

    public void GenerateSDF(Sprite source)
    {
        var bounds = source.bounds;
        if (texture != null)
            DestroyImmediate(texture);
        texture = VectorUtils.RenderSpriteToTexture2D(source, Mathf.RoundToInt(bounds.size.x * 64), Mathf.RoundToInt(bounds.size.y * 64), drawMat, 1, expandEdges);
    }
}
