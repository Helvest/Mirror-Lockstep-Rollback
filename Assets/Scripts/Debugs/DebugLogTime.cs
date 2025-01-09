using System.Collections.Generic;
using UnityEngine;

public class DebugLogTime : MonoBehaviour
{
    public int startAtFrame = 10;
    public int testForFrames = 10;

    private int nbOfFrame;

    public bool logAwake = true;

    public bool logOnEnable = true;

    public bool logStart = true;

    public bool logFixedUpdate = true;

    public bool logUpdate = true;

    private void Awake()
    {
        if (logAwake)
        {
            InitDebugLog("Awake");
        }
    }

    private void OnEnable()
    {
        if (logOnEnable)
        {
            InitDebugLog("OnEnable");
        }
    }

    private void Start()
    {
        if (logStart)
        {
            InitDebugLog("Start");
        }
    }

    private float fixedTimeCounter;

    private float preFixedTimeCounter;

    private void FixedUpdate()
    {
        if (testForFrames > 0)
        {
            preFixedTimeCounter = fixedTimeCounter;
            fixedTimeCounter += Time.fixedDeltaTime;

            if (logFixedUpdate)
            {
                FixedDebugLog("FixedUpdate");
            }
        }
    }

    private bool isFist = true;

    private float previousDeltaTime;

    private float timeByDelta;

    private float correctTime;
    private float correctDeltatime;

    private float predicTime;
    private float predictDeltatime;

    private const int smoothLength = 10;

    private List<float> deltatimeSaved = new List<float>(smoothLength);

    private void Update()
    {
        nbOfFrame++;

        if (nbOfFrame >= startAtFrame && testForFrames >= 0)
        {
            testForFrames--;

            if (isFist)
            {
                isFist = false;

                correctTime = Time.time;
            }
            else
            {
                if (logUpdate)
                {
                    DebugLog("Update");
                }
            }

            //___________________
            //Try to correct by array

            if (deltatimeSaved.Count == smoothLength)
            {
                deltatimeSaved.Remove(0);
            }

            deltatimeSaved.Add(Time.deltaTime);

            float allDelta = 0f;

            foreach (float delta in deltatimeSaved)
            {
                allDelta += delta;
            }

            predictDeltatime = allDelta / deltatimeSaved.Count;

            predicTime = Time.time + predictDeltatime;

            //___________________
            //Try to correct by difference

            float correct = correctTime - Time.time;
            correctDeltatime = Time.deltaTime - correct;
            correctTime = Time.time + correctDeltatime;

            //___________________
            //save previous frame data

            timeByDelta = Time.time + Time.deltaTime;

            previousDeltaTime = Time.deltaTime;


            if (testForFrames < 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
         		Application.OpenURL(webplayerQuitURL);
#else
         		Application.Quit();
#endif
            }
        }
    }

    private void InitDebugLog(string message)
    {
        Debug.Log(message + " | Frame: " + Time.frameCount + " | Realtime: " + Time.realtimeSinceStartup
                  + "\ntime: " + Time.time
                  + "\ndeltaTime: " + Time.deltaTime
                  + "\nsmoothDeltaTime: " + Time.smoothDeltaTime
                  + "\n----------------"
        );
    }

    private void FixedDebugLog(string message)
    {
        Debug.Log(message + " | Frame: " + Time.frameCount + " | Realtime: " + Time.realtimeSinceStartup
                  + "\nfixedTime: " + Time.fixedTime
                  + "\nfixedDeltaTime: " + Time.fixedDeltaTime
                  + "\n-"
                  + "\nendFixedFrameTime: " + preFixedTimeCounter + " -> " + fixedTimeCounter
                  + "\nendFixedDecal: " + (preFixedTimeCounter - Time.fixedTime)
                  + "\n----------------"
        );
    }

    private void DebugLog(string message)
    {
        Debug.Log($"{message} | Frame: {Time.frameCount:00000} | Realtime: {Time.realtimeSinceStartup:F5}"
                  + $"\n" + $"time: {Time.time:F5}"
                  + $"\n" + $"deltaTime: {Time.deltaTime:F5}"
                  + $"\n" + $"smoothDeltaTime: {Time.smoothDeltaTime:F5}"
                  + $"\n-"
                  + $"\n" + $"previousDeltaTime: {previousDeltaTime:F5}"
                  + $"\n" + $"Decal: {previousDeltaTime - Time.deltaTime:F5}"
                  + $"\n-"
                  + $"\n" + $"timeByDelta: {timeByDelta:F5}"
                  + $"\n" + $"Decal: {timeByDelta - Time.time:F5}"
                  + $"\n-"
                  + $"\n" + $"correctTime: {correctTime:F5}"
                  + $"\n" + $"Decal: {correctTime - Time.time:F5}"
                  + $"\n-"
                  + $"\n" + $"predicTime: {predicTime:F5}"
                  + $"\n" + $"predictDeltatime: {predictDeltatime:F5}"
                  + $"\n" + $"Decal: {predicTime - Time.time:F5}"
                  + $"\n-"
                  + $"\n" + $"----------------"
        );
    }
}