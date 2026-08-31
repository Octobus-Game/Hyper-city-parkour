using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class floatwater : MonoBehaviour
{
    public Rigidbody rigibody;
    public CharacterController characterController;

    public Rigidbody[] rigidbodies;
    public CharacterController[] characterControllers;

    public float depthBefore = 1f;
    public float displace = 3f;

    private void FixedUpdate()
    {
       
        if (rigibody != null && transform.position.y < 0)
        {
            float displacementMultiplier = Mathf.Clamp01(-transform.position.y / depthBefore) * displace;
            rigibody.AddForce(new Vector3(0, Mathf.Abs(Physics.gravity.y) * displace, 0), ForceMode.Acceleration);
        }

       
        if (rigidbodies != null && rigidbodies.Length > 0)
        {
            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null && rb.transform.position.y < 0)
                {
                    float displacementMultiplier = Mathf.Clamp01(-rb.transform.position.y / depthBefore) * displace;
                    rb.AddForce(new Vector3(0, Mathf.Abs(Physics.gravity.y) * displace, 0), ForceMode.Acceleration);
                }
            }
        }
    }

    private void Update()
    {
       
        if (characterController != null && characterController.transform.position.y < 0)
        {
            float displacementMultiplier = Mathf.Clamp01(-characterController.transform.position.y / depthBefore) * displace;
            Vector3 buoyancy = new Vector3(0, Mathf.Abs(Physics.gravity.y) * displace * Time.deltaTime, 0);
            characterController.Move(buoyancy);
        }

      
        if (characterControllers != null && characterControllers.Length > 0)
        {
            foreach (CharacterController cc in characterControllers)
            {
                if (cc != null && cc.transform.position.y < 0)
                {
                    float displacementMultiplier = Mathf.Clamp01(-cc.transform.position.y / depthBefore) * displace;
                    Vector3 buoyancy = new Vector3(0, Mathf.Abs(Physics.gravity.y) * displace * Time.deltaTime, 0);
                    cc.Move(buoyancy);
                }
            }
        }
    }
}