using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public static PlayerSpawn Instance { get; private set; }

    [SerializeField] private Transform spawnPoint;

    public Transform SpawnTransform => spawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}