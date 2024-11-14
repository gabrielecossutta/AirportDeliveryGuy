# AirportDeliveryGuy
In AirportDeliveryGuy, players step into the role of an airport worker with the task of moving passengers and luggage to their respective planes. With unique power-ups, various vehicles, this game captures the fast-paced world of airport logistics.

<sub>This game was created for 6 days game Jam theme Travel</sub>

![menu](./GIF/menu.gif)

## Goal
Help passengers reach their flights and ensure safe delivery of luggage on the respective plane. Deliver passengers and baggage to maximize your score and receive money to purchase [**Power-Ups**](#Power-Ups) and [**Vehicles**](#Vehicles).

## Challenges 
Navigate through the airport, and avoid obstacles like busses and planes while delivering.

![busEnemy](./GIF/busEnemy.gif)![airplane](./GIF/airplane.gif)

## Interactions
Guide passengers using vehicles and power-ups to speed up transport.

![InteractionPassengers](./GIF/InteractionPassengers.gif)

Load luggage onto planes using vehicles or throw them directly onto conveyor belts.

![throw](./GIF/throw.gif)

# Power-Ups
### Speed Boost:
Increases movement speed, allowing for faster transport of passengers or luggage.

![speed](./GIF/speed.gif)
### Strength:
Increases carrying capacity, allowing the player to transport multiple luggage or passengers at once.

![strength](./GIF/strength.gif)

### Magnetism:
Attracts nearby luggage, making it easier to collect items without direct contact.

![magnetic](./GIF/magnetic.gif)

# Vehicles
### Cart:
Basic vehicle with low speed, suitable for short distances.

![cart](./GIF/cart.gif)

### Kart:
The Fastest vehicle, ideal for agile and large movements of luggage.

![Kart](./GIF/kart.gif)

### Bus:
High capacity, best for transporting large numbers of passengers over longer distances.

![bus](./GIF/bus.gif)

##

Some Code
### Baggage.cs
``` cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baggage : BaseInteractableObj
{
    private Rigidbody _rb;
    private Point currentPoint;
    public static event Action<Point> OnObjectRemoved;
    [SerializeField] GameObject vfxTrail;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }
    protected override void InteractNoParam()
    {
        NotifyObjectRemoved();
        _rb.constraints = RigidbodyConstraints.FreezeAll;
        DisableTrail();
        GetComponent<EntityProp>().ResetMoveAction();
    }

    public void NotifyObjectRemoved()
    {
        if (currentPoint != null)
        {
            currentPoint.isOccupied = false;
            OnObjectRemoved?.Invoke(currentPoint);
            currentPoint = null;
        }
    }

    public override void ThrowAway(Vector3 force)
    {
        base.ThrowAway(force);

        if (vfxTrail)
        {
            vfxTrail.GetComponentInChildren<TrailRenderer>().emitting = true;
            Invoke("DisableTrail", 1f);
        }
    }
    private void DisableTrail()
    {
        if (vfxTrail)
        {
            vfxTrail.GetComponentInChildren<TrailRenderer>().emitting = false;
        }
    }
}

```

### RunnerBaggage.cs
```cs
using System;
using UnityEngine;

public class RunnerBehaviour : MonoBehaviour
{
    [SerializeField] float speed;
    private Transform targetPoint;
    private Rigidbody _rb;
    int lastIndex = -1;
    [SerializeField] GameObject OwnerRef;
    private Action TransitioAnim;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        GameManager.OnLoseRound += OnLoseRound;
    }
    private void OnDestroy()
    {
        GameManager.OnLoseRound -= OnLoseRound;
    }
    private void OnLoseRound()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public void Init(GameObject Owner,ColorType colorType)
    {
        SetRandomPoint();
        OwnerRef = Owner;
        Color color = Color.white;
        switch (colorType)
        {
            case ColorType.Blue:
                ColorUtility.TryParseHtmlString("#1561FF", out color);
                break;
            case ColorType.Red:
                ColorUtility.TryParseHtmlString("#FF1C15", out color);
                break;
            case ColorType.Green:
                ColorUtility.TryParseHtmlString("#55FF2A", out color);
                break;
            default:
                break;
        }
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().materials[1].color = color;
        TransitioAnim = TransitionAnimationOnEnable;
        gameObject.layer = 0;
    }
    private void SetRandomPoint()
    {
        if (lastIndex >= 0)
        {
            int currentIndex = lastIndex;
            while (currentIndex == lastIndex)
            {
                currentIndex = UnityEngine.Random.Range(0, RunnerPath.GetPathCount());
            }
            lastIndex = currentIndex;
        }
        else
        {
            lastIndex = UnityEngine.Random.Range(0, RunnerPath.GetPathCount());
        }
        targetPoint = RunnerPath.GetPathAtIndex(lastIndex);
    }

    private void Update()
    {
        TransitioAnim?.Invoke();
        if (targetPoint != null)
        {
            Vector3 dir = targetPoint.position - transform.position;
            dir.y = 0;
            dir.Normalize();

            dir = AvoidObstacles(dir);

            _rb.AddForce(dir * speed * Time.deltaTime, ForceMode.Force);
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation , targetRot, 5 * Time.deltaTime);
            float dist = Mathf.Abs((targetPoint.position - transform.position).magnitude);
            if (dist < 5)
            {
                SetRandomPoint();
            }
        }
    }

    private Vector3 AvoidObstacles(Vector3 direction)
    {
        RaycastHit hit;
        float sphereRadius = 2f;
        float distance = 1f;

        if (Physics.SphereCast(transform.position, sphereRadius, direction, out hit, distance))
        {
            if (hit.collider != null)
            {
                return Vector3.Cross(hit.normal, Vector3.up).normalized;
            }
        }
        return direction;
    }
    
    public Transform InteractWithRunenrBaggage()
    {
        OwnerRef.SetActive(true);
        return OwnerRef.transform;
    }

    float timer = .1f;
    private void TransitionAnimationOnEnable()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            gameObject.layer = 0;
            timer = .1f;
            transform.localScale = Vector3.one;
            gameObject.layer = LayerMask.NameToLayer("Interactable");
            TransitioAnim = null;
            return;
        }
        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, timer / .1f);
    }
}
```

### Throw.cs
``` cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shooter : MonoBehaviour
{
    TrajectoryPredictor trajcetory;
    Inventory _inventory;
    [SerializeField] PlayerView view;
    [SerializeField] float throwForce = 10f;
    [SerializeField] float minForce = 2f;
    [SerializeField] float upForceMultiplier = .2f;
    [SerializeField] float forceIncrementMultiplier = 2f;
    [SerializeField] AudioClip sound;
    float currentForce;
    private bool canShoot;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        trajcetory = GetComponent<TrajectoryPredictor>();
    }



    // Update is called once per frame
    void Update()
    {
        if (!_inventory.IsEmpty)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                trajcetory.SetTrajectoryVisible(true);
                currentForce = minForce;
                canShoot = true;
            }
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Prediction();
            }
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                Throw();
            }
        }
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            trajcetory.SetTrajectoryVisible(false);
            canShoot = false;
        }
    }

    private void Prediction()
    {
        currentForce += Time.deltaTime * forceIncrementMultiplier;
        currentForce = Mathf.Clamp(currentForce, 0, throwForce);
        Rigidbody rb = _inventory.GetLastItem.GetComponent<Rigidbody>();
        ProjectileProperties property = new ProjectileProperties();
        property.Drag = rb.drag;
        property.Mass = rb.mass;

        Vector3 dir = view == PlayerView.FirstPerson
            ? Camera.main.transform.forward + transform.up * upForceMultiplier
            : transform.forward + transform.up * upForceMultiplier;

        property.Direction = dir;
        property.InitialPosition = _inventory.GetLastItem.transform.position;
        property.InitialSpeed = currentForce;
        trajcetory.PredictTrajectory(property);

        if (view == PlayerView.Iso)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit result, 100);
            Vector3 dist = result.point - transform.position;
            dist.Normalize();

            var rot = Quaternion.LookRotation(dist, Vector3.up);
            rot.eulerAngles = Vector3.up * rot.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5 * Time.deltaTime);
        }
    }
    private void Throw()
    {
        if (!canShoot) return;
        IInteract _interface = _inventory.GetLastItem.GetComponent<IInteract>();

        Vector3 dir = view == PlayerView.FirstPerson
            ? Camera.main.transform.forward + transform.up * upForceMultiplier
            : transform.forward + transform.up * upForceMultiplier;

        _inventory.HandleOnLostLastItem();
        _inventory.GetLastItem.GetComponent<BaseInteractableObj>().ThrowAway(dir * currentForce);
        _inventory.RemoveLastItem();
        trajcetory.SetTrajectoryVisible(false);
        AudioManager.PlaySound2d(sound);
    }
}
```

### Kart.cs
```c#
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;



public class MulinoController : MonoBehaviour
{
    [Header("Movemnet")]
    [SerializeField] float _speed = 5f;
    [SerializeField] float _turnSpeed = 2f;
    private float currentSpeed;

    [Header("Jump")]
    [SerializeField] float gravity = 20f;

    Rigidbody _rb;
    [SerializeField] bool isGrounded;
    [SerializeField] Transform groundChecker;
    [SerializeField] Transform playerDismountPos;

    private Vector3 _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        currentSpeed = _speed;

        PlayerStats.OnChangeStats += (float inSpeedMultiplier, float InForce, bool InMagnetism) =>
        {
            currentSpeed = _speed * inSpeedMultiplier;
        };
    }

    private void Update()
    {
        GatherInput();
        Look();
        isGrounded = Physics.Raycast(groundChecker.position, -transform.up, 1f);
        if (!isGrounded)
        {
            _rb.velocity -= new Vector3(0, gravity, 0) * Time.deltaTime;
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            UnrealPlayerController controller = LibraryFunction.GetUnrealPlayerController();
            controller.EnableInput();
            controller.SetTransfrom(playerDismountPos);
            controller.GetComponent<PlayerAnimation>().NotifyOnCar(false);


            gameObject.layer = LayerMask.NameToLayer("Interactable");
            enabled = false;
        }

    }
    private void FixedUpdate()
    {
        Move();
    }

    private void GatherInput()
    {
        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
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
            if (GetComponent<Inventory>())
            {
                GetComponent<Inventory>().ThrowAwayAllItems();
            }
        }
    }

    #region Movement
    private void Look()
    {
        if (_input.z == 0) return;

        transform.Rotate(0, _input.x * _turnSpeed * Time.deltaTime, 0, Space.Self);
    }

    private void Move()
    {
        if (!isGrounded) { return; }

        _rb.AddForce(transform.forward * currentSpeed * _input.z, ForceMode.Acceleration);
    }

    #endregion
}
```
