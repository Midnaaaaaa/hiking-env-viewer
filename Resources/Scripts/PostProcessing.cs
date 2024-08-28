using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[RequireComponent(typeof(Camera))]
public class PostProcessing : MonoBehaviour
{
    [SerializeField] Material postProcessingMaterial;
    [SerializeField] Color fogColor;
    [SerializeField] float fogDensity;
    [SerializeField] Light sun;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
        Matrix4x4 viewProjMatrix = projMatrix * cam.worldToCameraMatrix;
        postProcessingMaterial.SetVector("_FogColor", fogColor);
        postProcessingMaterial.SetFloat("_FogDensity", fogDensity);
        postProcessingMaterial.SetMatrix("InverseViewProjection", viewProjMatrix.inverse);
        sun.transform.LookAt(cam.transform.position);
        postProcessingMaterial.SetVector("SunDirection", sun.transform.position);
        postProcessingMaterial.SetVector("SunColor", sun.color);
        Graphics.Blit(source, destination, postProcessingMaterial);
    }


    public Color GetFogColor()
    {
        return fogColor;
    }

    public float GetFogDensity() { return fogDensity; }
}
