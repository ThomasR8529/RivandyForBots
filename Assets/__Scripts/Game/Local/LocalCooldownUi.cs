using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LocalCooldownUi : NetworkBehaviour
{
    public Text Qcd;
    public RawImage Qimage;
    public bool Qstatus;

    public Text Zcd;
    public RawImage Zimage;
    public bool Zstatus;

    public Text Ecd;
    public RawImage Eimage;
    public bool Estatus;

    public Text Rcd;
    public RawImage Rimage;
    public bool Rstatus;

    GameObject player;
    PlayerClasses playerClass;
    PlayerStatistics playerStats;

    public Text localPlayerKills;
    public Text localPlayerPoints;
    public Text localPlayerText;
    public Slider localPlayerHeath;
    public Text localPlayerHealthText;

    // Todo: Il faut ajouter dans l'UI les touches pour chaque sort
    void Update()
    {
        if (IsClient)
        {
            if (!player)
            {
                player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
                if (!playerClass) playerClass = player.GetComponent<PlayerClasses>();
                if (!playerStats) playerStats = player.GetComponent<PlayerStatistics>();
                Qimage.texture = playerClass.spells[0].avatar;
                Zimage.texture = playerClass.spells[1].avatar;
                Eimage.texture = playerClass.spells[2].avatar;
                Rimage.texture = playerClass.spells[3].avatar;
            }
        }

        if (playerStats)
        {
            if (localPlayerText.text == "" || localPlayerText.text == "test") localPlayerText.text = PlayerData.player.data.playerName.Value.ToString();
            localPlayerHeath.value = playerStats.playerStatData.health;
            localPlayerHealthText.text = playerStats.playerStatData.health + " / 100";
            localPlayerKills.text = playerStats.GetKills() + " kills";
            localPlayerPoints.text = playerStats.GetPoints() + " points";
        }

        if (playerClass)
        {

            // Q
            if (playerClass.warriorSpell1CD != 0.0f)
            {
                Qcd.text = Mathf.Round(playerClass.warriorSpell1CD * 10.0f) * 0.1f + " s";
                if (Qstatus)
                {
                    Qstatus = false;
                    Qimage.color = new Color(0.3584906f, 0.3584906f, 0.3584906f, 0.3584906f);
                    Qcd.gameObject.SetActive(true);
                }
            }
            else
            {
                if (!Qstatus)
                {
                    Qstatus = true;
                    Qimage.color = new Color(1, 1, 1, 1);
                    Qcd.gameObject.SetActive(false);
                }
            }

            // Z
            if (playerClass.warriorSpell2CD != 0.0f)
            {
                Zcd.text = Mathf.Round(playerClass.warriorSpell2CD * 10.0f) * 0.1f + " s";
                if (Zstatus)
                {
                    Zstatus = false;
                    Zimage.color = new Color(0.3584906f, 0.3584906f, 0.3584906f, 0.3584906f);
                    Zcd.gameObject.SetActive(true);
                }
            }
            else
            {
                if (!Zstatus)
                {
                    Zstatus = true;
                    Zimage.color = new Color(1, 1, 1, 1);
                    Zcd.gameObject.SetActive(false);
                }
            }

            // E
            if (playerClass.warriorSpell3CD != 0.0f)
            {
                Ecd.text = Mathf.Round(playerClass.warriorSpell3CD * 10.0f) * 0.1f + " s";
                if (Estatus)
                {
                    Estatus = false;
                    Eimage.color = new Color(0.3584906f, 0.3584906f, 0.3584906f, 0.3584906f);
                    Ecd.gameObject.SetActive(true);
                }
            }
            else
            {
                if (!Estatus)
                {
                    Estatus = true;
                    Eimage.color = new Color(1, 1, 1, 1);
                    Ecd.gameObject.SetActive(false);
                }
            }
            // R
            if (playerClass.warriorSpell4CD != 0.0f)
            {
                Rcd.text = Mathf.Round(playerClass.warriorSpell4CD * 10.0f) * 0.1f + " s";
                if (Rstatus)
                {
                    Rstatus = false;
                    Rimage.color = new Color(0.3584906f, 0.3584906f, 0.3584906f, 0.3584906f);
                    Rcd.gameObject.SetActive(true);
                }
            }
            else
            {
                if (!Rstatus)
                {
                    Rstatus = true;
                    Rimage.color = new Color(1, 1, 1, 1);
                    Rcd.gameObject.SetActive(false);
                }
            }
        }

    }
}
