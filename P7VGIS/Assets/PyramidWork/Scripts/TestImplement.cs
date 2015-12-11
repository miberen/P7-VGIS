using UnityEngine;

public class TestImplement : MonoBehaviour
{
    private PTimer timer;
    private int counter = 0;
    float realTime = 0;
    float subtractTime = 0;

    private NPFrame2 frame;

    void Awake()
    {
        timer = PerformanceTimer.CreateTimer(); // Create and assign timer
        frame = new NPFrame2("main", 7);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (counter < 60)
            subtractTime = Time.realtimeSinceStartup;
        if (counter > 60)
        {
            if (counter < 1061)
                PerformanceTimer.MeasurePointBegin(timer);
        }

        frame.Analyze(source);
        frame.GenerateSynthesis("LOL", sourceLevel: 2);
        Graphics.Blit(source, destination);

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
}
