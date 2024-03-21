using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR;

public class TrackControl : MonoBehaviour
{
    public int currentTrack = 0;
    [SerializeField] List<GameObject> trackPrefabs = new List<GameObject>();

    [SerializeField]
    GameObject gatePrefab;

    private GameObject car;
    private GameObject track;
    private List<GameObject> gates = new List<GameObject>();
    private SplineContainer splineContainer;
    


    public enum GateState { Disabled, Enabled, StartFinish }

    public static void SetGateState(GameObject gate, GateState state)
    {
        Color c = Color.white;
        switch (state)
        {
            case GateState.Disabled:
                c = new Color(1, 0.25f, 0.25f, 0.75f);
                break;
            case GateState.Enabled:
                c = new Color(0.25f, 1, 0.25f, 0.75f);
                break;
            case GateState.StartFinish:
                c = new Color(1, 1, 0.25f, 0.75f);
                break;
        }
        gate.transform.Find("bar").GetComponent<SpriteRenderer>().color = c;
    }

    // gets the spline ratio t for a knot on the curve
    float getSplineParam(BezierKnot k)
    {
        float3 temp;
        float t;
        SplineUtility.GetNearestPoint(splineContainer.Spline, k.Position, out temp, out t);

        return t;
    }


    // update currentTrack then call switchTrack()
    void switchTrack()
    {
        // destroy old track and gates
        if (track != null)
        {
            Destroy(track);
            foreach (GameObject gate in gates)
                Destroy(gate);
            gates = new List<GameObject>();
        }



        track = Instantiate(trackPrefabs[currentTrack], this.transform);
        splineContainer = track.GetComponent<SplineContainer>();
        car.GetComponent<CarControl>().trackSpline = splineContainer;
        car.GetComponent<CarControl>().ResetCar();
        car.GetComponent<Timing>().LevelChanged(currentTrack);



        // gates ensure that the player does not skip parts of the track
        // the track is a spline defined by knots
        // the curve tends to be fairly straight at the midpoints of these knots, so i am placing the gates there
        // (at every other midpoint)

        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        Spline spline = splineContainer.Spline;


        // the first gate is placed behind knots[0] - at the midpoint of the first and last know
        // the last knot will have a parameter value of something like 0.95
        // if we try to find the midpoint with this value, the first gate will end up being at halfway through the track since (0.95 + 0) / 2 = ~0.5
        // so we transform the first prevT to make it something like -0.05 so that the midpoint is behind
        float prevT = getSplineParam(spline.Knots.Last()) - 1;

        int i = 0;
        foreach (BezierKnot thisKnot in spline.Knots)
        {
            float thisT = getSplineParam(thisKnot);

            if (i % 2 == 0)
            {
                float midT = (prevT + thisT) * 0.5f;
                if (midT < 0) // for first gate - midT will be negative (unity caps at 0 instead of looping the values)
                    midT += 1;

                Vector3 gatePos = spline.EvaluatePosition(midT);
                gatePos.z = -15;
                Vector3 gateDir = spline.EvaluateTangent(midT);

                GameObject gate = Instantiate(gatePrefab, gatePos, Quaternion.FromToRotation(Vector3.up, gateDir), this.transform);
                gates.Add(gate);

                SetGateState(gate, i == 0 ? GateState.StartFinish : GateState.Disabled);

                int gateID = i / 2;
                gate.name = "gate" + gateID;
                car.GetComponent<Timing>().gateCount = gateID + 1;
            }

            prevT = thisT;
            i++;
        }
    }


    void Start()
    {
        car = GameObject.Find("car");
        switchTrack();



        
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            currentTrack--;
            if (currentTrack < 0) currentTrack = trackPrefabs.Count - 1;
            switchTrack();
        } 
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            currentTrack++;
            if (currentTrack > trackPrefabs.Count - 1) currentTrack = 0;
            switchTrack();
        }
    }


    private void DebugDrawPoint(Vector3 p, Color c, float diameter = 0.5f)
    {
        Debug.DrawRay(new(p.x - diameter, p.y, p.z), 2 * diameter * Vector3.right, c);
        Debug.DrawRay(new(p.x, p.y - diameter, p.z), 2 * diameter * Vector3.up, c);
    }
    private void DebugDrawPoint(float3 p, Color c)
    {
        DebugDrawPoint(new Vector3(p.x, p.y, p.z), c);
    }

    private void DebugDrawPoint(float2 p, Color c)
    {
        DebugDrawPoint(new Vector3(p.x, p.y, 100), c);
    }


}
