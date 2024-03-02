using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof(NavMeshAgent))]
public class Enemy : HealthComponent
{
    public enum State { Idle, Chasing, Attacking};
    State _currentState;

    private NavMeshAgent _nav;
    private Transform _target;
    private Material _skinMaterial;
    private HealthComponent _targetHealth;
    private bool _hasTarget;

    [SerializeField] private float _attackDistanceThreshold = 0.5f;
    [SerializeField] private float _timeBetweenAttacks = 5f;
    private float _nextAttackTime; //zmienne modyfikowane w edytore ([SerializeField]), raczej powinny byc trzymana w jednej grupie
    [SerializeField] private Color _originalColor; //original color? jeżeli zamierzenie jeste takie że jest to kolor do któego wracamy po zmianie koloru, to sensowniej jest go nie ustawiać w edytorze, tylko w Start lub Awake, pobrać kolor z materiału, aczkolwiek z tego co widzę modyfikujesz ten kolor publiczna metodą, przy spawnowaniu, wiec pytanie czy to w ogole powinno byc [SerializeField]
    [SerializeField] private ParticleSystem _enemyHitEffect;
    [SerializeField] private int _damageRate = 1; //to chyba bardziej zwykle damage, a nie damageRate

    private float _enemyCollsionRadius;
    private float _targetCollsionRadius;

    private void Awake()
    {
        //raczej albo wszsytkie komponenty (NavMeshAgent, ParticleSystem) z serializowanych pól, albo wszsytko z GetComponent, aby było to spójne. (aczkolwiek w kazdej firmie w ktorej pracowałem standardem były serializowane pola)
        _nav = GetComponent<NavMeshAgent>();
    }
    //brakowało tutaj entera
    protected override void Start()
    {
        base.Start();
        if (_hasTarget) 
        {
            _currentState = State.Chasing; //czy te rzeczy nie powinny byc w metodzie SetTarget?
            _targetHealth.OnDeath += OnTargetDeath;  //ngdzie nie odsubskrybujesz sie od tego eventu (senownemiejsce to metoda OnTargetDeath())

            StartCoroutine(UpdatePath());
        }
    }

    public override void TakeHit(int damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (damage >= _health)
        {
#pragma warning disable CS0618 // Typ lub sk�adowa jest przestarza�a
            //nie mam pojecia co się tutaj dzieje, zinstancjonuj coś po czym od razu to zniszcz? nie wygląda to jak coś co ma dużo sensu, btw nawet gdyby to mialo sens to lepiej dla czytelnosci podzielic takie rzeczy na np 2 linisjki, tzn w pierwszszej przypisujesz do zmiennej to co zinstancjnowałeś, a w drugiej niszczyszcz tą zmienną
            Destroy(Instantiate(_enemyHitEffect, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection)) as ParticleSystem, _enemyHitEffect.startLifetime);
#pragma warning restore CS0618 // Typ lub sk�adowa jest przestarza�a
        }
        base.TakeHit(damage, hitPoint, hitDirection);
    }

    private void FixedUpdate()
    {
        //Całość do osobnej metody zeby od razu bylo wiadomo co to robi, np TryStartAttack()
        if(_hasTarget == true && Time.time > _nextAttackTime)
        {
            float sqrDistanceToTarget = (_target.position - transform.position).sqrMagnitude;
            if(sqrDistanceToTarget < Mathf.Pow (_attackDistanceThreshold + _enemyCollsionRadius + _targetCollsionRadius,2))
            {
                _nextAttackTime = Time.time + _timeBetweenAttacks;
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator Attack()
    {
        _currentState = State.Attacking;
        _nav.enabled = false;

        Vector3 originalPosition = transform.position;
        Vector3 targetDirection = (_target.position - transform.position).normalized;
        Vector3 attackPosition = _target.position - targetDirection * (_enemyCollsionRadius + _targetCollsionRadius);

        //wszsytkie magiczne liczbyponiżej: 0.5, 3, ewentualnie 1(percent) do serializowanych pol
        float percent = 0; //percent of what?
        float _attackSpeed = 3;

        _skinMaterial.color = Color.cyan; // kolor do serializowanego pola

        bool hasAppliedDamage = false;

        while (percent<=1)
        {
            if(percent >=.5f && hasAppliedDamage == false)
            {
                hasAppliedDamage = true;
                _targetHealth.TakeDamage(_damageRate);
            }
            percent += Time.deltaTime * _attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);
            yield return null;
        }

        _skinMaterial.color = _originalColor;
        _currentState = State.Chasing;
        _nav.enabled = true;
    }

    IEnumerator UpdatePath()
    {
        float refreshRate = 0.1f; // do serializownaego pola
        while (_hasTarget == true)
        {
            if (_currentState == State.Chasing)
            {
                Vector3 targetDirection = (_target.position- transform.position).normalized;
                Vector3 targetPosition = _target.position - targetDirection * (_enemyCollsionRadius + _targetCollsionRadius+ _attackDistanceThreshold/2);
                if (!_dead) //to sprawdzenie powinno chyba byc na samej gorze? tzn while (!_dead && _hasTarget == true)
                {
                    _nav.SetDestination(targetPosition);
                }
            }
            yield return new WaitForSeconds(refreshRate);
        }
    }

    private void OnTargetDeath()
    {
        _hasTarget = false;
        _currentState = State.Idle;
    }

    public void SetTarget(PlayerController playerController) //przekazywanie całego PlayerControllera nie wyglada na fajna rzecz tymbardziej, ze pobierasz później get componentem rzeczy z niego
                                                            //sensowniej bylo by zrobic jakis interfejs np ITargetable, ktory zawiera: HealthComponent, oraz float radius
                                                            //jak coś to mozemy sie zgadac i powiedzialbym dokladniej o co mi chodzi
    {
        _target = playerController.transform;
        _targetHealth = playerController.GetComponent<HealthComponent>();
        _hasTarget = true;

        _enemyCollsionRadius = GetComponent<CapsuleCollider>().radius;
        _targetCollsionRadius = playerController.GetComponent<CapsuleCollider>().radius;
    }

    public void SetCharacteristics(float moveSpeed, int hitsToKillPlayer, int enemyHealth, Color skinColour)
    {
        _nav.speed = moveSpeed;

        if (_hasTarget)
        {
            _damageRate = Mathf.CeilToInt(_targetHealth._startingHealth / hitsToKillPlayer);
        }
        _startingHealth = enemyHealth;

        _skinMaterial = GetComponent<Renderer>().sharedMaterial;
        _skinMaterial.color = skinColour;
        _originalColor = _skinMaterial.color;
    }
}
