/*
 * PerformanceTimer.cs 1.1
 * By Samuel Johansson 2014
 * samuel@phaaxgames.com
 * 
 * Measures performance in your Unity projects.
 * Uses StopWatch which has very good resolution.
 * You can have multiple timers!
 * 
 * Free to use in any project! I would be happy
 * if you gave me some credit or link to my website.
 * www.phaaxgames.com
 * 
 * Drop this script anywhere in your project, but do
 * not assign it to any GameObject.
 * 
 * Example usage:
 * 

	private PTimer timer; // Define your timer object

	void Start() {
		timer = PerformanceTimer.CreateTimer(); // Create and assign your timer
	}

	void Update () {
		PerformanceTimer.MeasurePointBegin(timer); // Begin measuring here
		// Do your stuff here
		PerformanceTimer.MeasurePointEnd(timer); // End measuring here
	}

	void LateUpdate () {
		// Print your collected data
		Debug.Log("Total time: "+(Time.realtimeSinceStartup).ToString("f4")+"s\n"+
				"Processing time: "+(timer.processTimeTotal).ToString("f4")+"ms\n"+
				"Frame number: "+timer.measureCount+"\n"+
				"Current time: "+(timer.measureTime).ToString("f4")+"ms\n"+
				"Shortest time: "+(timer.shortestTime).ToString("f4")+"ms\n"+
				"Longest time: "+(timer.longestTime).ToString("f4")+"ms\n"+
				"Average time. "+(timer.averageTime).ToString("f4")+"ms\n\n");
	}

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class PTimer {
	public int id; 									// Do not modify this id!
	public double processTimeTotal = 0; 			// Total time of all measurements made (milliseconds)
	public double measureStartPoint = 0;			// When our timer started last (milliseconds)
	public double measureEndPoint = 0;				// When our timer stopped last (milliseconds)
	public double measureTime = 0;					// How long the last measure lasted (milliseconds)
	public double longestTime = 0;					// Longest time recorded (milliseconds)
	public double shortestTime = Mathf.Infinity;	// Shortest time recorded (milliseconds)
	public double averageTime = 0;					// Average time (milliseconds)
    public double frameTime = 0;                    // Time since last frame
    public double frameTimeTotal = 0;               // Total frame time
    public double averageFrameTime = 0;             // Average frame time
	public long measureCount = 0;					// Number of measurements made
}

public class PerformanceTimer : MonoBehaviour {

	private static List<Stopwatch> stopWatches = new List<Stopwatch>();

	private static double StopwatchElapsedMS(PTimer timer) {
		long ticks = stopWatches[timer.id].ElapsedTicks;
		double ns = 1000000000.0 * (double)ticks / Stopwatch.Frequency;
		double ms = ns / 1000000.0;
		return ms;
	}

	public static PTimer CreateTimer() {
		Stopwatch newStopwatch = new Stopwatch();
		PTimer newTimer = new PTimer();
		newTimer.id = stopWatches.Count;

		stopWatches.Add (newStopwatch);

		return newTimer;
	}

	public static void RemoveAllTimers() {
		stopWatches.Clear();
	}

	public static void ResetTimer(PTimer timer) {
		if (timer != null) stopWatches[timer.id].Reset();
	}

	public static void MeasurePointBegin(PTimer timer) {
		if (timer != null) {
			stopWatches[timer.id].Start();
			timer.measureStartPoint = StopwatchElapsedMS(timer);
		} else {
			UnityEngine.Debug.LogError("PerformanceTimer error: Timer is null");
		}
	}

	public static void MeasurePointEnd(PTimer timer) {
		if (timer != null) {
			timer.measureEndPoint = StopwatchElapsedMS(timer);
			stopWatches[timer.id].Stop();

			timer.measureTime = timer.measureEndPoint - timer.measureStartPoint;
			timer.measureCount++;
			timer.processTimeTotal += timer.measureTime;
		    timer.frameTime = Time.deltaTime * 1000;
		    timer.frameTimeTotal += timer.frameTime;

			timer.averageTime = timer.processTimeTotal / timer.measureCount;
		    timer.averageFrameTime = timer.frameTimeTotal / timer.measureCount;

			if (timer.measureTime > timer.longestTime) timer.longestTime = timer.measureTime;
			if (timer.shortestTime > timer.measureTime) timer.shortestTime = timer.measureTime;
		} else {
			UnityEngine.Debug.LogError("PerformanceTimer error: Timer is null");
		}
	}
}
