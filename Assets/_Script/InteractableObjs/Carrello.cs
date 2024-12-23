using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Carrello : BaseInteractableObj
{
    private Rigidbody _rb;
    public Transform handlePosition;
    public float interactionDistance = 1.0f;
    private Inventory _inventory;
    public Vector3 fixedPlayerOffset = new Vector3(0, 0, -1);
    public Vector3 rotationOffset = new Vector3(0, 0, 0);
    public float magnetRadius = 5.0f;
    public float magnetSpeed = 10.0f;
    public bool magnetActive = false;
    public float attachDistance = 1.0f;
    private Transform _socketTransform;
    public bool isCarrelloInHand = false;

    [SerializeField] bool canBeHandle = true;
    public bool CanBeHandle => canBeHandle;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        _rb = GetComponent<Rigidbody>();
        _socketTransform = _inventory.socketRef;
    }


    private void OnLoseRound()
    {
        LibraryFunction.GetUnrealPlayerController().transform.parent = null;
        LibraryFunction.GetUnrealPlayerController().EnableInput();
        LibraryFunction.GetUnrealPlayerController().GetComponent<PlayerAnimation>().NotifyOnCart(false);
        Destroy(gameObject);
    }


    private void Start()
    {
        GameManager.OnLoseRound += OnLoseRound;
    }
    private void OnDestroy()
    {
        GameManager.OnLoseRound -= OnLoseRound;
    }
    private void Update()
    {
        if (isCarrelloInHand && magnetActive)
        {
            AttractNearbyValigie();
        }
    }

    [Header("Exolosion when getting hitted")]
    [SerializeField] float explosionForce = 200f;
    [SerializeField] float explosionRadius = 20f;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Autobus"))
        {
            _rb.AddExplosionForce(explosionForce, collision.transform.position, explosionRadius);
            _rb.freezeRotation = false;
            GetComponent<Inventory>().ThrowAwayAllItems();
        }
    }

    protected override void InteractOneParam(Transform obj)
    {
        _inventory.AddObjInInventory(obj);
    }

    public bool Handle(Transform player)
    {
        if (handlePosition == null)
        {
            return false;
        }

        if (IsPlayerNearHandle(player))
        {
            isCarrelloInHand = true;

            _rb.isKinematic = true;
            Vector3 newPosition = handlePosition.position + handlePosition.TransformDirection(fixedPlayerOffset);
            player.position = newPosition;
            Quaternion newRotation = handlePosition.rotation * Quaternion.Euler(rotationOffset);
            player.rotation = newRotation;
            transform.SetParent(player);
            return true;
        }
        return false;

    }

    public void Release(Transform player)
    {
        isCarrelloInHand = false;
        _rb.isKinematic = false;
        transform.SetParent(null);
    }


    private bool IsPlayerNearHandle(Transform player)
    {
        if (player != null)
        {
            float distance = Vector3.Distance(player.position, handlePosition.position);
            return distance <= interactionDistance;
        }
        return false;
    }

    private void AttractNearbyValigie()
    {
        if (_inventory.count >= _inventory.maxPickableObj)
        {
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
        foreach (Collider col in colliders)
        {
            IInteract interactable = col.GetComponent<IInteract>();
            if (interactable != null && interactable.GetInteractType() == InteractType.Valigia)
            {
                Transform valigiaTransform = col.transform;

                if (_inventory.IsAlreadyAdded(valigiaTransform))
                {
                    continue;
                }

                Rigidbody valigiaRb = valigiaTransform.GetComponent<Rigidbody>();
                if (valigiaRb != null)
                {
                    Vector3 direction = (_socketTransform.position - valigiaTransform.position).normalized;
                    float distToSocket = Vector3.Distance(valigiaTransform.position, _socketTransform.position);

                    if (distToSocket <= attachDistance)
                    {
                        AttachValigiaToSocket(valigiaTransform);
                    }
                    else
                    {
                        valigiaRb.velocity = direction * magnetSpeed;
                    }
                }
            }
        }
    }

    private void AttachValigiaToSocket(Transform valigia)
    {
        if (_inventory.count >= _inventory.maxPickableObj)
        {
            return;
        }
        _inventory.AddObjInInventory(valigia);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
