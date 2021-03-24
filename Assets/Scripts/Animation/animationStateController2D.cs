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

    // Increase Performance
    int VelocityZHash;
    int VelocityXHash;

    void Start()
    {
        animator = GetComponent<Animator>();

        VelocityZHash = Animator.StringToHash("Velocity Z");
        VelocityXHash = Animator.StringToHash("Velocity X");
    }

    // Handles Acceleration and Deceleration
    void changeVelocity(bool movePressed, bool strafeLeftPressed, bool strafeRightPressed, bool runToggle, float currentMaxVelocity)
    {
        // Forward walk in any direction - WASD
        if (movePressed && velocityZ < currentMaximumVelocity)
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
        if (!movePressed && velocityZ > 0.0f)
        {
            velocityZ -= Time.deltaTime * deceleration;
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
    }
    // Handles locking or reseting velocity
    void lockOrResetVelocity(bool movePressed, bool strafeLeftPressed, bool strafeRightPressed, bool runToggle, float currentMaxVelocity)
    {
        

        // Reset VelocityZ
        if (!movePressed && velocityZ < 0.0f)
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
        if (movePressed && velocityZ > currentMaximumVelocity)
        {
            velocityZ = currentMaximumVelocity;
        }
        // decelerate to maximum walk velocity
        else if (movePressed && velocityZ > currentMaximumVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            // round to currentMaxVelocity if within offset
            if (velocityZ > currentMaximumVelocity && velocityZ < (currentMaximumVelocity + 0.05f))
            {
                velocityZ = currentMaximumVelocity;
            }
        }
        else if (movePressed && velocityZ < currentMaximumVelocity && velocityZ > (currentMaximumVelocity - 0.05f))
        {
            velocityZ = currentMaximumVelocity;
        }

        // Set the parameters to our local variable values
        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
    }
    void Update()
    {
        // Get key input from player
        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool leftPressed = Input.GetKey(KeyCode.A);
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool downPressed = Input.GetKey(KeyCode.S);
        bool strafeLeftPressed = Input.GetKey(KeyCode.Q);
        bool strafeRightPressed = Input.GetKey(KeyCode.E);
        bool runPressed = Input.GetKey(KeyCode.R);

        // Moving if WASD pressed but not A and D or W and S
        bool movePressed = forwardPressed || leftPressed || rightPressed || downPressed;
        // TODO:  A or D, W or S

        // Swap runToggle
        // bool runToggle = runPressed ? false : true;
        
        // Set Current Max Velocity
        currentMaximumVelocity = runToggle ? maximumRunVelocity : maximumWalkVelocity;

        // Set currentMaximumVelocity
        if (runToggle && runPressed)
        {
            currentMaximumVelocity = maximumRunVelocity;
            runToggle = false;
        }
        if (!runToggle && runPressed)
        {
            currentMaximumVelocity = maximumWalkVelocity;
            runToggle = true;
        }

        // Handle changes in velocity
        changeVelocity(movePressed, strafeLeftPressed, strafeRightPressed, runToggle, currentMaximumVelocity);
        lockOrResetVelocity(movePressed, strafeLeftPressed, strafeRightPressed, runToggle, currentMaximumVelocity);

    }
}
