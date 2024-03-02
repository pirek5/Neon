using System.Collections;
using UnityEngine;
using Zenject;

public class Spawner : MonoBehaviour
{
    //[SerializeField] private PlayerController _playerPrefab;
    private MapProvider _mapProvider;
    private PlayerController _playerController;
    private Transform _playerPosition => _playerController.transform; // to nie jest pozycja playera, tylko transform playera; to jest property wiec nie powinno sie zaczyanć od _p, tylko P

    //SerialzieField private
    public Wawe[] wawes; //pierdoła ale masz literówke w wave xd
    private Wawe currentWawe;
    //SerialzieField private _enemyPrefab;
    public Enemy enemy;
    private int _currentWaweNumber;
    private int _enemiesToSpawn;
    private int _enemiesRamainingAlive; //literówka
    private float _nextSpawnTime;
    private float _timeBetweenCampaingChecks = 2f;
    private float _nextCampCheckTime;
    private float _campThresholdDistance = 1.5f;
    private Vector3 campPositionOld;
    private bool isCamping; //brak _
    private bool isDiseabled; //literówka brak _

    //public event System.Action<int> OnNewWave;

    [Inject]
    public void Construct(PlayerController playerController, MapProvider mapProvider)
    {
        _playerController = playerController;
        _mapProvider = mapProvider;
        Debug.Log("spawnerInstalled");
    }

    private void Start()
    {
        //wszystko oprócz NextWave do osobnej metody, np "Initialize"
        _nextCampCheckTime = _timeBetweenCampaingChecks + Time.time;
        campPositionOld = _playerController.transform.position;
        //SpawnPlayer();
        _playerController.GetComponent<HealthComponent>().OnDeath += OnPlayerDeath; //jeżeli musisz pobierać coś GetComponentem z konkretnej klasy, to raczej powinno to byc po prostu udostępnione publicznie z tej klasy w jakieś formie, dodatkowo nie odsubskrybowałeś się od eventu nigdzie
        _playerController.Respawn();
        NextWave();
    }

    private void Update()
    {
        //do osobnych metod mowiacych co sie dzieje np TrySpawn , UpdatePositionAndTime, StartSpawnEnemy (to tylko propozycje); co do zasady, cała logika w klasie powinna byc jakos nazwana, nie powinno sie wrzucac bezposrednio 'instrukcji' do metod typu Update czy Start bez nazwania ich jakas nazwa bo nie mamy zadnych informacji co dana rzecz ma robic i  trzeba t orozszyfrowywac po kodzie
        if (!isDiseabled)
        {
            //check if camping
            if (Time.time > _nextCampCheckTime)
            {
                if (_playerPosition != null) // to sparwdzenie nie ma sensu, jedyna możliwość zeby ta zmienna była nullem byłą by wtedy gdyby PlayerController był nullem, a jest to injectowane, czly by design nie powinno by nullem, dodaktowo nawet gdyby bylo nullem to ten null check nie rozwiaze problemu bo poleci Ci null ref w trakcie próby pobrania transforma z PlayerControllera korzystajac z tego property
                {
                    _nextCampCheckTime = Time.time + _timeBetweenCampaingChecks;
                    isCamping = (Vector3.Distance(_playerPosition.position, campPositionOld) < _campThresholdDistance);
                    campPositionOld = _playerPosition.position;
                }
                isCamping = false; // w  kazdej klatce nadpisujesz wartosc isCamping na false, wiec niezalzenie co ustawisz w linijce 60 tutaj zostanie to nadpisane na false
            }
            if ((_enemiesToSpawn > 0 || currentWawe.infinite) && Time.time > _nextSpawnTime)
            {
                _enemiesToSpawn--;
                _nextSpawnTime = Time.time + currentWawe.timeBetwenSpawn;
                if(_playerPosition.position !=null) // postion to struct, wiec nigdy nie będzie nullem, dodatkowo, z zasady rzeczy po ifie powinny byc w klamrach
                StartCoroutine(SpawEnemy());
            }
        }
    }

    private IEnumerator SpawEnemy()
    {
        Transform randomTile = null;
        float spawnDelay = 1f;
        float tileFlashSpeed = 4;

        if (isCamping)
        {
            if (_playerPosition != null) //jw
            {
                randomTile = _mapProvider.TileFromPosition(_playerPosition.position);
            }
        }
        else
        {
            randomTile = _mapProvider.GetRandomOpenTile();
            Debug.Log(randomTile.position);
        }
        //spawn warning 
        Material tileMat = randomTile.GetComponent<Renderer>().material;
        Color inialColor = Color.white;//tileMat.color;   //kolory do serializowanego pola
        Color spawnColor = Color.cyan;
        float spawnTimer = 0;

        while (spawnTimer < spawnDelay)
        {
            tileMat.color = Color.Lerp(inialColor, spawnColor, Mathf.PingPong(spawnTimer * tileFlashSpeed, 1f));
            spawnTimer += Time.deltaTime;
            yield return null;
        }

        //zmiast pisac komentarze podzziel na pod metody
        //spawn
        Enemy spawnedEnemy = Instantiate(enemy, randomTile.position + Vector3.up, Quaternion.identity);
        spawnedEnemy.OnDeath += OnEnemyDeath;
        spawnedEnemy.SetTarget(_playerController);
        spawnedEnemy.SetCharacteristics(currentWawe.moveSpeed, currentWawe.hitsToKillPlayer, currentWawe.enemyHealth, currentWawe.enemyColor);
    }

    //co do zasady nazwy klas powinny mówić co robią a nie czym są, czyli np HandleNewWave()
    private void NextWave()
    {
        _currentWaweNumber++;
        if (_currentWaweNumber - 1 < wawes.Length)
        {
            currentWawe = wawes[_currentWaweNumber - 1];
            _enemiesToSpawn = currentWawe.enemyCount;
            _enemiesRamainingAlive = _enemiesToSpawn;
            //if(OnNewWave != null)
            //{
            //    OnNewWave(_currentWaweNumber);
            //}
        }
        else //brak klamr po else (wiem ze teoretycznie, dla jednej linijki nie trzeba, ale nie spotkałem nikogo kto by tak robił i uważał ze to dobre :)) 
            Debug.Log("you won nothing!");
    }

    public void ResetPlayerPosition()
    {
        _playerPosition.position = _mapProvider.GetRandomOpenTile().position + Vector3.up;
    }

    private void OnPlayerDeath()
    {
        isDiseabled = true;
    }

    private void OnEnemyDeath()
    {
        _enemiesRamainingAlive --;
        if (_enemiesRamainingAlive == 0)
        {
            _mapProvider.NextMap();
            //_mapProvider = FindObjectOfType<MapProvider>();
            NextWave();
            _playerController.Respawn(); //turn off PlayerController
        }
    }

    [System.Serializable]
    public class Wawe
    {
        public int enemyCount;
        public float timeBetwenSpawn;//literówka

        public float moveSpeed;
        public int hitsToKillPlayer;
        public int enemyHealth;
        public Color enemyColor;
        public bool infinite;
    }
}

//private void SpawnPlayer()
//{
//    if (_playerPrefab != null)
//    {
//        HealthComponent _playerInstance = Instantiate(_playerPrefab, _mapGenerator.GetRandomOpenTile());
//        _playerHealthComponent = _playerInstance.GetComponent<HealthComponent>();
//        _playerPosition = _playerHealthComponent.transform;
//        campPositionOld = _playerHealthComponent.transform.position;
//    }
//    else
//        Debug.Log("Missing player prefab");
//}