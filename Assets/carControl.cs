using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    //public Vector2 velocity { get; private set; } = Vector2.zero;
    public Vector2 velocity = Vector2.zero; // for debugging only
    private Vector2 acceleration = Vector2.zero;
    public Vector2 bodyDir = Vector2.up; // direction of the body of the car


    public float engineForce = 0;


    private float accelBrakeInput = 0;
    private float steerInput = 0;



    public float maxSpeed = 15;
    public float maxEngineTorque = 6000;
    public AnimationCurve enginePower;



    public float maxLongFriction = 500;


    public float braking = 10000;


    float wheelBase = 2;
    public float maxSteerAngle = 40;

    public const float mass = 1000;

    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelBackLeft;
    public Transform wheelBackRight;

    public float visualWheelBase  = 0;
    public float visualAxleWidth  = 0;
    public float insideSteerAngle = 0;
    public float steerAngle = 0;

    Vector2 rotateVec2(Vector2 v, float a)
    {
        return Quaternion.AngleAxis(a, Vector3.forward) * v;
    }


    private void Start()
    {

        Keyframe[] engineKeyFrames =
        {
            new Keyframe(0, 0.5f, 4, 4, 0, 0),
            new Keyframe(0.25f, 0.5f, 4, 4, 0, 0),
            new Keyframe(0, 0.5f, 4, 4, 0, 0),
        };


        wheelFrontLeft = transform.Find("wheelFrontLeft");
        wheelFrontRight = transform.Find("wheelFrontRight");
        wheelBackLeft = transform.Find("wheelBackLeft");
        wheelBackRight = transform.Find("wheelBackRight");
    }

    void Update()
    {

        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");

        steerAngle = -steerInput * maxSteerAngle;

        // longitudinal (forward/back) forces

        engineForce = maxEngineTorque * enginePower.Evaluate(velocity.magnitude / maxSpeed) * Mathf.Max(0, accelBrakeInput);
        Vector2 tractiveForce =  engineForce * bodyDir;

        Vector2 brakingForce = -braking * Mathf.Max(0, -accelBrakeInput) * velocity.normalized; // TODO - braking should only apply to the component of velocity in the direction of the car's body


        Vector2 friction = -maxLongFriction * velocity; 
        friction *= 1 - Mathf.Abs(accelBrakeInput); // don't include friction if we're accelerating or braking. makes it easier to change driving behaviour since you only need to change one variable
        //friction *= 1 + 0.01f / Mathf.Max(velocity.magnitude, 0.001f); // when speed is low, apply lots of friction so the car stops succinctly // THIS DOESNT SEEM TO DO ANYTHING >:(

        //float weightOnFrontWheels = 0.5f*mass*10 - 0.2*mass*() 

        Vector2 longForce = tractiveForce + brakingForce + friction;



        float angularVelocity = velocity.magnitude * Mathf.Sin(steerAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg / wheelBase;
        bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);

        Vector2 frontWheelDir = rotateVec2(bodyDir, steerAngle).normalized;
        velocity = velocity.magnitude * frontWheelDir;



        //float slipAngle = Mathf.Acos(Vector2.Dot(velocity, bodyDir) / (velocity.magnitude * bodyDir.magnitude));


        acceleration = longForce / mass;

        velocity += acceleration * Time.deltaTime;

        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);



        // update visual rotation of the car and front wheels
        transform.Rotate(Vector3.forward, angularVelocity * Time.deltaTime);

        Transform outsideWheel = steerAngle < 0 ? wheelFrontLeft : wheelFrontRight;
        Transform insideWheel = steerAngle >= 0 ? wheelFrontLeft : wheelFrontRight;
        outsideWheel.localScale = new Vector3(1, 2, 1);
        insideWheel.localScale = new Vector3(1, 1, 1);
        outsideWheel.SetLocalPositionAndRotation(outsideWheel.localPosition, Quaternion.Euler(0, 0, steerAngle));

        // increase the angle of the wheel on the inside of the curve, since it has a smaller circle path to follow so needs to be tighter
        visualWheelBase = Mathf.Abs(wheelFrontLeft.localPosition.y - wheelBackLeft.localPosition.y);
        visualAxleWidth = Mathf.Abs(wheelFrontLeft.localPosition.x - wheelFrontRight.localPosition.x);
        insideSteerAngle = Mathf.Atan2(visualWheelBase, visualWheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad) - visualAxleWidth) * Mathf.Rad2Deg;

        insideWheel.SetLocalPositionAndRotation(insideWheel.localPosition, Quaternion.Euler(0, 0, insideSteerAngle));

        Func<Vector2, Vector2> rot90 = v => new Vector3(v.y, -v.x);
        Debug.DrawRay(outsideWheel.transform.position, rot90(outsideWheel.transform.up).normalized * 10, Color.blue);
        Debug.DrawRay(insideWheel.transform.position, -rot90(insideWheel.transform.up).normalized * 10, Color.blue);
        Debug.DrawRay(wheelBackLeft.transform.position, rot90(wheelBackLeft.transform.up).normalized * 10, Color.blue);

    }

    void FixedUpdate() {


    }
}
