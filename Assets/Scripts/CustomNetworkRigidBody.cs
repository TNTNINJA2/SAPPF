using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


public class CustomNetworkRigidbody : NetworkRigidbody2D
{
    private Rigidbody m_Rigidbody;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnGainedOwnership()
    {
        // Let the NetworkRigidbody update the kinematic state first since this is
        // what determines whether the Rigidbody is kinematic or not
        base.OnGainedOwnership();

        // Added a check here in case you are changing ownership or ownership changes
        // when parented.
        if (transform.parent != null)
        {
            var parentNetworkObject = transform.parent.GetComponent<NetworkObject>();

            // You might need to add an additional check for the type of parent your
            // object is parented under, but this is the general idea
            if (parentNetworkObject != null)
            {
            }
        }
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if (IsOwner)
        {
            // You might need to add an additional check for the type of parent your
            // object is parented under, but this is the general idea
            if (parentNetworkObject != null)
            {
                m_Rigidbody.isKinematic = true;
            }
            else
            {
                m_Rigidbody.isKinematic = false;
            }
        }
        base.OnNetworkObjectParentChanged(parentNetworkObject);
    }
}
