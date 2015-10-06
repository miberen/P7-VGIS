using UnityEngine;
using System.Collections;

public class MessingWComputerShaders : MonoBehaviour {

    public ComputeShader shader;

	// Use this for initialization
	void Start () {

        ComputeBuffer buffer = new ComputeBuffer(4, sizeof(int));

        shader.SetBuffer(0, "buffer1", buffer);
        shader.Dispatch(0, 1, 1, 1);
        int[] data = new int[4];
        buffer.GetData(data);

        for (int i = 0; i < 4; i++)
            Debug.Log(data[i]);

        buffer.Release();
	}

}
