using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bloom : MonoBehaviour
{

    private NPFrame2 frame;
    public RenderTexture donePow2;
    public RenderTexture  bloomTexture;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public List<RenderTexture> Analstuff = new List<RenderTexture>();
    public List<RenderTexture> Synthstuff = new List<RenderTexture>();
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    [Range(0, 255)]
    public int bloomValue = 180;
    [Range(0.0f, 1.0f)]
    public float bloomStrength= 0.5f;

    void Start()
    {
        frame = new NPFrame2("Bloom", 8);

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();

        bloomTexture =  new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        bloomTexture.enableRandomWrite = true;
        bloomTexture.Create();

    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;
        frame.Analyze(ref source);
        Analstuff = frame.AnalyzeList;
        GenerateBloomTexture(frame.AnalyzeList[3]);
        frame.GenerateSynthesis("Bloomsynth", _SynthesisMode, 4);
        Synthstuff = frame.GetSynthesis("Bloomsynth").Pyramid;
        DoBloom(frame.AnalyzeList[0]);

        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);

    }

    void GenerateBloomTexture(RenderTexture source)
    {
            frame.GetShader.SetInt("firstPass", 1);

            frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
            frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", frame.AnalyzeList[4]);

            frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(frame.AnalyzeList[0].width / 32), (int)Mathf.Ceil(frame.AnalyzeList[0].height / 32), 1);

      }

    void DoBloom(RenderTexture source)
    {

        frame.GetShader.SetInt("firstPass", 0);
        frame.GetShader.SetInt("bloomValue", bloomValue);
        frame.GetShader.SetFloat("bloomStrength", bloomStrength);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "bloom", frame.GetSynthesis("Bloomsynth")[frame.GetSynthesis("Bloomsynth").Count-1]);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", donePow2);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(frame.GetSynthesis("Bloomsynth").Pyramid[frame.GetSynthesis("Bloomsynth").Pyramid.Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("Bloomsynth").Pyramid[frame.GetSynthesis("Bloomsynth").Pyramid.Count - 1].height / 32), 1);


    }

    }
