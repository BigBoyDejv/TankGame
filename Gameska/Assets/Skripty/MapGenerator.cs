using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Enemy")]
    public GameObject[] enemyPrefabs;
    public GameObject shellPrefab;

    [Header("Krytie")]
    public GameObject coverPrefab;      // Cube alebo skala
    public Material groundMaterial;
    public Material wallMaterial;

    [Header("Nastavenia mapy")]
    public int numberOfCamps = 4;
    public float corridorLength = 200f;
    public float corridorWidth = 30f;
    public float wallHeight = 8f;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        CreateGround();
        CreateCorridorWalls();
        CreateCamps();
        CreateOpenField();
    }

    void CreateGround()
    {
        // Zem
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, corridorLength / 2f);
        ground.transform.localScale = new Vector3(corridorWidth / 5f, 1f, corridorLength / 5f);
        if (groundMaterial != null)
            ground.GetComponent<Renderer>().material = groundMaterial;
    }

    void CreateCorridorWalls()
    {
        float spacing = 8f;
        int wallCount = (int)(corridorLength / spacing);

        for (int i = 0; i < wallCount; i++)
        {
            float z = i * spacing;

            // Ľavá stena (kopec)
            CreateWallBlock(new Vector3(-corridorWidth / 2f - 2f, wallHeight / 2f, z), spacing);
            // Pravá stena (kopec)
            CreateWallBlock(new Vector3(corridorWidth / 2f + 2f, wallHeight / 2f, z), spacing);
        }
    }

    void CreateWallBlock(Vector3 pos, float size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.position = pos;
        wall.transform.localScale = new Vector3(size, wallHeight, size);
        if (wallMaterial != null)
            wall.GetComponent<Renderer>().material = wallMaterial;
    }

    void CreateCamps()
    {
        float campSpacing = corridorLength / (numberOfCamps + 1);

        for (int i = 0; i < numberOfCamps; i++)
        {
            float z = campSpacing * (i + 1);
            bool isOpenField = (i == numberOfCamps / 2); // stredný camp = open field

            if (isOpenField)
                CreateOpenFieldCamp(z, i + 2); // viac enemy v open field
            else
                CreateCamp(z, i + 1);
        }
    }

    void CreateCamp(float z, int enemyCount)
    {
        GameObject campParent = new GameObject("Camp_" + z);
        campParent.transform.position = new Vector3(0f, 0f, z);

        // Krytie - rozložené po campe
        Vector3[] coverPositions = {
            new Vector3(-8f, 0f, z - 5f),
            new Vector3(8f, 0f, z - 5f),
            new Vector3(-5f, 0f, z + 5f),
            new Vector3(5f, 0f, z + 5f),
            new Vector3(0f, 0f, z - 8f),
        };

        foreach (Vector3 pos in coverPositions)
            CreateCover(pos, campParent.transform);

        // Enemy tanky
        for (int i = 0; i < enemyCount; i++)
        {
            float xOffset = (i % 2 == 0) ? -10f : 10f;
            float zOffset = (i / 2) * 6f;
            SpawnEnemy(new Vector3(xOffset, 0f, z + zOffset), campParent.transform);
        }

        Debug.Log($"Camp vytvorený na Z:{z} s {enemyCount} enemy");
    }

    void CreateOpenFieldCamp(float z, int enemyCount)
    {
        GameObject campParent = new GameObject("OpenField_Camp_" + z);

        // Rozšírené otvorené pole — žiadne steny po stranách
        GameObject openGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
        openGround.name = "OpenGround";
        openGround.transform.position = new Vector3(0f, -0.5f, z);
        openGround.transform.localScale = new Vector3(8f, 1f, 6f);
        openGround.transform.SetParent(campParent.transform);

        // Len pár kusov krytia — ťažší camp
        CreateCover(new Vector3(-15f, 0f, z), campParent.transform);
        CreateCover(new Vector3(15f, 0f, z), campParent.transform);
        CreateCover(new Vector3(0f, 0f, z - 10f), campParent.transform);

        // Viac enemy v open fielde
        for (int i = 0; i < enemyCount; i++)
        {
            float xOffset = Random.Range(-20f, 20f);
            float zOffset = Random.Range(-10f, 10f);
            SpawnEnemy(new Vector3(xOffset, 0f, z + zOffset), campParent.transform);
        }

        Debug.Log($"Open Field camp vytvorený na Z:{z} s {enemyCount} enemy");
    }

    void CreateOpenField()
    {
        // Wide open section pred posledným campom
        GameObject openField = GameObject.CreatePrimitive(PrimitiveType.Plane);
        openField.name = "OpenField";
        openField.transform.position = new Vector3(0f, -0.51f, corridorLength * 0.75f);
        openField.transform.localScale = new Vector3(15f, 1f, 8f);
        if (groundMaterial != null)
            openField.GetComponent<Renderer>().material = groundMaterial;
    }

    void CreateCover(Vector3 position, Transform parent)
    {
        GameObject cover;
        if (coverPrefab != null)
            cover = Instantiate(coverPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        else
        {
            cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.transform.position = position + Vector3.up * 1.5f;
            cover.transform.localScale = new Vector3(
                Random.Range(3f, 6f),
                Random.Range(2f, 4f),
                Random.Range(3f, 6f)
            );
            cover.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        cover.name = "Cover";
        if (parent != null) cover.transform.SetParent(parent);
    }

    void SpawnEnemy(Vector3 position, Transform parent)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Nastav Enemy Prefabs v MapGenerator!");
            return;
        }

        // Náhodný enemy tank z poľa
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemy = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        enemy.name = "Enemy_" + position.z;
        if (parent != null) enemy.transform.SetParent(parent);

        // Pridaj TankHealth ak ho nemá
        if (enemy.GetComponent<TankHealth>() == null)
            enemy.AddComponent<TankHealth>();

        // Pridaj EnemyTank AI ak ho nemá
        EnemyTank ai = enemy.GetComponent<EnemyTank>();
        if (ai == null) ai = enemy.AddComponent<EnemyTank>();
        if (shellPrefab != null) ai.shellPrefab = shellPrefab;
    }
}