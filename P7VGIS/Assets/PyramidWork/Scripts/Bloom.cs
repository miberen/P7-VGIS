using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bloom : MonoBehaviour
{
    //Variables used for Bloom - These are common for most effects
    private NPFrame2 frame;
    public RenderTexture donePow2;
    public RenderTexture bloomTexture;
    public RenderTexture origin;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    [Range(0, 1)]
    public float bloomValue = 0.5f;
    [Range(1, 6)]
    public int bloomStrength= 3;

    void Start()
    {
        frame = new NPFrame2("Bloom", 7);

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();

        origin = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        origin.enableRandomWrite = true;
        origin.Create();

        bloomTexture =  new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        bloomTexture.enableRandomWrite = true;
        bloomTexture.Create();

    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;

        frame.MakePow2(source, origin);

        GenerateBloomTexture(origin);

        frame.Analyze(bloomTexture);

        frame.GenerateSynthesis("Bloomsynth", _SynthesisMode, bloomStrength);

        DoBloom(origin, frame.GetSynthesis("Bloomsynth")[frame.GetSynthesis("Bloomsynth").Count-1]);

        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);

    }

    void GenerateBloomTexture(RenderTexture source)
    {
            frame.GetShader.SetInt("firstPass", 1);

            frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
            frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", bloomTexture);

            frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(source.width / 32), (int)Mathf.Ceil(source.height / 32), 1);

      }

    void DoBloom(RenderTexture source, RenderTexture bloom)
    {

        frame.GetShader.SetInt("firstPass", 0);
        frame.GetShader.SetFloat("bloomValue", bloomValue);
        frame.GetShader.SetFloat("bloomStrength", bloomStrength);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "bloom", bloom);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", donePow2);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(bloom.width / 32), (int)Mathf.Ceil(bloom.height / 32), 1);


    }

    }
