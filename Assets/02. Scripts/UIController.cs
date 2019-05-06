using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour {
    private bool gameConnected = false;
    private bool roomAvailable = false;

    // Pregame UI
    [SerializeField]
    private GameObject connectionUI;
    [SerializeField]
    private Button[] connectionButtons;
    [SerializeField]
    private TextMeshProUGUI statusText;
    [SerializeField]
    private TMP_InputField nameText;

    // Ingame UI
    [SerializeField]
    private TextMeshProUGUI[] scoresObjects;
    [SerializeField]
    private Image[] imagesObjects;
    [SerializeField]
    private TextMeshProUGUI timerObject;
    [SerializeField]
    private TextMeshProUGUI winnerObject;

    public void ManageName()
    {
        if (nameText.text.Length > 0)
        {
            connectionButtons[0].interactable = gameConnected;
            connectionButtons[1].interactable = roomAvailable;
            statusText.SetText("Estado: Conectado al servidor");
        } else
        {
            connectionButtons[0].interactable = false;
            connectionButtons[1].interactable = false;
            statusText.SetText("Estado: Conectado al servidor\nEsperando nombre");
        }
    }
    
    public void SetGameConnected(bool ra)
    {
        roomAvailable = ra;
        gameConnected = true;
        statusText.SetText("Estado: Conectado al servidor\nEsperando nombre");
    }

    public string GetPlayerName()
    {
        return nameText.text;
    }

    public void DeactivatePregameUI()
    {
        connectionUI.SetActive(false);
    }

    public void SetPlayerScore(int player, int score)
    {
        scoresObjects[player].text = score.ToString();
    }

    public void SetMaxPlayers(int max)
    {
        for (int i = 0; i < max; ++i)
        {
            scoresObjects[i].gameObject.SetActive(true);
            imagesObjects[i].gameObject.SetActive(true);
        }
    }

    public void ActivateTimer()
    {
        timerObject.gameObject.SetActive(true);
    }

    public void SetTime(int time)
    {
        int minutes = time / 60;
        int seconds = time % 60;

        string sminutes = minutes.ToString();
        timerObject.text = string.Format("{0:00}:{1:00}s", minutes, seconds);
    }

    public void SetWinner(string winner)
    {
        winnerObject.gameObject.SetActive(true);
        winnerObject.text = string.Format("~The mummy of the party is {0}~", winner);
        timerObject.gameObject.SetActive(false);
    }
}
