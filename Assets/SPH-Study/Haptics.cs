using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Haptics : MonoBehaviour
{
    public HapticsDensityCube densityCube;
    public SteamVR_Action_Vibration hapticAction;
    public SteamVR_Action_Boolean gripAction;

    public GameObject leftController;
    public GameObject rightController;

    public SteamVR_Behaviour_Pose leftHandPose;
    public SteamVR_Behaviour_Pose rightHandPose;

    private bool rightGripPressed = false;
    private bool leftGripPressed = false;

    private void Start()
    {
        leftHandPose = leftController.GetComponent<SteamVR_Behaviour_Pose>();
        rightHandPose = rightController.GetComponent<SteamVR_Behaviour_Pose>();

        gripAction.AddOnStateDownListener(RightGripPressed, rightHandPose.inputSource);
        gripAction.AddOnStateDownListener(LeftGripPressed, leftHandPose.inputSource);

        gripAction.AddOnStateUpListener(RightGripRelease, rightHandPose.inputSource);
        gripAction.AddOnStateUpListener(LeftGripRelease, leftHandPose.inputSource);
    }

    void Update()
    {
        float amp;
        if (rightGripPressed)
        {
            amp = GetDensityInCube(rightController);
            Vibrate(.01f, 150, amp, SteamVR_Input_Sources.RightHand);
        }

        if (leftGripPressed)
        {
            amp = GetDensityInCube(leftController);
            Vibrate(.01f, 150, amp, SteamVR_Input_Sources.LeftHand);
        }
    }

    void RightGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        rightGripPressed = true;
        //print("Right Grip is pressed.");

    }

    void LeftGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        leftGripPressed = true;
        //print("Left Grip is pressed.");

    }

    void RightGripRelease(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        rightGripPressed = false;
        //print("Right Grip is released.");

    }

    void LeftGripRelease(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        leftGripPressed = false;
        //print("Left Grip is released.");

    }

    private void Vibrate(float duration, float frequency, float amplitude, SteamVR_Input_Sources source)
    {
        hapticAction.Execute(0, duration, frequency, amplitude, source);

        //print("Grip pressed, " + source.ToString());
    }

    private float GetDensityInCube(GameObject source)
    {
        float[,,] cube = densityCube.hapticsCube;
        int cubeLength = cube.GetLength(0);

        Vector3 cubeOrigin = new Vector3(-.5f, -.5f, -.5f);
        Vector3 handPosition = transform.InverseTransformPoint(source.transform.position);
        //Vector3 handPosition = source.transform.position;

        Vector3 handInCube = handPosition - cubeOrigin;

        if (handInCube.x > 0 && handInCube.x < 1 && 
            handInCube.y > 0 && handInCube.y < 1 && 
            handInCube.z > 0 && handInCube.z < 1)
        {
            int xBin = (int)(handInCube.x * cubeLength);
            int yBin = (int)(handInCube.y * cubeLength);
            int zBin = (int)(handInCube.z * cubeLength);
            float density = cube[xBin,yBin,zBin];
            print("Hand in cube at " + xBin.ToString() + "|" + yBin.ToString() + "|" + zBin.ToString() +  ", density: " + density.ToString());
            return density;
        }
        print(handPosition.ToString());
        return 0f;
    }
}
