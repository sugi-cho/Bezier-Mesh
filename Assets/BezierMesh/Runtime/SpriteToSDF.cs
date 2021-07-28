using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;

public class SpriteToSDF : MonoBehaviour
{
    [SerializeField] Texture2D texture;
    [SerializeField] RenderTexture sdfTexture;
    [SerializeField] Material drawMat;
    [SerializeField] Renderer quad;
    [SerializeField] Renderer sdf;
    [SerializeField] int pixelPerUnit = 64;
    [SerializeField] ComputeShader sdfCompute;

    public void GenerateSDF(Sprite source)
    {
        var rect = source.rect;
        if (texture != null)
            DestroyImmediate(texture);
        texture = VectorUtils.RenderSpriteToTexture2D(source, Mathf.RoundToInt(rect.width * pixelPerUnit), Mathf.RoundToInt(rect.height * pixelPerUnit), drawMat, 4);
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

        var tmp = RenderTexture.GetTemporary(sdfTexture.descriptor);
        Graphics.CopyTexture(sdfTexture, tmp);
        var kernel = sdfCompute.FindKernel("Init");
        sdfCompute.SetInt("width", sdfTexture.width);
        sdfCompute.SetInt("height", sdfTexture.height);
        sdfCompute.SetFloat("pixelDistance", 1f / pixelPerUnit);
        sdfCompute.SetTexture(kernel, "Source", tmp);
        sdfCompute.SetTexture(kernel, "Result", sdfTexture);
        sdfCompute.Dispatch(kernel, sdfTexture.width / 8 + 1, sdfTexture.height / 8 + 1, 1);
        Graphics.CopyTexture(sdfTexture, tmp);

        kernel = sdfCompute.FindKernel("Spread");
        var rts = new[] { sdfTexture, tmp };
        for (var i = 0; i < pixelPerUnit; i++)
        {
            sdfCompute.SetTexture(kernel, "Source", rts[0]);
            sdfCompute.SetTexture(kernel, "Result", rts[1]);
            sdfCompute.Dispatch(kernel, sdfTexture.width / 8 + 1, sdfTexture.height / 8 + 1, 1);

            var rt0 = rts[0];
            rts[0] = rts[1];
            rts[1] = rt0;
        }

        if (rts[0] != sdfTexture)
            Graphics.ConvertTexture(rts[0], sdfTexture);
        
        RenderTexture.ReleaseTemporary(tmp);

        if (sdf != null)
        {
            sdf.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", sdfTexture);
            sdf.SetPropertyBlock(mpb);
            sdf.transform.localPosition = rect.center;
            sdf.transform.localScale = new Vector3(rect.width + 2f, rect.height + 2f, 1f);
        }
    }
}
