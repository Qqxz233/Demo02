using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    public GameObject menuPanel;
    public Button startButton;

    private void Awake()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void HideMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void OnStartClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.UIClick);
        GameManager.Instance.StartGame();
    }
}