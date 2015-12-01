using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CustomKernelAttempt : MonoBehaviour
{
    private NPFrame2 frame;
    public RenderTexture donePow2;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    int[] kernel = {0, -1, 1, 0};
    void Start()
    {
        frame = new NPFrame2("stuff", 8);

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;
        frame.Analyze(ref source);

        frame.ApplyCustomKernel(source, donePow2, kernel);

        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);

    }

}