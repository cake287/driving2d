using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using static UnityEditor.PlayerSettings;
using UnityEngine.UIElements;

public class CarControl : MonoBehaviour
{
    public Vector2 velocity { get; private set; } = Vector2.zero;
    Vector2 acceleration = Vector2.zero;
    [SerializeField] Vector2 bodyDir = Vector2.up; // direction of the body of the car


    [SerializeField] float engineForce = 0;

    float accelBrakeInput = 0;
    float steerInput = 0;

    [SerializeField] float maxSpeed = 25;
    [SerializeField] float maxEnginePower = 6;

    [SerializeField, Tooltip("The proportion of engine power used at a given velocity (where velocity is a ratio of max speed)")] 
    AnimationCurve enginePower;
    
    [SerializeField] float maxLongFriction = 0.5f; // max longitudinal friction
    [SerializeField] float grassLongFriction = 1;

    [SerializeField] float maxBrakingForce = 10;


    [SerializeField] float maxSteerAngle = 25;
    [SerializeField] float steerAngle = 0;
    [SerializeField] float maxLatFriction = 6;
    [SerializeField] float handbrakeLatFrictionMultiplier = 0.33f;


    [SerializeField] Transform wheelFrontLeft;
    [SerializeField] Transform wheelFrontRight;
    [SerializeField] Transform wheelBackLeft;
    [SerializeField] Transform wheelBackRight;

    [SerializeField] TrailRenderer[] tarmacTrails = new TrailRenderer[2];
    [SerializeField] TrailRenderer[] grassTrails = new TrailRenderer[4];

    public SplineContainer trackSpline;

        
    Vector2 rotateVec2(Vector2 v, float a) // angle in degrees
    {
        return Quaternion.AngleAxis(a, Vector3.forward) * v;
    }
    Vector2 rot90(Vector2 v) // rotates 90 degrees clockwise
    {
        return new Vector2(v.y, -v.x);
    }


    public void ResetCar()
    {
        foreach (TrailRenderer t in tarmacTrails)
            if (t != null)
                t.emitting = false;
        foreach (TrailRenderer t in grassTrails)
            if (t != null)
                t.emitting = false;

        transform.position = new(0, 0, transform.position.z);
        bodyDir = Vector2.up;
        velocity = Vector2.zero;
        GetComponent<Timing>().ResetTimer();
    }

    private void Start()
    {
        wheelFrontLeft = transform.Find("wheelFrontLeft");
        wheelFrontRight = transform.Find("wheelFrontRight");
        wheelBackLeft = transform.Find("wheelBackLeft");
        wheelBackRight = transform.Find("wheelBackRight");


        tarmacTrails[0] = wheelBackRight.transform.Find("tarmacTrail").GetComponent<TrailRenderer>();
        tarmacTrails[1] = wheelBackLeft.transform.Find("tarmacTrail").GetComponent<TrailRenderer>();

        grassTrails[0] = wheelBackRight.transform.Find("grassTrail").GetComponent<TrailRenderer>();
        grassTrails[1] = wheelBackLeft.transform.Find("grassTrail").GetComponent<TrailRenderer>();
        grassTrails[2] = wheelFrontLeft.transform.Find("grassTrail").GetComponent<TrailRenderer>();
        grassTrails[3] = wheelFrontLeft.transform.Find("grassTrail").GetComponent<TrailRenderer>();
    }



    bool isOnTrack(Vector2 p)
    {
        // get distance from car to the nearest point on the spline. if this is greater than the track radius then the car is off the track
        
        float3 pos = new(p.x, p.y, trackSpline.transform.position.z);
        float3 nearestPoint = new(); // the point on the spline closest to the car
        float t;
        SplineUtility.GetNearestPoint(trackSpline.Spline, pos, out nearestPoint, out t);

        float trackRadius = trackSpline.gameObject.GetComponent<SplineExtrude>().Radius;

        return math.distance(new float2(pos.x, pos.y), new float2(nearestPoint.x, nearestPoint.y)) < trackRadius;
    }

