using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    private string previousScene;
    private string currentScene;

    public bool paused;

    public GameObject MainMenu;
    public GameObject OptionsMenu;

    public GameObject GameplayMenu;
    public GameObject Difficulty;
    public GameObject Enemies;
    public GameObject Magazine;
    public GameObject Sprint;
    public GameObject Spray;
    public GameObject Health;

    public GameObject SettingsMenu;
    public GameObject Sense;
    public GameObject SFX;
    public GameObject Music;
    public GameObject Sight;
    public GameObject Display;
    public Material[] Reticles;

    public GameObject LoadingMenu;
    public Slider bar;

    public float sensitivity;
    public float spread;
    public int AILevel;
    public int AICount;
    public int magSize;
    public int maxSpeed;
    public float effects;
    public float volume;
    public int reticle;
    public int playerHP;
    public int index;

    public GameObject enemy;
    private GameObject[] actives;
    public GameObject[] respawns;

    void Start()
    {
        //Get scene
        currentScene = SceneManager.GetActiveScene().name;
        previousScene = currentScene;

        //Get predetermined values
        SensSlide();
        SpreadSlide();
        DiffSlide();
        EnemySlide();
        MagSlide();
        SpeedSlide();
        EffectVolume();
        MusicVolume();
        ReticleDistance();
        ReticleShape();
        HealthSlide();
    }

    void Update()
    {
        currentScene = SceneManager.GetActiveScene().name;
        if (previousScene != currentScene) {
            previousScene = currentScene;
            if (currentScene == "Main") {
                //Find new GameManager and copy values onto it
                Scene copy = SceneManager.GetSceneByName("Main");
                GameObject[] list = copy.GetRootGameObjects();
                list[0].GetComponent<GameManager>().Override(sensitivity, spread, AILevel, AICount, magSize, maxSpeed, effects, volume, reticle, playerHP, index);

                //Destroy old GameManager when loading main
                Destroy(gameObject);
            } else {
                //Find all respawns on Game loading
                respawns = GameObject.FindGameObjectsWithTag("Respawn");
            }
        }

        if (currentScene != "Main") {
            //Find all active enemies. Spawn if below enemy count
            actives = GameObject.FindGameObjectsWithTag("Enemy");
            if (actives.Length <= AICount) {
                Regen(AICount - actives.Length);
            }
        }
    }

    //Generate X enemies at random spawns
    private void Regen(int count)
    {
        for (int i = 0; i < count; i++) {
            int spawn = Random.Range(0, respawns.Length);
            Instantiate(enemy, respawns[spawn].transform.position, respawns[spawn].transform.rotation);
        }
    }

    //Show/hide options
    public void Options(bool showOptions)
    {
        MainMenu.SetActive(!showOptions);
        OptionsMenu.SetActive(showOptions);
    }

    //Show/hide gameplay
    public void Gameplay(bool showGameplay)
    {
        OptionsMenu.SetActive(!showGameplay);
        GameplayMenu.SetActive(showGameplay);
    }

    //Show/hide settings
    public void Settings(bool showSettings)
    {
        OptionsMenu.SetActive(!showSettings);
        SettingsMenu.SetActive(showSettings);
    }

    //Slider Functions
    public void SensSlide() { sensitivity = Sense.GetComponent<Slider>().value; }

    public void SpreadSlide() { spread = Spray.GetComponent<Slider>().value; }

    public void DiffSlide() { AILevel = Mathf.RoundToInt(Difficulty.GetComponent<Slider>().value); }

    public void EnemySlide() { AICount = Mathf.RoundToInt(Enemies.GetComponent<Slider>().value); }

    public void MagSlide() { magSize = Mathf.RoundToInt(Magazine.GetComponent<Slider>().value); }

    public void SpeedSlide() { maxSpeed = Mathf.RoundToInt(Sprint.GetComponent<Slider>().value); }

    public void EffectVolume() { effects = SFX.GetComponent<Slider>().value; }

    public void MusicVolume() { volume = Music.GetComponent<Slider>().value; }

    public void ReticleDistance() { reticle = 31 - Mathf.RoundToInt(Sight.GetComponent<Slider>().value); }

    public void NextPrevShape(int add)
    {
        index = (index + add + Reticles.Length) % Reticles.Length;
        ReticleShape();
    }

    public void ReticleShape() { Display.GetComponent<Image>().material = Reticles[index]; }

    public void HealthSlide() { playerHP = Mathf.RoundToInt(Health.GetComponent<Slider>().value); }

    public void Quit() { Application.Quit(); }

    public void Override(float Sn, float Ra, int Df, int En, int Mg, int Sp, float Ef, float Mu, int Rt, int Hp, int Dx)
    {
        sensitivity = Sn;
        spread = Ra;
        AILevel = Df;
        AICount = En;
        magSize = Mg;
        maxSpeed = Sp;
        effects = Ef;
        volume = Mu;
        reticle = Rt;
        playerHP = Hp;
        index = Dx;
        //Set slider values
        Sense.GetComponent<Slider>().value = sensitivity;
        Spray.GetComponent<Slider>().value = spread;
        Difficulty.GetComponent<Slider>().value = AILevel;
        Enemies.GetComponent<Slider>().value = AICount;
        Magazine.GetComponent<Slider>().value = magSize;
        Sprint.GetComponent<Slider>().value = maxSpeed;
        SFX.GetComponent<Slider>().value = effects;
        Music.GetComponent<Slider>().value = volume;
        Sight.GetComponent<Slider>().value = reticle;
        Health.GetComponent<Slider>().value = playerHP;
        NextPrevShape(0);
    }

    //Load desired scene
    public void Load(string scene)
    {
        DontDestroyOnLoad(gameObject);
        if (currentScene == "Main") {
            MainMenu.SetActive(false);
            LoadingMenu.SetActive(true);
        }
        StartCoroutine(LoadScene(scene));
    }

    private IEnumerator LoadScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        while (!asyncLoad.isDone) {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (currentScene == "Main") bar.value = progress;
            yield return null;
        }
    }
}