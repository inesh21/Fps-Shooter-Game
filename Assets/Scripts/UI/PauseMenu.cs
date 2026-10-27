using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple in-game pause menu.  Press Escape to toggle.
/// Shows the Relay join code (if hosting), a Resume button, and a Quit button.
/// Attach to a canvas GameObject that is a child of the HUD or scene root.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button quitButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text joinCodeLabel;
    [SerializeField] private TMP_Text statusLabel;

    private bool isPaused;

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (copyCodeButton != null)
            copyCodeButton.onClick.AddListener(CopyCode);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(Disconnect);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPause(isPaused);
    }

    public void SetPause(bool paused)
    {
        isPaused = paused;
        if (pausePanel != null) pausePanel.SetActive(paused);

        // Unlock cursor while paused; re-lock on resume.
        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        RefreshCodeLabel();
    }

    private void RefreshCodeLabel()
    {
        if (joinCodeLabel == null) return;

        // Try to pull the code from RelayConnect if it is in the scene.
        var relay = FindObjectOfType<RelayConnect>();
        string code = relay != null ? relay.CurrentJoinCode : "";

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        if (!string.IsNullOrEmpty(code) && isHost)
        {
            joinCodeLabel.text = $"Join Code: {code}";
            if (copyCodeButton != null) copyCodeButton.gameObject.SetActive(true);
        }
        else
        {
            joinCodeLabel.text = isHost ? "Not connected via Relay" : "";
            if (copyCodeButton != null) copyCodeButton.gameObject.SetActive(false);
        }
    }

    private void Resume()   => SetPause(false);

    private void CopyCode()
    {
        var relay = FindObjectOfType<RelayConnect>();
        if (relay != null && !string.IsNullOrEmpty(relay.CurrentJoinCode))
        {
            GUIUtility.systemCopyBuffer = relay.CurrentJoinCode;
            if (statusLabel != null) statusLabel.text = "Code copied!";
        }
    }

    private void Disconnect()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        // Optionally load the main menu scene.
        // UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        SetPause(false);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
