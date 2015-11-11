using UnityEngine;
using System.Collections;

public class DoF : MonoBehaviour {

    [Tooltip("Enabling Fixed mode makes the focal length static and independant on where you are looking")]
    public bool FixedDepthofField = false;
    [Range(1, 100)]
    public float focalLength = 2;
    [Range(0.1f, 10)]
    public float FocalSize = 0.8f;
    [Range(1.0f, 30)]
    [Tooltip("F-Stop - The higher the value the more is in focus")]
    public float aperture = 4.0f;

    void DOF(RenderTexture dest)
    {
        float maxBlurDist = aperture;
        int firstPass = 1;
        ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "depth", depth);
        ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "source", analyzeList[0]);

        if (!FixedDepthofField)
        {
            RaycastHit rhit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out rhit, cam.farClipPlane))
                focalLength = rhit.distance;

        }

        ComputeTest.SetInt("firstPass", firstPass);
        ComputeTest.SetFloat("focalLength", focalLength);
        ComputeTest.SetFloat("focalSize", FocalSize);
        ComputeTest.SetFloat("farClipPlane", cam.farClipPlane);

        if (firstPass == 1)
        {
            firstPass = 0;
            ComputeTest.Dispatch(ComputeTest.FindKernel("DOF"), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].width / 32), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].height / 32), 1);
            ComputeTest.SetInt("firstPass", firstPass);
        }
        if (firstPass == 0)
        {
            for (int i = 0; i <= 3; i++)
            {
                float blurInterval = maxBlurDist / 3;
                float far1 = cam.nearClipPlane + focalLength + FocalSize + (blurInterval * i);
                float far2 = cam.nearClipPlane + focalLength + FocalSize + (blurInterval * (i + 1));
                float near1 = Mathf.Clamp(cam.nearClipPlane + focalLength - FocalSize - (blurInterval * i), 0, cam.farClipPlane);
                float near2 = Mathf.Clamp(cam.nearClipPlane + focalLength - FocalSize - (blurInterval * (i + 1)), 0, cam.farClipPlane);

                //Debug.Log(far1 + " \t" + far2 + " \t" + near1 + " \t" + near2);
                //Do Calc
                ComputeTest.SetFloats("blurPlanes", new float[] { far1, far2, near1, near2 });
                //Set Texture
                ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "DOF0", synthesizeList[i]);

                ComputeTest.SetTexture(ComputeTest.FindKernel("DOF"), "dest", donePow2);
                ComputeTest.Dispatch(ComputeTest.FindKernel("DOF"), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].width / 32), (int)Mathf.Ceil(synthesizeList[synthesizeList.Count - 1].height / 32), 1);
            }
        }

    }
}
