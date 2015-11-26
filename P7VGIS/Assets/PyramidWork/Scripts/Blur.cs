using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class Blur : MonoBehaviour
{
    private NPFrame2 frame;
    public RenderTexture donePow2;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    //this stuff is for show & tell @ computer graphics presentation

    [Range(1, 7)]
    public int blurStrength = 2;

    void Start()
    {
        frame = new NPFrame2("Blur", 8);     

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.TextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.TextureFormat = _textureFormat;
        frame.FilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;
        frame.Analyze(ref source);
        frame.GenerateSynthesis(blurStrength, "Synth",_SynthesisMode);

        MakeBlur(donePow2);

        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);

        }

    void MakeBlur(RenderTexture dest)
    {
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Blur"), "source", frame.GetSynthesis("Synth").Pyramid[frame.GetSynthesis("Synth").Pyramid.Count - 1]);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Blur"), "dest", donePow2);
                frame.GetShader.Dispatch(frame.GetShader.FindKernel("Blur"), (int)Mathf.Ceil(frame.GetSynthesis("Synth").Pyramid[frame.GetSynthesis("Synth").Pyramid.Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("Synth").Pyramid[frame.GetSynthesis("Synth").Pyramid.Count - 1].height / 32), 1);


     }
 }