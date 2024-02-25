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
    public float angularVelocity = 0;


    public float mass = 1000;
    public float engineForce = 0;


    private float accelBrakeInput = 0;
    private float steerInput = 0;



    public float maxSpeed = 15;
    public float maxEngineTorque = 6000;
    public AnimationCurve enginePower;


    public float maxLongFriction = 500;

    public float braking = 10000;


    float wheelBase = 2;
    public float maxSteerAngle = 25;


    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelBackLeft;
    public Transform wheelBackRight;

    public float steerAngle = 0;

    public AnimationCurve latForceBySlipAngle;
    public float maxLatFriction = 2000;

    public float rearSlipAngle = 0;
    public float rearLatForce = 0;
    
    public float frontSlipAngle = 0;
    public float frontLatForce = 0;


    Vector2 rotateVec2(Vector2 v, float a)
    {
        return Quaternion.AngleAxis(a, Vector3.forward) * v;
    }
    Vector2 rot90(Vector2 v) // rotates 90 degrees clockwise
    {
        return new Vector2(v.y, -v.x);
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

        wheelBase = Mathf.Abs(wheelFrontLeft.localPosition.y - wheelBackLeft.localPosition.y);
    }

    void Update()
    {

        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");


        ////// longitudinal (forward/back) forces

        engineForce = maxEngineTorque * enginePower.Evaluate(velocity.magnitude / maxSpeed) * Mathf.Max(0, accelBrakeInput);
        Vector2 tractiveForce =  engineForce * bodyDir;

        Vector2 brakingForce = -braking * Mathf.Max(0, -accelBrakeInput) * velocity.normalized; // TODO - braking should only apply to the component of velocity in the direction of the car's body


        Vector2 friction = -maxLongFriction * velocity; 
        friction *= 1 - Mathf.Abs(accelBrakeInput); // don't include friction if we're accelerating or braking. makes it easier to change driving behaviour since you only need to change one variable
        //friction *= 1 + 0.01f / Mathf.Max(velocity.magnitude, 0.001f); // when speed is low, apply lots of friction so the car stops succinctly // THIS DOESNT SEEM TO DO ANYTHING >:(

        Vector2 longForce = tractiveForce + brakingForce + friction;




        ////// lateral forces

        steerAngle = -steerInput * maxSteerAngle;

        //float angularVelocity = velocity.magnitude * Mathf.Sin(steerAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg / wheelBase;
        //bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);

        ////Vector2 frontWheelDir = rotateVec2(bodyDir, steerAngle).normalized;
        //velocity = velocity.magnitude * bodyDir.normalized;



        //float slipAngle = Mathf.Acos(Vector2.Dot(velocity, bodyDir) / (velocity.magnitude * bodyDir.magnitude));

        float vLong = Vector2.Dot(velocity, bodyDir); // component of velocity in the direction of the car body
        float vLat = Vector2.Dot(velocity, rot90(bodyDir)); // component of velocity in the direction perpendicular to the car body
        Debug.DrawRay(transform.position, vLong * bodyDir, Color.blue);
        Debug.DrawRay(transform.position, vLat * rot90(bodyDir), Color.blue);


        rearSlipAngle = Mathf.Atan2(vLat + angularVelocity * wheelBase / 2, Mathf.Abs(vLong));
        rearLatForce = latForceBySlipAngle.Evaluate(Mathf.Abs(rearSlipAngle)) * maxLatFriction * Mathf.Sign(rearSlipAngle);

        frontSlipAngle = Mathf.Atan2(vLat + angularVelocity * wheelBase / 2, Mathf.Abs(vLong)) - steerAngle * Mathf.Sign(vLong);
        frontLatForce = latForceBySlipAngle.Evaluate(Mathf.Abs(frontSlipAngle)) * maxLatFriction * Mathf.Sign(frontSlipAngle);


        Vector2 latForce = (rearLatForce + Mathf.Cos(steerAngle) * frontLatForce) * rot90(bodyDir);

        float torque = -rearLatForce * wheelBase / 2 + Mathf.Cos(steerAngle) * frontLatForce * wheelBase / 2;
        angularVelocity += torque * Time.deltaTime;
        bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);
            




        acceleration = (longForce + latForce) / mass;
        velocity += acceleration * Time.deltaTime;

        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);







        ////// update visual rotation of the car and front wheels

        transform.Rotate(Vector3.forward, angularVelocity * Time.deltaTime);
        Debug.DrawRay(transform.position, bodyDir * 2, Color.yellow);

        Transform outsideWheel = steerAngle >= 0 ? wheelFrontLeft : wheelFrontRight;
        Transform insideWheel = steerAngle < 0 ? wheelFrontLeft : wheelFrontRight;

        // increase the angle of the wheel on the inside of the curve, since it has a smaller circle path to follow so needs to be tighter
        float visualWheelBase = Mathf.Abs(wheelFrontLeft.localPosition.y - wheelBackLeft.localPosition.y);
        float visualAxleWidth = Mathf.Abs(wheelFrontLeft.localPosition.x - wheelFrontRight.localPosition.x);

        float turnRadius = Math.Abs(visualWheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad));

        float insideSteerAngle = Mathf.Sign(steerAngle) * Mathf.Atan2(visualWheelBase, turnRadius + visualAxleWidth / 2) * Mathf.Rad2Deg;
        insideWheel.SetLocalPositionAndRotation(insideWheel.localPosition, Quaternion.Euler(0, 0, insideSteerAngle));

        float outsideSteerAngle = Mathf.Sign(steerAngle) * Mathf.Atan2(visualWheelBase, turnRadius - visualAxleWidth / 2) * Mathf.Rad2Deg;
        outsideWheel.SetLocalPositionAndRotation(outsideWheel.localPosition, Quaternion.Euler(0, 0, outsideSteerAngle));



        //Debug.DrawRay(outsideWheel.transform.position, -Mathf.Sign(steerAngle) * outsideWheel.transform.right.normalized * 10, Color.blue);
        //Debug.DrawRay(insideWheel.transform.position, -Mathf.Sign(steerAngle) * insideWheel.transform.right.normalized * 10, Color.blue);
        //Debug.DrawRay(0.5f * (wheelBackRight.transform.position + wheelBackLeft.transform.position), -Mathf.Sign(steerAngle) * wheelBackRight.transform.right.normalized * turnRadius, Color.blue);


    }

}
