using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CustomKernelAttempt : MonoBehaviour
{
    //In this implementation we use Sobel kernels to do edge detection. We use a graycale image, and we need two textures to store the vertical, and horizontal edges. 
    private NPFrame2 frame;
    public RenderTexture edgesPoT_h;
    public RenderTexture edgesPoT_v;
    public RenderTexture donePoT;
    public RenderTexture gray;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    //The two sobel kernels, they are initialized from buttom left in our case due to how compute shaders handle texture (0,0 is buttom left). First kernel is horizontal, other is vertical. 
    int[] sobelKernel_h = {-1, -2, -1, 0, 0, 0, 1, 2, 1};
    int[] sobelKernel_v = { -1, 0, 1, -2, 0, -2, 1, 0, 1 };
    int[] gaussianKernel = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
    int[] gaussianKernel_7 = { 1, 4, 7, 4, 1, 4, 16, 26, 16, 4, 7, 26, 41, 26, 7, 4, 16, 26, 16, 4, 1, 4, 7, 4, 1 };

    void Start()
    {
        frame = new NPFrame2("stuff", 8);

        edgesPoT_h = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        edgesPoT_h.enableRandomWrite = true;
        edgesPoT_h.Create();

        edgesPoT_v = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        edgesPoT_v.enableRandomWrite = true;
        edgesPoT_v.Create();

        donePoT = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePoT.enableRandomWrite = true;
        donePoT.Create();

        gray = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        gray.enableRandomWrite = true;
        gray.Create();
    }

    //While we are working on PoT texture, it is not needed for this example as we detect edges on a full rest PoT texture which is expensive. The detection could be done on the lower resolution textures instead, to save computational time. 
    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;
        frame.Analyze(source);

        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Grayscale"), "source", frame.AnalyzeList[0]);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Grayscale"), "dest", gray);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Grayscale"), (int)Mathf.Ceil(edgesPoT_v.width / 32), (int)Mathf.Ceil(edgesPoT_v.height / 32), 1);

        frame.ApplyCustomKernel(frame.AnalyzeList[0], edgesPoT_h, gaussianKernel_7);
        //frame.ApplyCustomKernel(gray, edgesPoT_v, sobelKernel_h);

        //frame.GetShader.SetTexture(frame.GetShader.FindKernel("AddImages"), "img1", edgesPoT_h);
        //frame.GetShader.SetTexture(frame.GetShader.FindKernel("AddImages"), "img2", edgesPoT_v);
        //frame.GetShader.SetTexture(frame.GetShader.FindKernel("AddImages"), "dest", donePoT);

        //frame.GetShader.Dispatch(frame.GetShader.FindKernel("AddImages"), (int)Mathf.Ceil(edgesPoT_v.width / 32), (int)Mathf.Ceil(edgesPoT_v.height / 32), 1);

        frame.MakeNPOT(edgesPoT_h);

        Graphics.Blit(frame.GetDoneNPOT, dest);
    }
}