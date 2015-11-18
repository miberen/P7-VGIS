using UnityEngine;
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

    void Start()
    {
        frame = new NPFrame2("DoF", 8);
        depth = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        depth.Create();
        cam = GetComponent<Camera>();

        donePow2 = new RenderTexture(frame.GetNativePOTRes, frame.GetNativePOTRes, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        Graphics.Blit(source, depth, new Material(Shader.Find("Custom/DepthShader")));
        frame.Analyze(ref source);
        frame.GenerateSynthesis(1, "from1");
        frame.GenerateSynthesis(2, "from2");
        frame.GenerateSynthesis(3, "from3");

        Analstuff = frame.AnalyzeList;
        Synthstuff = frame.GetSynthesis("from1").Pyramid;

        DOF(donePow2);

        frame.MakeNPOT(donePow2);

        Graphics.Blit(frame.GetDoneNPOT, dest);
    }

    void DOF(RenderTexture dest)
    {
        float maxBlurDist = aperture;
        int firstPass = 1;
        int lastPass = 0;
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("DOF"), "depth", depth);

        if (!FixedDepthofField)
        {
            RaycastHit rhit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out rhit, cam.farClipPlane))
                focalLength = Mathf.Lerp(focalLength, rhit.distance, Time.deltaTime * focusSpeed);
        }

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
