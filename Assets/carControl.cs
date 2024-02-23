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
    float maxSteerAngle = 50;

    public const float mass = 1000;

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

    }

    void Update()
    {

        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");

        float steerAngle = -steerInput * maxSteerAngle;

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
        velocity = rotateVec2(velocity, angularVelocity * Time.deltaTime);
        transform.Rotate(Vector3.forward, angularVelocity * Time.deltaTime);


        //float slipAngle = Mathf.Acos(Vector2.Dot(velocity, bodyDir) / (velocity.magnitude * bodyDir.magnitude));


        acceleration = longForce / mass;

        velocity += acceleration * Time.deltaTime;

        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);


    }

    void FixedUpdate() {


    }
}
