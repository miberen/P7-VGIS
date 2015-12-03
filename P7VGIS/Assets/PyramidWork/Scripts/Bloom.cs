using UnityEngine;
using System.Collections;

public class Bloom : MonoBehaviour
{

    private NPFrame2 frame;
    public RenderTexture donePow2;
    public RenderTexture  bloomTexture;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    [Range(0, 255)]
    public int bloomValue = 200;
    [Range(0.0f, 1.0f)]
    public float bloomStrength= 0.5f;

    void Start()
    {
        frame = new NPFrame2("Bloom", 5);

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

        GenerateBloomTexture(frame.AnalyzeList[frame.AnalyzeList.Count - 1]);

        frame.GenerateSynthesis(3, "Bloomsynth", _SynthesisMode);

        DoBloom(source);

        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);

    }

    void GenerateBloomTexture(RenderTexture source)
    {
            frame.GetShader.SetInt("firstPass", 1);

            frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
            frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", frame.AnalyzeList[frame.AnalyzeList.Count - 1]);

            frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(frame.AnalyzeList[frame.AnalyzeList.Count - 1].width / 32), (int)Mathf.Ceil(frame.AnalyzeList[frame.AnalyzeList.Count - 1].height / 32), 1);

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
