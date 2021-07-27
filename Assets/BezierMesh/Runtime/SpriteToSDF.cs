using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;

public class SpriteToSDF : MonoBehaviour
{
    [SerializeField] Texture2D texture;
    [SerializeField] RenderTexture sdfTexture;
    [SerializeField] Material drawMat;
    [SerializeField] bool expandEdges;
    [SerializeField] Renderer quad;
    [SerializeField] Renderer sdf;
    [SerializeField] int pixelPerUnit = 64;
    [SerializeField] ComputeShader sdfCompute;

    public void GenerateSDF(Sprite source)
    {
        var rect = source.rect;
        if (texture != null)
            DestroyImmediate(texture);
        texture = VectorUtils.RenderSpriteToTexture2D(source, Mathf.RoundToInt(rect.width * pixelPerUnit), Mathf.RoundToInt(rect.height * pixelPerUnit), drawMat, 1, expandEdges);
        texture.wrapMode = TextureWrapMode.Clamp;
        var mpb = new MaterialPropertyBlock();
        if (quad != null)
        {
            quad.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", texture);
            quad.SetPropertyBlock(mpb);
            quad.transform.localPosition = rect.center;
            quad.transform.localScale = new Vector3(rect.width, rect.height, 1f);
        }
        if (sdfTexture != null)
            DestroyImmediate(sdfTexture);
        sdfTexture = new RenderTexture(texture.width + pixelPerUnit * 2, texture.height + pixelPerUnit * 2, 0, RenderTextureFormat.ARGB32);
        sdfTexture.enableRandomWrite = true;
        sdfTexture.Create();
        Graphics.CopyTexture(texture, 0, 0, 0, 0, texture.width, texture.height, sdfTexture, 0, 0, pixelPerUnit, pixelPerUnit);
        if (sdf != null)
        {
            sdf.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", sdfTexture);
            sdf.SetPropertyBlock(mpb);
            sdf.transform.localPosition = rect.center;
            sdf.transform.localScale = new Vector3(rect.width + 2f, rect.height + 2f, 1f);

            var tmp = RenderTexture.GetTemporary(sdfTexture.descriptor);
            Graphics.CopyTexture(sdfTexture, tmp);
            var kernel = sdfCompute.FindKernel("Clear");
            sdfCompute.SetInt("width", sdfTexture.width);
            sdfCompute.SetInt("height", sdfTexture.height);
            sdfCompute.SetTexture(kernel, "Source", tmp);
            sdfCompute.SetTexture(kernel, "Result", sdfTexture);
            sdfCompute.Dispatch(kernel, sdfTexture.width / 8, sdfTexture.height / 8, 1);
            RenderTexture.ReleaseTemporary(tmp);
        }
    }
}
