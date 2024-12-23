using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseInteractableObj : MonoBehaviour, IInteract
{
    [SerializeField] InteractType interactType;

    protected virtual void InteractNoParam() { }
    protected virtual void InteractOneParam(Transform obj) { }
    protected virtual void InteractTwoParam(Transform transform,bool bNig) { }

    void IInteract.Interact()
    {
        InteractNoParam();
    }

    void IInteract.Interact(Transform obj)
    {
        InteractOneParam(obj);
    }

    InteractType IInteract.GetInteractType()
    {
        return interactType;
    }


    public virtual void ThrowAway(Vector3 force)
    {
        Rigidbody _rb = GetComponent<Rigidbody>();
        transform.parent = null;
        _rb.constraints = RigidbodyConstraints.None;
        _rb.AddForce(force, ForceMode.Impulse);
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
    public void Interact(Transform baggage, bool bHasToStore)
    {
        InteractTwoParam(baggage, bHasToStore);
    }
}