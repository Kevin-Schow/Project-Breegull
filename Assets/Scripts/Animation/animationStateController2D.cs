using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationStateController2D : MonoBehaviour
{
    Animator animator;
    float velocityZ = 0.0f;
    float velocityX = 0.0f;
    public float acceleration = 5.0f;
    public float deceleration = 5.0f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;
    public float currentMaximumVelocity = 2.0f;
    public bool runToggle = true;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Get key input from player
        bool forwardPressed = Input.GetKey("w");
        bool leftPressed = Input.GetKey("a");
        bool rightPressed = Input.GetKey("d");
        bool downPressed = Input.GetKey("s");
        bool strafeLeftPressed = Input.GetKey("q");
        bool strafeRightPressed = Input.GetKey("e");
        bool runPressed = Input.GetKey("r");

        // Swap runToggle
        runToggle = runPressed ? false : true;

        // Set currentMaximumVelocity
        if (runToggle)
        {
            currentMaximumVelocity = maximumWalkVelocity;
        }
        if (!runToggle)
        {
            currentMaximumVelocity = maximumRunVelocity;
        }
        
        // Run speed toggle - R
        currentMaximumVelocity = runToggle ? maximumRunVelocity : maximumWalkVelocity;

        // Forward walk in any direction - WASD
        if ((forwardPressed || leftPressed || rightPressed || downPressed) && velocityZ < currentMaximumVelocity)
        {
            velocityZ += Time.deltaTime * acceleration;
        }

        //Strafe Left - Q 
        if (strafeLeftPressed && velocityX < currentMaximumVelocity)
        {
            velocityX -= Time.deltaTime * acceleration;
        }

        //Strafe Right - E
        if (strafeRightPressed && velocityX > -currentMaximumVelocity)
        {
            velocityX += Time.deltaTime * acceleration;
        }

        // Decrease VelocityZ
        if (!(forwardPressed || leftPressed || rightPressed || downPressed) && velocityZ > 0.0f)
        {
            velocityZ -= Time.deltaTime * deceleration;
        }

        // Reset VelocityZ
        if (!(forwardPressed || leftPressed || rightPressed || downPressed) && velocityZ < 0.0f)
        {
            velocityZ = 0;
        }

        // Maximum VelocityZ
        if (velocityZ > currentMaximumVelocity)
        {
            velocityZ = currentMaximumVelocity;
        }

        // Maximum VelocityX
        if (velocityX > currentMaximumVelocity)
        {
            velocityX = currentMaximumVelocity;
        }
        // Minimum VelocityX
        if (velocityX < -currentMaximumVelocity)
        {
            velocityX = -currentMaximumVelocity;
        }

        //Reset VelocityX
        if (!strafeLeftPressed && !strafeRightPressed && velocityX != 0.0f && (velocityX > -0.05f && velocityX < 0.05f))
        {
            velocityX = 0.0f;
        }

        // lock forward
        if ((forwardPressed || leftPressed || rightPressed || downPressed) && runToggle && velocityZ > currentMaximumVelocity)
        {
            velocityZ = currentMaximumVelocity;
        }
        // decelerate to maximum walk velocity
        else if ((forwardPressed || leftPressed || rightPressed || downPressed) && velocityZ > currentMaximumVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            // round to currentMaxVelocity if within offset
            if (velocityZ > currentMaximumVelocity && velocityZ < (currentMaximumVelocity + 0.05f))
            {
                velocityZ = currentMaximumVelocity;
            }
        }
        else if ((forwardPressed || leftPressed || rightPressed || downPressed) && velocityZ < currentMaximumVelocity && velocityZ > (currentMaximumVelocity - 0.05f))
        {
            velocityZ = currentMaximumVelocity;
        }
        
        // Approach 0 with VelocityX
        if (!strafeLeftPressed && velocityX < 0.0f)
        {
            velocityX += Time.deltaTime * deceleration;
        }
        if (!strafeRightPressed && velocityX > 0.0f)
        {
            velocityX -= Time.deltaTime * deceleration;
        }

        // Set the parameters to our local variable values
        animator.SetFloat("Velocity Z", velocityZ);
        animator.SetFloat("Velocity X", velocityX);

    }
}
