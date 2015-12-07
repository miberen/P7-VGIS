using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Blur : MonoBehaviour
{
    //Variables used for blur - These are common for most effects
    private NPFrame2 frame;
    public RenderTexture donePow2;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    //Variable to control at which level to synthesise from - higher value = more blur - synth from lower level
    [Range(1, 6)]
    public int blurStrength = 2;

    void Start()
    {
        frame = new NPFrame2("Blur", 8);     

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;

        frame.Analyze(source); 
        frame.GenerateSynthesis("Synth",_SynthesisMode, blurStrength);
        MakeBlur(donePow2);
        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);
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