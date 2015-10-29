using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PyramidEffects : MonoBehaviour
{
    #region Publics
    public Camera cam;
    public GameObject plane1;
    public GameObject plane2;
    public GameObject plane3;
    public GameObject plane4;
    public GameObject focusTarget;

    public bool infillIsRange;

    public Color infillColor;
    public Color infillLowRange;
    public Color infillHighRange;

    public ComputeShader ComputeTest;

    public FilterMode FiltMode;
    [Range(2, 7)]
    public int Levels = 2;
    [Range(1, 100)]
    public float focalLength = 1;
    [Range(0.1f, 10)]
    public float FocalSize = 1;
    [Range(0.1f, 10)]
    public float aperture = 1;

    #endregion
    #region Privates

    //Bool to see if nescessary textures and shader variables are initialized
    private int index = 0;
    private bool isInit = false;

    // To check if levels has changed
    private int lastLevels;

    //Creates the nescessary RenderTextures
    public List<RenderTexture> analyzeList = new List<RenderTexture>();
    public List<RenderTexture> synthesizeList = new List<RenderTexture>();
    private RenderTexture done;
    private RenderTexture donePow2;
    private RenderTexture depth;

    //The size of the screen last frame;
    private Vector2 lastScreenSize;

    //List of power of 2s
    private List<int> pow2s = new List<int>();
    #endregion
 
    // Use this for initialization
    void Awake()
    {
        //Generates the list of pow2s
        GenPow2();
    }

    //Start smart
    void Start()
    {
        depth = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        depth.Create();
        //Get the initial screen size
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        lastLevels = Levels;
    }

    void Update()
    {
        focalLength = Vector3.Distance(focusTarget.transform.position, cam.transform.position);
        if (Input.GetKeyDown("k"))
        {
            plane1.GetComponent<Renderer>().material.SetTexture("_MainTex", analyzeList[index]);
            plane2.GetComponent<Renderer>().material.SetTexture("_MainTex", donePow2);
            plane3.GetComponent<Renderer>().material.SetTexture("_MainTex", done);
            plane4.GetComponent<Renderer>().material.SetTexture("_MainTex", depth);
            foreach (RenderTexture rT in analyzeList)
            {
                //Debug.Log("analyze:" + rT.height.ToString() + "x" + rT.width.ToString());
            }
            foreach (RenderTexture rT in synthesizeList)
            {
                //Debug.Log("synthesize" + rT.height.ToString() + "x" + rT.width.ToString());
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index += 1;
            if (index >= synthesizeList.Count)
                index = 0;
            plane1.GetComponent<Renderer>().material.SetTexture("_MainTex", analyzeList[index]);
            plane2.GetComponent<Renderer>().material.SetTexture("_MainTex", donePow2);
            plane3.GetComponent<Renderer>().material.SetTexture("_MainTex", done);
            plane4.GetComponent<Renderer>().material.SetTexture("_MainTex", depth);
        }
    }

    /// <summary>
    /// Overrides the OnRenderImage function.</summary>
    /// <param name="source"> The RenderTexture about to be displayed on the screen.</param>
    /// <param name="destination"> The final RenderTexture actually displayed.</param>
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {    
        
        //Check if the temp texture isn't initialized or the screen size has changed requiring a new size texture, if it has then reinit them // TODO: fix last levels change( dem mem leaks)
        if (!isInit || lastScreenSize != new Vector2(Screen.width, Screen.height) || lastLevels != Levels) Init(source, Levels);

        Refresh(source);

        MakePow2(source);

        Analyze(Levels);

        Synthesize(Levels);

        //Infill();

        Graphics.Blit(source, depth, new Material(Shader.Find("Custom/DepthShader")));
        DOF(donePow2);
        
        MakeNonPow2(donePow2);
        
        //Blit that shit
        Graphics.Blit(done, destination);
        //Graphics.Blit(source, destination);
    }

    /// <summary>
    /// Creates a texture that is the next power of 2, creates it on the GPU then sets the compute shader uniforms.</summary>
    /// <param name="source"> Reference to the source texture.</param>
    void Init(RenderTexture source, int levels)
    {

        // These loops to handle changing levels size dnamically works a bit iffy ( I.E Doesnt fucking work ATM, memleak)
        foreach (RenderTexture rT in analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.DiscardContents();
                rT.Release();
                Destroy(rT);
            }
        }
        foreach (RenderTexture rT in synthesizeList)
        {
            if (rT.IsCreated())
            {
                rT.DiscardContents();
                rT.Release();
                Destroy(rT);
            }
        }

        analyzeList.Clear();
        synthesizeList.Clear();

        int size = NextPow2(source);
        for (int i = 0; i < levels; i++)
        {
            analyzeList.Add(new RenderTexture(pow2s[pow2s.IndexOf(size) - i], pow2s[pow2s.IndexOf(size) - i], 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            analyzeList[i].enableRandomWrite = true;
            analyzeList[i].filterMode = FiltMode;
            analyzeList[i].Create();
        }

        for (int i = 0; i < levels; i++)
        {
            synthesizeList.Add(new RenderTexture(analyzeList[levels - 1 - i].width, analyzeList[levels - 1 - i].height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            synthesizeList[i].enableRandomWrite = true;
            synthesizeList[i].filterMode = FiltMode;
            synthesizeList[i].Create();
        }

        donePow2 = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.filterMode = FiltMode;
        donePow2.Create();

        done = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        done.enableRandomWrite = true;
        done.filterMode = FiltMode;
        done.Create();

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastLevels = levels;
        isInit = true;
    }

    void Refresh(RenderTexture source)
    {
        
        if (done.IsCreated() && donePow2.IsCreated())
        {
            done.Release();
            donePow2.Release();
            done.Create();
            donePow2.Create();
        }
        
        //Check if there is already a texture, if there is then release the old one before making a new one
        foreach (RenderTexture rT in analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
                rT.Create();
            }
        }

        foreach (RenderTexture rT in synthesizeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
                rT.Create();
            }
        }
    }
    /// <summary>
    /// Generates a list of power of 2s up to 13 (8192).</summary>
    void GenPow2()
    {
        for (int i = 1; i < 14; i++)
        {
            pow2s.Add((int)Mathf.Pow(2, i));
        }
    }

    /// <summary>
    /// Finds the power of 2 image which will fit the source image ( 951x100 -> 1024x1024 ).</summary>
    /// <param name="source"> The source RenderTexture to find a fit for.</param>
    int NextPow2(RenderTexture source)
    {
        int biggest = Mathf.Max(source.width, source.height);
        int final = 0;

        foreach (int i in pow2s)
        {
            if (biggest > i) final = pow2s[pow2s.IndexOf(i) + 1];
        }
        return final;
    }

    void MakePow2(RenderTexture source)
    {
        //Set the shader uniforms
        ComputeTest.SetTexture(ComputeTest.FindKernel("MakePow2"), "source", source);
        ComputeTest.SetTexture(ComputeTest.FindKernel("MakePow2"), "dest", analyzeList[0]);

        //Dispatch the compute shader
        ComputeTest.Dispatch(ComputeTest.FindKernel("MakePow2"), (int)Mathf.Ceil(analyzeList[0].width / 32), (int)Mathf.Ceil(analyzeList[0].height / 32), 1);
    }

    void MakeNonPow2(RenderTexture source)
    {
        //Set the shader uniforms
        ComputeTest.SetTexture(ComputeTest.FindKernel("MakeNPow2"), "source", source);
        ComputeTest.SetTexture(ComputeTest.FindKernel("MakeNPow2"), "dest", done);

        //Dispatch the compute shader
        ComputeTest.Dispatch(ComputeTest.FindKernel("MakeNPow2"), (int)Mathf.Ceil(source.width / 32), (int)Mathf.Ceil(source.height / 32), 1);
    }

    void Analyze(int levels)
    {
        for (int i = 0; i < levels - 1; i++)
        {
            ComputeTest.SetTexture(ComputeTest.FindKernel("Analyze"), "source", analyzeList[i]);
            ComputeTest.SetTexture(ComputeTest.FindKernel("Analyze"), "dest", analyzeList[i + 1]);

            ComputeTest.Dispatch(ComputeTest.FindKernel("Analyze"), (int)Mathf.Ceil(analyzeList[i + 1].width / 32), (int)Mathf.Ceil(analyzeList[i + 1].height / 32), 1);
        }
        synthesizeList[0] = analyzeList[levels - 1];
    }

    void Synthesize(int levels)
    {
        for (int i = 0; i < levels - 1; i++)
        {
            ComputeTest.SetTexture(ComputeTest.FindKernel("Synthesize"), "source", synthesizeList[i]);
            ComputeTest.SetTexture(ComputeTest.FindKernel("Synthesize"), "dest", synthesizeList[i + 1]);

            ComputeTest.Dispatch(ComputeTest.FindKernel("Synthesize"), (int)Mathf.Ceil(synthesizeList[i].width / 32), (int)Mathf.Ceil(synthesizeList[i].height / 32), 1);
        }
    }

    void Infill()
    {
        if (infillIsRange)
        {
            ComputeTest.SetInt("isRange", Convert.ToInt32(infillIsRange));
            ComputeTest.SetVector("infillLowRange", new Vector4(infillLowRange.r, infillLowRange.g, infillLowRange.b, infillLowRange.a ));
            ComputeTest.SetVector("infillHighRange", new Vector4( infillHighRange.r, infillHighRange.g, infillHighRange.b, infillHighRange.a));

            ComputeTest.SetTexture(ComputeTest.FindKernel("Infill"), "source", synthesizeList[synthesizeList.Count - Levels/2]);
            ComputeTest.SetTexture(ComputeTest.FindKernel("Infill"), "dest", synthesizeList[synthesizeList.Count - 1]);
            ComputeTest.SetTexture(ComputeTest.FindKernel("Infill"), "infillRead", synthesizeList[synthesizeList.Count - 1]);

            ComputeTest.Dispatch(ComputeTest.FindKernel("Infill"), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].width / 32), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].height / 32), 1);
        }
        else
        {
            ComputeTest.SetInt("isRange", Convert.ToInt32(infillIsRange));
            ComputeTest.SetVector("infillColor", new Vector4( infillColor.r, infillColor.g, infillColor.b, infillColor.a));

            ComputeTest.SetTexture(ComputeTest.FindKernel("Infill"), "source", synthesizeList[synthesizeList.Count - Levels / 2]);
            ComputeTest.SetTexture(ComputeTest.FindKernel("Infill"), "dest", synthesizeList[synthesizeList.Count - 1]);
            ComputeTest.SetTexture(ComputeTest.FindKernel("Infill"), "infillRead", synthesizeList[synthesizeList.Count - 1]);

            ComputeTest.Dispatch(ComputeTest.FindKernel("Infill"), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].width / 32), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].height / 32), 1);
        }
    }

    void DOF(RenderTexture dest)
    {
        ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "depth", depth);
        ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "source", analyzeList[0]);
        for (int i = 0; i <= synthesizeList.Count - 1; i++) // -2 because final texture is output
        {
            ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "DOF" + i , synthesizeList[i]);
        }
        ComputeTest.SetFloat("focalLength", focalLength);
        ComputeTest.SetFloat("focalSize", FocalSize);
        ComputeTest.SetFloat("aperture", aperture);
        ComputeTest.SetFloat("farClipPlane", cam.farClipPlane);

        ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "dest", donePow2);
        ComputeTest.Dispatch(ComputeTest.FindKernel("DOF"), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].width / 32), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].height / 32), 1);
    }

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}