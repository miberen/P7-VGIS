using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Blur : MonoBehaviour
{
    // Variable for testing.
    private PTimer timer;
    private int counter = 0;
    float realTime = 0;
    float subtractTime = 0;

    //Variables used for blur - These are common for most effects
    private NPFrame2 frame;
    public RenderTexture donePow2;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    //Variable to control at which level to synthesise from - higher value = more blur - synth from lower level
    [Range(1, 4)]
    public int blurStrength = 2;

    void Start()
    {
        // Variable for testing
        timer = PerformanceTimer.CreateTimer(); // Create and assign timer

        frame = new NPFrame2("Blur", 5);     

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
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

        frame.Analyze(source); 
        frame.GenerateSynthesis("Synth",_SynthesisMode, blurStrength);
        MakeBlur(donePow2);
        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);

        // For testing
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

    /// <summary>
    /// Compute image blur by using the top level of the synthesis pyramid
    /// </summary>
    /// <param name="dest">Destination texture (usually nearest PoT texture from native resolution)</param>
    void MakeBlur(RenderTexture dest)
    {
        //Set the source in the compute shader to the top level of the synthesis pyramid
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Blur"), "source", frame.GetSynthesis("Synth")[blurStrength-1]);
        //Set the destination texture to our destination texture (Empty PoT texture)
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("Blur"), "dest", dest);
        //Dispatch the shader - Mathf.Ceil is used to split the image into blocks of 32x32 pixels - with as many as are needed - for non PoT add +1
        frame.GetShader.Dispatch(frame.GetShader.FindKernel("Blur"), (int)Mathf.Ceil(dest.width / 32), (int)Mathf.Ceil(dest.height / 32), 1);
     }
 }