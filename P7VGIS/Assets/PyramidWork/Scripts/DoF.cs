using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class DoF : MonoBehaviour
{
    //Variables used for blur - These are common for most effects
    private NPFrame2 frame;
    private Camera cam = null;
    private RenderTexture depth;
    public RenderTexture donePow2;
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    //this stuff is for show & tell @ presentation stuff - to see in inspector
    //LineRenderer line;
    //Vector3 offsetUP = new Vector3(0.33f, 0.33f, 0);
    public List<RenderTexture> AnalysisList = new List<RenderTexture>();
    public List<RenderTexture> SynthesisList = new List<RenderTexture>();

    [Tooltip("Enabling Fixed mode makes the focal length static and independant of where you are looking")]
    public bool FixedDepthofField = false;

    [Range(1, 100)]
    [Tooltip("Distance from the camera to the point you want in focus - in Unity units")]
    public float focalDistance = 2;
 
    [Range(0.1f, 10)]
    [Tooltip("Area within which the image will be in focus (on each side of the focalDistance)")]
    public float FocalSize = 0.8f;

    [Range(1.0f, 30)]
    [Tooltip("F-Stop - The higher the value the deeper the focus")]
    public float aperture = 4.0f;

    [Range(4.0f, 10.0f)]
    [Tooltip("Controls how fast the depth of field should adjust when changing view angle (only relevant when in fixed depth of field mode)")]
    public float focusSpeed = 4.0f;

    //Show and tell value used to cycle through the different levels of the different pyramids
    private int imageIndex = 0;
    private int listIndex = 0;

    void Start()
    {        
        //line = GetComponent<LineRenderer>();
        frame = new NPFrame2("DoF", 4);
        cam = GetComponent<Camera>();

        depth = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        depth.Create();

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.SetTextureFormat = _textureFormat;
        frame.SetFilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;

        //We blit to a depth texture using the implemented DepthShader
        Graphics.Blit(source, depth, new Material(Shader.Find("Custom/DepthShader")));

        frame.Analyze(source);
        AnalysisList = frame.AnalyzeList;

        //Generate three synthesis pyramids to achieve stronger blur widths
        frame.GenerateSynthesis("from1",_SynthesisMode, 1);
        frame.GenerateSynthesis("from2",_SynthesisMode, 2);
        frame.GenerateSynthesis("from3",_SynthesisMode, 3);
        SynthesisList = frame.GetSynthesis("from1").Pyramid;

        DOF(donePow2);

        frame.MakeNPOT(donePow2);

        //Graphics.Blit(frame.GetDoneNPOT, dest);

        //Switch for toggling between the different pyramids for presentation purposes
        switch (listIndex)
        {
            case 0:
                Graphics.Blit(frame.GetDoneNPOT, dest);
                break;
            case 1:
                Graphics.Blit(AnalysisList[imageIndex], dest);
                break;
            case 2:
                Graphics.Blit(frame.GetSynthesis("from3").Pyramid[imageIndex], dest);
                break;
            case 3:
                Graphics.Blit(frame.GetSynthesis("from2").Pyramid[imageIndex], dest);
                break;
            case 4:
                Graphics.Blit(frame.GetSynthesis("from1").Pyramid[imageIndex], dest);
                break;
            default:
                break;
        }
    }

    //Entire update function used for presentation purposes to toggle between different levels in different pyramids
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            imageIndex += 1;
            if (imageIndex >= AnalysisList.Count)
                imageIndex = 0;
            
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            imageIndex -= 1;
            if (imageIndex >= AnalysisList.Count)
                imageIndex = 0;
            if (imageIndex == -1)
                imageIndex = AnalysisList.Count - 1;

        }

        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            listIndex += 1;
            imageIndex = 0;
            if (listIndex >= 5)
                listIndex = 0;

        }

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            listIndex -= 1;
            imageIndex = 0;
            if (listIndex >= 5)
                listIndex = 0;
            if (listIndex == -1)
                listIndex = 4;
        }
    }

    //Used for dynamic depth of field, where the blur changes depending on the distance to the object you are looking at. Calculated using a raycast in the forwards direction from the camera to the far clipping plane. If nothing is hit, a default focal distance is set. The focal distance is lerped with the focus speed to control how fast the focus should adjust.
    void OnPostRender()
    {
        if (!FixedDepthofField)
        {
            RaycastHit rhit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out rhit, cam.farClipPlane))
            {
                focalDistance = Mathf.Lerp(focalDistance, rhit.distance, Time.deltaTime * focusSpeed);
                //line.SetPosition(0, transform.TransformPoint(offsetUP));
                //line.SetPosition(1, rhit.point);
            }
            else
            {
                   focalDistance = Mathf.Lerp(focalDistance, 2, Time.deltaTime * focusSpeed);
            //    line.SetPosition(0, transform.TransformPoint(offsetUP));
            //    line.SetPosition(1, cam.transform.forward * 100);
           }
        }
    }

    /// <summary>
    /// Applies the depth of field given a destination texture using the values set in the inspector
    /// </summary>
    /// <param name="dest">Destination texture - PoT</param>
    void DOF(RenderTexture dest)
    {
        //These variables are used to control when to finalize the image (fill in whatever is missing once the three levels of blur have been applied)
        int firstPass = 1;
        int lastPass = 0;
        

        frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "depth", depth);
        
        //Send over the variables used to the compute shader. 
        frame.GetShader.SetInt("firstPass", firstPass);
        frame.GetShader.SetFloat("focalDistance", focalDistance);
        frame.GetShader.SetFloat("focalSize", FocalSize);
        frame.GetShader.SetFloat("nearClipPlane", cam.nearClipPlane);
        frame.GetShader.SetFloat("farClipPlane", cam.farClipPlane);

        //The first pass of the compute shader which computes the part of the image that is in focus
        if (firstPass == 1)
        {
            firstPass = 0;
            frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.AnalyzeList[0]);
            frame.GetShader.Dispatch(frame.GetShader.FindKernel("DOF"), (int)Mathf.Ceil(frame.GetSynthesis("from3")[frame.GetSynthesis("from3").Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("from3")[frame.GetSynthesis("from3").Count - 1].height / 32), 1);
            frame.GetShader.SetInt("firstPass", firstPass);
        }

        //After the first pass is finished the different blur widths are applied to the image by iterating the process. This iteration is done in order to store less textures in memory. 
        if (firstPass == 0)
        {
            frame.GetShader.SetInt("lastPass", lastPass);
            for (int i = 0; i < 3; i++)
            {
                float blurInterval = aperture / 3;
                float far1 = cam.nearClipPlane + focalDistance + FocalSize + (blurInterval * i);
                float far2 = cam.nearClipPlane + focalDistance + FocalSize + (blurInterval * (i + 1));
                float near1 = Mathf.Clamp(cam.nearClipPlane + focalDistance - FocalSize - (blurInterval * i), 0, cam.farClipPlane);
                float near2 = Mathf.Clamp(cam.nearClipPlane + focalDistance - FocalSize - (blurInterval * (i + 1)), 0, cam.farClipPlane);

                frame.GetShader.SetFloats("blurPlanes", new float[] { far1, far2, near1, near2 });
                if (i == 0)
                {
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("from1")[frame.GetSynthesis("from1").Count-1]);
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF1", frame.AnalyzeList[0]);
                }

                if (i == 1)
                {
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("from2")[frame.GetSynthesis("from2").Count - 1]);
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF1", frame.GetSynthesis("from1")[frame.GetSynthesis("from1").Count - 1]);
                }

                if (i == 2)
                {
                    lastPass = 1;
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("from3")[frame.GetSynthesis("from3").Count - 1]);
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF1", frame.GetSynthesis("from2")[frame.GetSynthesis("from2").Count - 1]);
                    frame.GetShader.SetInt("lastPass", lastPass);
                    lastPass = 0;
                }

                frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "dest", donePow2);
                frame.GetShader.Dispatch(frame.GetShader.FindKernel("DOF"), (int)Mathf.Ceil(frame.GetSynthesis("from3")[frame.GetSynthesis("from3").Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("from3")[frame.GetSynthesis("from3").Count - 1].height / 32), 1);
            }
        }
    }
}
