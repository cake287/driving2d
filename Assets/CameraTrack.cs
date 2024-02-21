using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTrack : MonoBehaviour
{
    private const float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero;

    [SerializeField] private CarControl targetCar;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // add car's velocity so the camera looks ahead of the car to give the player a clearer view
        Vector3 targetPos = targetCar.transform.position + 0.5f * new Vector3(targetCar.velocity.x, targetCar.velocity.y, 0); 
        
        targetPos += new Vector3(0, 0, -10);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);


        
    }
}
