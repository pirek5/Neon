using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Tile;

public class MapProvider : MonoBehaviour
{
    [SerializeField] private List<GameObject> _mapList;

    public GameObject _map;
    private Transform[,] _tileMap;
    private List<Coord> _allUnoccupiedTileCoords = new List<Coord>();
    private float _tileSize;
    private int _mapsizeX;
    private int _mapsizeY;

    private void Start()
    {
        if (_mapList != null && _mapList.Count > 0)
        {
            GameObject selectedMap = _mapList[Random.Range(0, _mapList.Count)];
            GameObject instantiatedMap = Instantiate(selectedMap, transform.position, Quaternion.identity);
            if(_map != null) { _map = null; }
            _map = instantiatedMap;

            RecollectTiles();
        }
        else
        {
            Debug.LogError("List is empty or invalid!");
        }
    }

    private void RecollectTiles()
    {
        Tile[] tiles = FindObjectsOfType<Tile>();
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        float maxTileSize = float.MinValue;
        foreach (Tile tile in tiles)
        {
            if (tile.coord.x > maxX)
            {
                maxX = tile.coord.x;
            }
            if (tile.coord.y > maxY)
            {
                maxY = tile.coord.y;
            }
            if (!tile.isOccupied)
            {
                _allUnoccupiedTileCoords.Add(tile.coord);
            }
            if (tile._catchTileSize > maxTileSize)
            {
                maxTileSize = tile._catchTileSize;
            }
        }
        _tileMap = new Transform[maxX + 1, maxY + 1];

        foreach (Tile tile in tiles)
        {
            _tileMap[tile.coord.x, tile.coord.y] = tile.transform;
        }
        _tileSize = maxTileSize;
        _mapsizeX = maxX;
        _mapsizeY = maxY;
    }

    public Transform TileFromPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / _tileSize + (_mapsizeX - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / _tileSize + (_mapsizeY - 1) / 2f);
        x = Mathf.Clamp(x, 0, _tileMap.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, _tileMap.GetLength(1) - 1);
        return _tileMap[x, y];
    }


    public Transform GetRandomOpenTile()
    {
        if (_allUnoccupiedTileCoords.Count == 0)
        {
            return null;
        }
        int randomIndex = Random.Range(0, _allUnoccupiedTileCoords.Count);
        Coord randomCoord = _allUnoccupiedTileCoords[randomIndex];

        return _tileMap[randomCoord.x, randomCoord.y];
    }
}
