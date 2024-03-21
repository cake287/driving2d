using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Timing : MonoBehaviour
{
    [NonSerialized] public int gateCount = 0;
    private int nextGate = 0; // next gate the player must pass through in the course

    [SerializeField] TextMeshProUGUI timingText;
    float time = 0;
    [NonSerialized] public bool timerRunning = false;

    [SerializeField] TextMeshProUGUI fastestTimeText;
    Dictionary<int, float> fastestTimes = new Dictionary<int, float>();

    int currentLevel = 0;
    [SerializeField] TextMeshProUGUI levelNumberText;

    private string formatTime(float time)
    {
        if (float.IsInfinity(time))
            return "-:--.--";

        int minutes = (int)time / 60;
        int seconds = (int)time % 60;
        int milliseconds = (int)(time % 1 * 100);

        return $"{minutes}:{seconds:00}.{milliseconds:00}";
    }


    public void ResetTimer()
    {
        timerRunning = false;
        nextGate = 0;
        time = 0;
        timingText.text = formatTime(time);
    }

    private void OnTriggerEnter2D(Collider2D gate)
    {
        int gateID = int.Parse(gate.gameObject.name.Substring(4));
        if (gateID == nextGate)
        {
            if (gateID == 0)
            {
                // update fastest time
                if (timerRunning)
                {
                    fastestTimes[currentLevel] = Mathf.Min(fastestTimes[currentLevel], time);
                    fastestTimeText.text = formatTime(fastestTimes[currentLevel]);
                }

                timerRunning = true;
                time = 0;

                for (int i = 1; i < gateCount; i++)
                    TrackControl.SetGateState(GameObject.Find("gate" + i), TrackControl.GateState.Disabled);
            } else
                TrackControl.SetGateState(gate.gameObject, TrackControl.GateState.Enabled);

            nextGate++;
            if (nextGate == gateCount)
                nextGate = 0;
        }
    }

    private void FixedUpdate()
    {
        if (timerRunning) { 
            time += Time.fixedDeltaTime;
            timingText.text = formatTime(time);
        }
    }


    public void LevelChanged(int newLevelID)
    {
        currentLevel = newLevelID;
        if (fastestTimes.ContainsKey(currentLevel))
            fastestTimeText.text = formatTime(fastestTimes[currentLevel]);
        else
            fastestTimes.Add(currentLevel, float.PositiveInfinity);

        levelNumberText.text = "Level " + (newLevelID + 1);
    }

}
