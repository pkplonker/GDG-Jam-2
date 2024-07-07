using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuBehaviour : UIComponent
{
    public Button closeButton;

    public SettingsMenuBehaviour settings;
    public CreditsMenuBehaviour credits;

    public SceneSeeds sceneSeeds;

    protected override void Awake()
    {
        base.Awake();


        #if UNITY_WEBGL
            closeButton.gameObject.SetActive(false);
        #endif
    }

    public void OnStartGame()
    {
        Debug.Log("Starting Game");
        //Select Seed
        UnityEngine.Random.InitState((int)Time.time);

        sceneSeeds.currentSeed = sceneSeeds.availableSeeds[Random.Range(0,sceneSeeds.availableSeeds.Count)];



        SceneManager.LoadScene("GameScene");
    }

    public void OnSettings()
    {
        settings.DisplayComponent(settings,true);
    }

    public void OnCredits()
    {
        credits.DisplayComponent(credits,true);
    }

    public void OnExitGame()
    {
        Application.Quit();
    }
}
