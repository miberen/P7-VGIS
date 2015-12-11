using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bloom : MonoBehaviour
{
    private PTimer timer;
    private int counter = 0;
    float realTime = 0;
    float subtractTime = 0;

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
    [Tooltip("Controls how intense the light in the image has to be in order to have bloom applied to it. Higher value results in only bright lights included")]
    public float bloomValue = 0.5f;

    [Range(1, 6)]
    [Tooltip("Controls how strong the bloom effect will be by synthesising from a lower level of the analysis pyramid")]
    public int bloomStrength= 3;

    void Start()
    {
        timer = PerformanceTimer.CreateTimer();
        frame = new NPFrame2("Bloom", 7);

        //Textures used for the bloom effect, currently more than needed are used - probably. 
        // TODO: This could be reduced I believe by using the source image non PoT and generate the bloom texture from that and make that PoT and continue from there. 
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
        if (counter < 60)
            subtractTime = Time.realtimeSinceStartup;
        if (counter > 60)
        {
            if (counter < 1061)
                PerformanceTimer.MeasurePointBegin(timer);
        }

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

        if (counter > 60)
        {
            if (counter < 1061)
            {
                realTime = Time.realtimeSinceStartup - subtractTime;
                PerformanceTimer.MeasurePointEnd(timer);
            }
        }


        counter++;
    }

    /// <summary>
    /// Computes the bloom texture by applying a threshold to the source texture after conveting the intensity from the RGB value.  
    /// </summary>
    /// <param name="source">Source texter to generate the bloom texture from</param>
    void GenerateBloomTexture(RenderTexture source)
    {   
        //Used to control which part of the kernel to run, as the kernel is split into two - bloom texture computation and combining of textures.
        frame.GetShader.SetInt("firstPass", 1);

        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", bloomTexture);
        frame.GetShader.SetFloat("bloomValue", bloomValue);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(source.width / 32), (int)Mathf.Ceil(source.height / 32), 1);
    }

    /// <summary>
    /// Computes the bloom effect by taking the source texture and add the blurred bloom texture
    /// </summary>
    /// <param name="source">Original image in PoT</param>
    /// <param name="bloom">Bloom texture in PoT</param>
    void DoBloom(RenderTexture source, RenderTexture bloom)
    {

        frame.GetShader.SetInt("firstPass", 0);
        //frame.GetShader.SetFloat("bloomStrength", bloomStrength);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "source", source);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "bloom", bloom);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Bloom"), "dest", donePow2);

        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Bloom"), (int)Mathf.Ceil(bloom.width / 32), (int)Mathf.Ceil(bloom.height / 32), 1);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 250, 500), "Total time: " + realTime + "s\n" +
                "Processing time: " + (timer.processTimeTotal).ToString("f4") + "ms\n" +
                "Frame number: " + timer.measureCount + "\n" +
                "Current time: " + (timer.measureTime).ToString("f4") + "ms\n" +
                "Shortest time: " + (timer.shortestTime).ToString("f4") + "ms\n" +
                "Longest time: " + (timer.longestTime).ToString("f4") + "ms\n" +
                "Average time. " + (timer.averageTime).ToString("f4") + "ms\n" +
                "Frame time: " + (timer.frameTime).ToString("f4") + "ms\n" +
                "Average frame time. " + (timer.averageFrameTime).ToString("f4") + "ms\n\n");
    }
}