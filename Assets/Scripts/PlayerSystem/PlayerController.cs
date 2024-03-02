using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

[RequireComponent(typeof(CharacterController), typeof(WeaponController))]
public class PlayerController : HealthComponent
{
    private CharacterController _controller;
    private WeaponController _weaponController;
    private Spawner _spawner;
    private PlayerControls _input;

    public event Action OnStartShoot;
    public event Action OnStopShoot;
    //public event Action OnTeleport;

    [SerializeField] private PlayerSO _playerSO;
    [SerializeField] private float _playerSpeed = 6.0f;

    private Transform _actualPos => transform; // takie nazewnictwo raczej dodaje poziom zamieszania zamiast go zmniejszac, transform to transform, pozycja to tylko jedna ze zmiennych transforma
    private bool _isTeleporting = false;
    private Vector3 _aimPos;
    private Vector3 _move;
    private Vector3 _direction;

    private InputAction _moveAction;
    private InputAction _shootAction;
    private InputAction _reloadAction;
    private InputAction _teleportAction;

    [Inject]
    public void Construct(Spawner spawner)
    {
        _spawner = spawner;
        Debug.Log("spawner installed");
    }

    private void Awake()
    {
        GetReferences();
    }

    private void OnEnable()
    {
        _input.Enable();
        //nie ma potrzeby robic subskrybcji przez lambda expression, wystarczy:
        _reloadAction.performed += Reload;
        _teleportAction.performed += ctx => Teleport(ctx);
    }

    private void OnDisable()
    {
        _input.Disable(); 
        //odsubkrybowanie przez labda expresdion nie zadzaiała w ten sposób, ale jezeli przerobisz to tak jak wyzej zasugerowałem to bedzie git
        _reloadAction.canceled -= ctx => Reload(ctx);
        _teleportAction.canceled += ctx => Teleport(ctx); //masz plus zamiast minusa
    }

    //do wywalenia
    protected override void Start() 
    {
        base.Start();
    }

    void Update()
    {
        CalcMovePlayer(); //raczej nie powinno sie uzywac skrótów, chyba ze są całkowitym standardem
        CalcRotatePlayer(_weaponController._currentWeapon);
        UseWeapon(_weaponController._currentWeapon);
    }

    private void FixedUpdate()
    {
        if(!_isTeleporting)//klamry
        ExecuteMovement();
    }

    private void ExecuteMovement()
    {
        _controller.Move(_move * Time.deltaTime * _playerSpeed); //turbo pierdoła ale, najpier powinno sie mnozyc floaty i na koncu przez wektor ze wzgledu na wydajnosć, czyli np Time.delta time * _player speed wrzucic w nawias

        if (_direction != Vector3.zero)
        {
            transform.forward = new Vector3(_direction.x, 0, _direction.z);
        }
    }

    private void CalcRotatePlayer(WeaponBase weapon)
    {
        _aimPos = _playerSO.aimPos;
        if (weapon == null)
        {
            _aimPos.y = 0;
        }
        else //klamry
            _aimPos.y = (weapon is FireArms) ? ((FireArms)weapon)._shootPoint.position.y : ((MeleWeapon)weapon)._shootPoint.position.y;

        _direction = (_aimPos - _actualPos.position).normalized;
    }

    private void CalcMovePlayer()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        _move = new Vector3(input.x, 0f, input.y).normalized;
    }

    private void UseWeapon(WeaponBase weapon)
    {
        //ta część metody to Aim() lub HandleAiming() i ono rzeczywiscie powinno się odbywac co klatkę
        if (weapon == null)
            return;

        if (((new Vector2(_aimPos.x, _aimPos.z) - new Vector2(weapon.transform.position.x, weapon.transform.position.z)).magnitude) > 1)
        {
            weapon.AimWeapon(_aimPos);
        }

        // a ta konstrukcja nie wyglada fajnie, i:
        //1) raczej nie ma potrzeby zeby to robic co klatka
        //2) OnStopShoot bedzie zasadniczo wywolywane co klatke, a nazwa sugeruje ze stanie sie to jedynie kiedy strzelanie zostało zakończone
        //3) mogbys sie zasubskrybować gdzies w inicjalizacji do _shotAction.performed oraz _shotAction.canceled i na podstawie tych eventów wysyłć OnStarShoot oraz OnStopShoot poprzez 2 nowe metody np. "NotifyStartShoot() i NotifyStopShot() ktore robią invoke na tyc heventach
        if (_shootAction.IsPressed())
        {
            OnStartShoot?.Invoke();
        }
        else
        {
            OnStopShoot?.Invoke();
        }
    }

    private void GetReferences()
    {
        _input = new PlayerControls();
        _controller = GetComponent<CharacterController>();
        _weaponController = GetComponent<WeaponController>();
        // raczej do serializownaych pol zamaist get componentami, ale jakbys sie trzymał tego w całym projekcie zawsze to ewentualnie moze zostac

        _moveAction = _input.Player.Move;
        _shootAction = _input.Player.Shoot;
        _reloadAction = _input.Player.Reload;
        _teleportAction = _input.Player.Teleport;
        
        //straszny burdel sie Ci tutaj zrobił, OnStartShoot i OnStopShoot to publiczne eventy i to do nich inne klasy powiinny sie subskrybowac z zewnątrz, bo jeżeli tak ma to wygldać to nie ma to żadnego sensu, ponieważ podobną logikę możesz zrobic bez nich, i po prostu od razu odpalić finalne  metdy bez tego 'posrednika' w postaci eventów
        OnStartShoot += () => _weaponController.OnTriggerHold(_weaponController._currentWeapon); //skoro w parametrz jest przekazywane cos co juz jest w weaponControllerze t oczy w ogole jest sens zeby ten parametr isnitał? weapon contaroller sam sobie moze to wziac
        OnStopShoot += () => _weaponController.OnTriggerRelease(_weaponController._currentWeapon);
    }

    private void Reload(InputAction.CallbackContext context)
    {
        _weaponController._currentWeapon.Reload();
    }

    private void Teleport(InputAction.CallbackContext context)
    {
        if (!_isTeleporting)
        {
            _isTeleporting = true;
            StartCoroutine(PerformTeleport());
        }
    }

    public void Respawn()
    {
        if (!_isTeleporting)
        {
            _isTeleporting = true;
            StartCoroutine(PerformTeleport());
        }
    }

    private IEnumerator PerformTeleport()
    {
        _controller.enabled = false;
        _spawner.ResetPlayerPosition();
        yield return null;

        _controller.enabled = true;
        _isTeleporting = false;
    }

    private void OnDestroy()
    {
        //nie zadziała poprawnie
        //https://stackoverflow.com/questions/183367/unsubscribe-anonymous-method-in-c-sharp
        OnStartShoot -= () => _weaponController.OnTriggerHold(_weaponController._currentWeapon);
        OnStopShoot -= () => _weaponController.OnTriggerRelease(_weaponController._currentWeapon);
        //OnTeleport -= () => _spawner.ResetPlayerPosition();
    }
}
