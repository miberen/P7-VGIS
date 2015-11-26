﻿using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class DoF : MonoBehaviour
{
    private NPFrame2 frame;
    private Camera cam = null;
    private RenderTexture depth;
    public RenderTexture donePow2;
    public RenderTexture doneNPOT;
    public List<RenderTexture> Analstuff = new List<RenderTexture>();
    public List<RenderTexture> Synthstuff = new List<RenderTexture>();
    public NPFrame2.AnalysisMode _AnalysisMode;
    public NPFrame2.SynthesisMode _SynthesisMode;
    public FilterMode _filtMode;
    public RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    ////this stuff is for show & tell @ computer graphics presentation
    //LineRenderer line;
    //Vector3 offsetUP = new Vector3(0.33f, 0.33f, 0);

    [Tooltip("Enabling Fixed mode makes the focal length static and independant on where you are looking")]
    public bool FixedDepthofField = false;
    [Range(1, 100)]
    public float focalLength = 2;
    [Range(0.1f, 10)]
    public float FocalSize = 0.8f;
    [Range(1.0f, 30)]
    [Tooltip("F-Stop - The higher the value the more is in focus")]
    public float aperture = 4.0f;
    [Range(4.0f, 10.0f)]
    public float focusSpeed = 4.0f;

    private int imageIndex = 0;
    private int listIndex = 0;

    void Start()
    {
        
        //line = GetComponent<LineRenderer>();
        frame = new NPFrame2("DoF", 8);
        
        depth = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        depth.Create();
        cam = GetComponent<Camera>();

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, frame.TextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.TextureFormat = _textureFormat;
        frame.FilterMode = _filtMode;
        frame.SetAnalysisMode = _AnalysisMode;
        Graphics.Blit(source, depth, new Material(Shader.Find("Custom/DepthShader")));
        frame.Analyze(ref source);
        frame.GenerateSynthesis(1, "from1",_SynthesisMode);
        frame.GenerateSynthesis(2, "from2",_SynthesisMode);
        frame.GenerateSynthesis(3, "from3",_SynthesisMode);

        Analstuff = frame.AnalyzeList;
        Synthstuff = frame.GetSynthesis("from1").Pyramid;

        DOF(donePow2);

        frame.MakeNPOT(donePow2);

        //Graphics.Blit(frame.GetDoneNPOT, dest);


        switch (listIndex)
        {
            case 0:
                Graphics.Blit(frame.GetDoneNPOT, dest);
              //  Debug.Log("original");
                break;
            case 1:
                Graphics.Blit(Analstuff[imageIndex], dest);
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
                //Debug.Log("U dun goofed");
                break;
        }
        //RenderTexture aids = dest;
        //Debug.Log(aids.sRGB.ToString());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            imageIndex += 1;
            if (imageIndex >= Analstuff.Count)
                imageIndex = 0;
            
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            imageIndex -= 1;
            if (imageIndex >= Analstuff.Count)
                imageIndex = 0;
            if (imageIndex == -1)
                imageIndex = Analstuff.Count - 1;

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
    void OnPostRender()
    {
        if (!FixedDepthofField)
        {
            RaycastHit rhit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out rhit, cam.farClipPlane))
            {
                focalLength = Mathf.Lerp(focalLength, rhit.distance, Time.deltaTime * focusSpeed);
                //line.SetPosition(0, transform.TransformPoint(offsetUP));
                //line.SetPosition(1, rhit.point);
            }
            else
            {
                focalLength = Mathf.Lerp(focalLength, 2, Time.deltaTime * focusSpeed);
                //line.SetPosition(0, transform.TransformPoint(offsetUP));
                //line.SetPosition(1, cam.transform.forward * 100);
            }
        }
    }

    void DOF(RenderTexture dest)
    {
        float maxBlurDist = aperture;
        int firstPass = 1;
        int lastPass = 0;
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "depth", depth);

        frame.GetShader.SetInt("firstPass", firstPass);
        frame.GetShader.SetFloat("focalLength", focalLength);
        frame.GetShader.SetFloat("focalSize", FocalSize);
        frame.GetShader.SetFloat("nearClipPlane", cam.nearClipPlane);
        frame.GetShader.SetFloat("farClipPlane", cam.farClipPlane);

        if (firstPass == 1)
        {
            firstPass = 0;
            frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.AnalyzeList[0]);
            frame.GetShader.Dispatch(frame.GetShader.FindKernel("DOF"), (int)Mathf.Ceil(frame.GetSynthesis("from3").Pyramid[frame.GetSynthesis("from3").Pyramid.Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("from3").Pyramid[frame.GetSynthesis("from3").Pyramid.Count - 1].height / 32), 1);
            frame.GetShader.SetInt("firstPass", firstPass);
        }
        if (firstPass == 0)
        {
            frame.GetShader.SetInt("lastPass", lastPass);
            for (int i = 0; i < 3; i++)
            {
                float blurInterval = maxBlurDist / 3;
                float far1 = cam.nearClipPlane + focalLength + FocalSize + (blurInterval * i);
                float far2 = cam.nearClipPlane + focalLength + FocalSize + (blurInterval * (i + 1));
                float near1 = Mathf.Clamp(cam.nearClipPlane + focalLength - FocalSize - (blurInterval * i), 0, cam.farClipPlane);
                float near2 = Mathf.Clamp(cam.nearClipPlane + focalLength - FocalSize - (blurInterval * (i + 1)), 0, cam.farClipPlane);

                frame.GetShader.SetFloats("blurPlanes", new float[] { far1, far2, near1, near2 });
                if (i == 0)
                {
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("from1").Pyramid[frame.GetSynthesis("from1").Pyramid.Count - 1]);
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF1", frame.AnalyzeList[0]);
                }

                if (i == 1)
                {
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("from2").Pyramid[frame.GetSynthesis("from2").Pyramid.Count - 1]);
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF1", frame.GetSynthesis("from1").Pyramid[frame.GetSynthesis("from1").Pyramid.Count - 1]);
                }

                if (i == 2)
                {
                    lastPass = 1;
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("from3").Pyramid[frame.GetSynthesis("from3").Pyramid.Count - 1]);
                    frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF1", frame.GetSynthesis("from2").Pyramid[frame.GetSynthesis("from2").Pyramid.Count - 1]);
                    frame.GetShader.SetInt("lastPass", lastPass);
                    lastPass = 0;
                }
                frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "dest", donePow2);
                frame.GetShader.Dispatch(frame.GetShader.FindKernel("DOF"), (int)Mathf.Ceil(frame.GetSynthesis("from3").Pyramid[frame.GetSynthesis("from3").Pyramid.Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("from3").Pyramid[frame.GetSynthesis("from3").Pyramid.Count - 1].height / 32), 1);


            }
        }

    }
}
