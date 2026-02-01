using Managers;
using UnityEngine;

public class ResumeGame : MonoBehaviour
{
    public void resumeGame()
    {
        GameManager.Instance.ResumeGame();
    }
}
