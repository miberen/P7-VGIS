using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class DoF : MonoBehaviour {

    private NPFrame2 frame;
    private Camera cam = null;
    private RenderTexture depth;
    public RenderTexture donePow2;
    public List<RenderTexture> Analstuff = new List<RenderTexture>();
    public List<RenderTexture> Synthstuff = new List<RenderTexture>();

    [Tooltip("Enabling Fixed mode makes the focal length static and independant on where you are looking")]
    public bool FixedDepthofField = false;
    [Range(1, 100)]
    public float focalLength = 2;
    [Range(0.1f, 10)]
    public float FocalSize = 0.8f;
    [Range(1.0f, 30)]
    [Tooltip("F-Stop - The higher the value the more is in focus")]
    public float aperture = 4.0f;

    void Start ()
    {
        frame = new NPFrame2("DoF", 5);
        depth = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        depth.Create();
        cam = GetComponent<Camera>();

        donePow2 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        Graphics.Blit(source, depth, new Material(Shader.Find("Custom/DepthShader")));
        frame.Analyze(ref source);
        frame.GenerateSynthesis(3, "default");

        Analstuff = frame.AnalyzeList;
        Synthstuff = frame.GetSynthesis("default").Pyramid;

        Graphics.Blit(source, dest);

    }

    void DOF(RenderTexture dest)
    {

        float maxBlurDist = aperture;
        int firstPass = 1;
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "depth", depth);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "source", frame.AnalyzeList[0]);

        if (!FixedDepthofField)
        {
            RaycastHit rhit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out rhit, cam.farClipPlane))
                focalLength = rhit.distance;

        }

        frame.GetShader.SetInt("firstPass", firstPass);
        frame.GetShader.SetFloat("focalLength", focalLength);
        frame.GetShader.SetFloat("focalSize", FocalSize);
        frame.GetShader.SetFloat("farClipPlane", cam.farClipPlane);

        if (firstPass == 1)
        {
            firstPass = 0;
            frame.GetShader.Dispatch(frame.GetShader.FindKernel("DOF"), (int)Mathf.Ceil(frame.GetSynthesis("default").Pyramid[frame.GetSynthesis("default").Pyramid.Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("default").Pyramid[frame.GetSynthesis("default").Pyramid.Count - 1].height / 32), 1);
            frame.GetShader.SetInt("firstPass", firstPass);
        }
        if (firstPass == 0)
        {
            for (int i = 0; i <= 4; i++)
            {
                float blurInterval = maxBlurDist / 3;
                float far1 = cam.nearClipPlane + focalLength + FocalSize + (blurInterval * i);
                float far2 = cam.nearClipPlane + focalLength + FocalSize + (blurInterval * (i + 1));
                float near1 = Mathf.Clamp(cam.nearClipPlane + focalLength - FocalSize - (blurInterval * i), 0, cam.farClipPlane);
                float near2 = Mathf.Clamp(cam.nearClipPlane + focalLength - FocalSize - (blurInterval * (i + 1)), 0, cam.farClipPlane);

                //Debug.Log(far1 + " \t" + far2 + " \t" + near1 + " \t" + near2);
                //Do Calc
                frame.GetShader.SetFloats("blurPlanes", new float[] { far1, far2, near1, near2 });
                //Set Texture
                frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "DOF0", frame.GetSynthesis("default").Pyramid[i]);

                frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "dest", donePow2);
                frame.GetShader.Dispatch(frame.GetShader.FindKernel("DOF"), (int)Mathf.Ceil(frame.GetSynthesis("default").Pyramid[frame.GetSynthesis("default").Pyramid.Count - 1].width / 32), (int)Mathf.Ceil(frame.GetSynthesis("default").Pyramid[frame.GetSynthesis("default").Pyramid.Count - 1].height / 32), 1);
            }
        }

    }
}
