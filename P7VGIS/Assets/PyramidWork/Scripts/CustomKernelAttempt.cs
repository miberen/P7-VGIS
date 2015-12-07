using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CustomKernelAttempt : MonoBehaviour
{
    private NPFrame2 frame;
    public RenderTexture donePow21;
    public RenderTexture donePow22;
    public RenderTexture done;
    public RenderTexture gray;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    int[] kernel = {-1, -2, -1, 0, 0, 0, 1, 2, 1};
    int[] kernel2 = { -1, 0, 1, -2, 0, -2, 1, 0, 1 };

    void Start()
    {
        frame = new NPFrame2("stuff", 8);

        donePow21 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow21.enableRandomWrite = true;
        donePow21.Create();

        donePow22 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow22.enableRandomWrite = true;
        donePow22.Create();

        done = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        done.enableRandomWrite = true;
        done.Create();

        gray = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        gray.enableRandomWrite = true;
        gray.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;
        frame.Analyze(source);

        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Grayscale"), "source", frame.AnalyzeList[0]);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Grayscale"), "dest", gray);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Grayscale"), (int)Mathf.Ceil(donePow22.width / 32), (int)Mathf.Ceil(donePow22.height / 32), 1);

        frame.ApplyCustomKernel(gray, donePow21, kernel);
        frame.ApplyCustomKernel(gray, donePow22, kernel2);

        frame.GetShader.SetTexture(frame.GetShader.FindKernel("AddImages"), "img1", donePow21);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("AddImages"), "img2", donePow22);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("AddImages"), "dest", done);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("AddImages"), (int)Mathf.Ceil(donePow22.width / 32), (int)Mathf.Ceil(donePow22.height / 32), 1);


        frame.MakeNPOT(done);

        Graphics.Blit(frame.GetDoneNPOT, dest);

    }

}