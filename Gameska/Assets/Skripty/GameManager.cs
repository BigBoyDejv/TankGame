using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Vlny nepriateľov")]
    public GameObject enemyTankPrefab;
    public Transform[] spawnPoints;
    public int enemiesPerWave = 3;
    public float timeBetweenWaves = 5f;

    [Header("UI")]
    public Text waveText;
    public Text enemyCountText;
    public GameObject gameOverUI;
    public GameObject levelCompleteUI;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool waitingForNextWave = false;

    void Start()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (levelCompleteUI != null) levelCompleteUI.SetActive(false);

        StartCoroutine(StartNextWave());
    }

    void Update()
    {
        if (enemyCountText != null)
            enemyCountText.text = "Nepriatelia: " + enemiesAlive;
    }

    IEnumerator StartNextWave()
    {
        waitingForNextWave = true;
        currentWave++;

        if (waveText != null)
            waveText.text = "Vlna " + currentWave;

        Debug.Log("Vlna " + currentWave + " začína!");

        yield return new WaitForSeconds(timeBetweenWaves);

        SpawnEnemies();
        waitingForNextWave = false;
    }

    void SpawnEnemies()
    {
        int enemiesToSpawn = enemiesPerWave + (currentWave - 1) * 2;
        enemiesAlive = enemiesToSpawn;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (spawnPoints.Length == 0) break;

            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            Instantiate(enemyTankPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    public void EnemyDestroyed()
    {
        enemiesAlive--;
        Debug.Log("Nepriateľ zničený! Zostatok: " + enemiesAlive);

        if (enemiesAlive <= 0 && !waitingForNextWave)
        {
            StartCoroutine(StartNextWave());
        }
    }

    public void GameOver()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
