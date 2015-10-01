using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PyramidEffects : MonoBehaviour
{
    #region Publics
    public GameObject plane1;
    public ComputeShader computeTest;
    #endregion
    #region Privates

    private bool isInit = false;
    //Creates the nescessary RenderTextures
    private RenderTexture ping;
    private RenderTexture pong;
    private RenderTexture temp;

    private Vector2 lastScreenSize;

    //Creates the Material to be used in the Graphics.Blit function
    private Material material;

    //List of power of 2s
    private List<int> pow2s = new List<int>();
    #endregion

    // Use this for initialization
    void Awake()
    {
        //Generates the list of pow2s
        genPow2();

        //Initiates the Material with pyramid shader 
        // TODO: Make the actual shader
        material = new Material(Shader.Find("Hidden/PyramidBlur"));
    }

    void Start()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    /// <summary>
    /// Overrides the OnRenderImage function.</summary>
    /// <param name="source"> The RenderTexture about to be displayed on the screen.</param>
    /// <param name="destination"> The final RenderTexture actually displayed.</param>
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!isInit || lastScreenSize != new Vector2(Screen.width, Screen.height)) Init(source);

        computeTest.Dispatch(0, temp.width / 8, temp.height / 8, 1);   

        Graphics.Blit(source, destination);
    }

    void Init(RenderTexture source)
    {
        int size = nextPow2(source);

        if (temp != null) temp.Release();

        temp = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        temp.enableRandomWrite = true;
        temp.generateMips = true;
        temp.Create();

        computeTest.SetTexture(0, "source", source);
        computeTest.SetTexture(0, "dest", temp);

        plane1.GetComponent<Renderer>().material.SetTexture("_MainTex", temp);

        isInit = true;
    }
    /// <summary>
    /// Generates a list of power of 2s up to 13 (8192).</summary>
    void genPow2()
    {
        for (int i = 1; i < 14; i++)
        {
            pow2s.Add((int)Mathf.Pow(2, i));
        }
    }

    /// <summary>
    /// Finds the power of 2 image which will fit the source image ( 951x100 -> 1024x1024 ).</summary>
    /// <param name="source"> The source RenderTexture to find a fit for.</param>
    int nextPow2(RenderTexture source)
    {
        int biggest = Mathf.Max(source.width, source.height);
        int final = 0;

        foreach (int i in pow2s)
        {
            if (biggest > i) final = pow2s[pow2s.IndexOf(i) + 1];
        }
        return final;
    }
}