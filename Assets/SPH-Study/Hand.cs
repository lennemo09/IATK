using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Hand : MonoBehaviour
{

    public SteamVR_Action_Boolean grabAction;
    private SteamVR_Behaviour_Pose pose;
    private FixedJoint joint;

    private Interactable currentInteractable;
    public List<Interactable> contactInteractables = new List<Interactable>();

    void Awake()
    {
        pose = GetComponent<SteamVR_Behaviour_Pose>();
        joint = GetComponent<FixedJoint>();
    }

    // Update is called once per frame
    void Update()
    {
        // Trigger down
        if (grabAction.GetStateDown(pose.inputSource))
        {
            //print("Trigger down");
            Pickup();
        }

        // Trigger release
        if (grabAction.GetStateUp(pose.inputSource))
        {
            //print("Trigger up");
            Drop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Interactable")) return; // If not Interactable, ignore.
        contactInteractables.Add(other.gameObject.GetComponent<Interactable>());
        //print("Hand in collider");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Interactable")) return; // If not Interactable, ignore.
        contactInteractables.Remove(other.gameObject.GetComponent<Interactable>());
        //print("Hand left collider");
    }

    public void Pickup()
    {
        // Get nearest
        currentInteractable = GetNearestInteractable();

        // Check if null
        if (!currentInteractable) return;

        // Check if already held - drop from active hand.
        if (currentInteractable.activeHand) currentInteractable.activeHand.Drop();

        // Attach
        Rigidbody targetBody = currentInteractable.GetComponent<Rigidbody>();
        joint.connectedBody = targetBody;

        // Set active hand
        currentInteractable.activeHand = this;
    }

    public void Drop()
    {
        // Check if null
        if (!currentInteractable) return;

        // Detach
        joint.connectedBody = null;

        // Clear active hand
        currentInteractable.activeHand = null;
    } 

    private Interactable GetNearestInteractable()
    {
        Interactable nearest = null;
        float minDistance = float.MaxValue;
        float distance = 0f;

        foreach (Interactable interactable in contactInteractables)
        {
            distance = (interactable.transform.position - transform.position).sqrMagnitude;
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = interactable;
            }
        }

        return nearest;
    }
}