    void Update()
    {
        bool carOnTrack = isOnTrack(transform.position);



        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");


        ////// longitudinal (forward/back) forces

        engineForce = maxEnginePower * enginePower.Evaluate(velocity.magnitude / maxSpeed) * Mathf.Max(0, accelBrakeInput);
        Vector2 tractiveForce = engineForce * bodyDir;


        Vector2 brakingForce = -maxBrakingForce * Mathf.Max(0, -accelBrakeInput) * velocity.normalized; -
        

        float friction = maxLongFriction;
        friction *= 1 - Mathf.Abs(accelBrakeInput); // don't include friction if we're accelerating or braking. makes it easier to change driving behaviour since you only need to change one variable

        friction += carOnTrack ? 0 : grassLongFriction;
        -
        Vector2 frictionForce = friction * velocity;



        Vector2 longForce = tractiveForce + brakingForce - frictionForce;
        acceleration = longForce;
        velocity += acceleration * Time.deltaTime;


        ////// lateral forces

        steerAngle = -steerInput * maxSteerAngle;

        bool handbrake = Input.GetKey(KeyCode.Space);

        // taking the log of the speed makes steering more effective at low speeds and less at higher speeds 
        float angularVelocity = 2 * steerAngle * (float)Math.Log(velocity.magnitude + 1); 
        angularVelocity *= handbrake ? 1.5f : 0;
        bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);


        float latFriction = maxLatFriction * (carOnTrack ? 1 : 0.3f) * (handbrake ? handbrakeLatFrictionMultiplier : 1);

        // move velocity direction towards body direction depending on how much lat friction there is at this moment
        velocity = Vector2.Lerp(velocity.normalized, bodyDir, latFriction * Time.deltaTime) * velocity.magnitude;




        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);









        ////// update visual rotation of the car and front wheels

        transform.SetLocalPositionAndRotation(transform.position, Quaternion.FromToRotation(Vector3.up, Vec2to3(bodyDir)));
        //Debug.DrawRay(transform.position, bodyDir * 2, Color.yellow);

        Transform outsideWheel = steerAngle >= 0 ? wheelFrontLeft : wheelFrontRight;
        Transform insideWheel = steerAngle < 0 ? wheelFrontLeft : wheelFrontRight;

        // increase the angle of the wheel on the inside of the curve, since it has a smaller circle path to follow so needs to be tighter (ackerman steering)
        float visualWheelBase = Mathf.Abs(wheelFrontLeft.localPosition.y - wheelBackLeft.localPosition.y);
        float visualAxleWidth = Mathf.Abs(wheelFrontLeft.localPosition.x - wheelFrontRight.localPosition.x);

        float turnRadius = Math.Abs(visualWheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad));

        float insideSteerAngle = Mathf.Sign(steerAngle) * Mathf.Atan2(visualWheelBase, turnRadius + visualAxleWidth / 2) * Mathf.Rad2Deg;
        insideWheel.SetLocalPositionAndRotation(insideWheel.localPosition, Quaternion.Euler(0, 0, insideSteerAngle));

        float outsideSteerAngle = Mathf.Sign(steerAngle) * Mathf.Atan2(visualWheelBase, turnRadius - visualAxleWidth / 2) * Mathf.Rad2Deg;
        outsideWheel.SetLocalPositionAndRotation(outsideWheel.localPosition, Quaternion.Euler(0, 0, outsideSteerAngle));


        //// rays to show ackerman steering is satisfied
        //Debug.DrawRay(outsideWheel.transform.position, -Mathf.Sign(steerAngle) * outsideWheel.transform.right.normalized * 10, Color.blue);
        //Debug.DrawRay(insideWheel.transform.position, -Mathf.Sign(steerAngle) * insideWheel.transform.right.normalized * 10, Color.blue);
        //Debug.DrawRay(0.5f * (wheelBackRight.transform.position + wheelBackLeft.transform.position), -Mathf.Sign(steerAngle) * wheelBackRight.transform.right.normalized * turnRadius, Color.blue);




        //// tyre marks

        float driftAngle = Mathf.Acos(Vector2.Dot(bodyDir.normalized, velocity.normalized)) * Mathf.Rad2Deg;

        foreach (TrailRenderer tr in tarmacTrails)
            tr.emitting = driftAngle > 20 && isOnTrack(tr.transform.position);
        foreach (TrailRenderer tr in grassTrails)
            tr.emitting = !isOnTrack(tr.transform.position);




        // reset car if user presses R
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCar();
            transform.position = new(0, -3, transform.position.z);
            velocity = new(0, maxSpeed * 0.7f);
        }

    }


    private void DebugDrawPoint(Vector3 p, float diameter = 0.5f)
    {
        Debug.DrawRay(new(p.x - diameter, p.y, p.z), 2 * diameter * Vector3.right, Color.magenta);
        Debug.DrawRay(new(p.x, p.y - diameter, p.z), 2 * diameter * Vector3.up, Color.magenta);
    }
    private void DebugDrawPoint(float3 p)
    {
        DebugDrawPoint(new Vector3(p.x, p.y, p.z));
    }

}
