using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllersScript : MonoBehaviour
{
    public SteamVR_Action_Boolean m_GrabAction = null;

    private SteamVR_Behaviour_Pose m_Pose = null;

    private FixedJoint m_Joint = null;

    private Interactable m_CurrentInteractable = null;
    public List<Interactable> m_ContactInteractables = new List<Interactable>();

    private void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();

    }

    // Update is called once per frame
    private void Update()
    {
        //  Down
        if (m_GrabAction.GetStateDown(m_Pose.inputSource))
        {
            print(m_Pose.inputSource + "Trigger Down");
            PickUp();
        }
        // Up
        if (m_GrabAction.GetStateUp(m_Pose.inputSource))
        {
            print(m_Pose.inputSource + "Trigger Up");
            Drop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.gameObject.CompareTag("Interactable"))
        {
            return;
        }

        m_ContactInteractables.Add(other.gameObject.GetComponent<Interactable>());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Interactable"))
        {
            return;
        }

        m_ContactInteractables.Remove(other.gameObject.GetComponent<Interactable>());
    }

    private void PickUp()
    {
        // Get nearest interactable object
        m_CurrentInteractable = GetNearestInteractable();

        // Null check
        if (!m_CurrentInteractable)
        {
            return;
        }

        // Already held
        if (m_CurrentInteractable.m_ActiveHand)
        {
            m_CurrentInteractable.m_ActiveHand.Drop();
        }

        // Move object to controller
        //m_CurrentInteractable.transform.position = transform.position;

        // Attach rigidbody of object to FixedJoint of hand
        Rigidbody targetBody = m_CurrentInteractable.GetComponent<Rigidbody>();
        m_Joint.connectedBody = targetBody;

        // Set active hand
        m_CurrentInteractable.m_ActiveHand = this;

    }

    private void Drop()
    {
        // Null check
        if (!m_CurrentInteractable)
        {
            return;
        }

        // Detach
        m_Joint.connectedBody = null;

        // Clear
        m_CurrentInteractable.m_ActiveHand = null;
        m_CurrentInteractable = null;
    }

    private Interactable GetNearestInteractable()
    {
        Interactable nearest = null;
        float minDistrance = float.MaxValue;
        float distance = 0;

        foreach (Interactable interactable in m_ContactInteractables)
        {
            distance = (interactable.transform.position - transform.position).sqrMagnitude;

            if (distance < minDistrance)
            {
                minDistrance = distance;
                nearest = interactable;
            }
        }
        return nearest;
    }
}
