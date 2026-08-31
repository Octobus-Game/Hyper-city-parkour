using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ТРЮКИ : MonoBehaviour
{
    private bool isSpinning = false;
    private float spinTime = 5.0f;
    private float spinDuration = 5.0f;
    private int keyPressCount = 0;
    private bool isSpinningActivated = false;

    void Update()
    {
        // Check for simultaneous pressing of A and Space
        if (Input.GetKeyDown(KeyCode.A) && Input.GetKeyDown(KeyCode.Space))
        {
            keyPressCount++;
            if (keyPressCount == 4)
            {
                isSpinningActivated = true;
                keyPressCount = 0; // Reset the counter
            }
        }

        // If spinning is activated and the character is not already spinning
        if (isSpinningActivated && !isSpinning)
        {
            isSpinning = true;
            isSpinningActivated = false;
        }

        // Perform the spinning
        if (isSpinning)
        {
            if (spinTime > 0)
            {
                transform.Rotate(0, 360 * Time.deltaTime, 0); // Rotate 360 degrees per second
                spinTime -= Time.deltaTime;
            }
            else
            {
                isSpinning = false;
                spinTime = spinDuration; // Reset the spin time
                transform.rotation = Quaternion.identity; // Return to the original position
            }
        }
    }
}
