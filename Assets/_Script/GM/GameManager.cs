using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;



public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static Action OnRoundStart;
    public static Action OnLoseRound;
    public static Action<bool> OnIntermissionCall;

    [Header("Gameplay")]
    [SerializeField] int roundCount;

    [SerializeField] int startTimerPerRound;
    [SerializeField] int intermissionTimer = 5;
    [SerializeField] TextMeshProUGUI timerTxt;
    [SerializeField] GameObject shopTimeRef;
    private float currentTimer;
    public float GetTimerRound => currentTimer;


    [SerializeField] int startBaggageToSpawn;
    [SerializeField] int startPeopleToSpawn;

    [SerializeField] List<Spot> spotsList;

    private List<EntityInfo> baggagePerSpot = new List<EntityInfo>();
    private List<EntityInfo> peoplePerSpot = new List<EntityInfo>();

    private int currentBaggageOnSpot;
    private int currentPeopleOnSpot;

    [SerializeField] AudioSource audioSource;


    [Header("Debug")]
    [SerializeField] TMP_InputField consolecommand;
    [SerializeField] GameObject shop;
    [SerializeField] GameObject money;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        instance = this;

        OnLoseRound += () =>
        {
            roundCount = 0;
            StartGame();
        };
    }

    private void Start()
    {
        if (audioSource)
        {
            audioSource?.Play();
        }
        StartGame();
    }


    bool console;
    private void Update()
    {
        currentTimer -= Time.deltaTime;
        timerTxt.text = $"Timer:{currentTimer.ToString("0")}";
        if (currentTimer <= 0)
        {
            if (intermissionON) { return; }
            OnLoseRound?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!console)
            {
                console = true;
                consolecommand.gameObject.SetActive(true);
                consolecommand.text = "";
                EventSystem.current.SetSelectedGameObject(consolecommand.gameObject);
            }
            else
            {
                string commandText = consolecommand.text;
                if (!String.IsNullOrWhiteSpace(commandText))
                {
                    if (commandText == "open shop")
                    {
                        shop.SetActive(true);
                        console = false;
                        consolecommand.gameObject.SetActive(false);
                        EventSystem.current.SetSelectedGameObject(null);
                        return;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(commandText, @"^round (\d+)$");
                    if (match.Success)
                    {
                        int roundNumber = int.Parse(match.Groups[1].Value);
                        OnLoseRound?.Invoke();
                        roundCount = roundNumber - 1;
                        StartGame();
                        console = false;
                        consolecommand.gameObject.SetActive(false);
                        EventSystem.current.SetSelectedGameObject(null);
                        return;
                    }
                    match = System.Text.RegularExpressions.Regex.Match(commandText, @"^money (\d+)$");
                    if (match.Success)
                    {
                        int amount = int.Parse(match.Groups[1].Value);
                        money.GetComponent<MoneyCounter>().SetMoney(amount);
                        console = false;
                        consolecommand.gameObject.SetActive(false);
                        EventSystem.current.SetSelectedGameObject(null);
                        return;
                    }
                }
                console = false;
                consolecommand.gameObject.SetActive(false);
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    private void StartGame()
    {
        roundCount++;
        if (roundCount == 1)
        {
            currentTimer = startTimerPerRound;
        }
        else
        {
            currentTimer = startTimerPerRound + (startTimerPerRound * 0.5f * (roundCount - 1));
        }
        baggagePerSpot.Clear();
        peoplePerSpot.Clear();

        currentBaggageOnSpot = 0;
        currentPeopleOnSpot = 0;

        int maxBaggage = startBaggageToSpawn * roundCount;
        int maxPeople = startPeopleToSpawn * roundCount;

        for (int i = 0; i < spotsList.Count; i++)
        {
            EntityInfo infoBaggage = new EntityInfo();
            EntityInfo infoPeople = new EntityInfo();

            if (i < spotsList.Count - 1)
            {
                if (maxBaggage > 0)
                {
                    int baggageInSpot = UnityEngine.Random.Range(0, maxBaggage);
                    spotsList[i].amountBaggage = baggageInSpot;
                    maxBaggage -= baggageInSpot;
                    infoBaggage.count = baggageInSpot;
                    infoBaggage.color = spotsList[i].GetColor;
                }
                if (maxPeople > 0)
                {
                    int peopleInSpot = UnityEngine.Random.Range(0, maxPeople);
                    spotsList[i].amountPeople = peopleInSpot;
                    maxPeople -= peopleInSpot;
                    infoPeople.count = peopleInSpot;
                    infoPeople.color = spotsList[i].GetColor;
                }
            }
            else
            {
                spotsList[i].amountBaggage = maxBaggage;
                spotsList[i].amountPeople = maxPeople;
                infoBaggage.count = maxBaggage;
                infoBaggage.color = spotsList[i].GetColor;
                infoPeople.count = maxPeople;
                infoPeople.color = spotsList[i].GetColor;
            }
            baggagePerSpot.Add(infoBaggage);
            peoplePerSpot.Add(infoPeople);
        }
        OnRoundStart?.Invoke();
    }
    public List<EntityInfo> GetEnityInfoToSpawn(EntityType entityType)
    {
        return entityType == EntityType.Baggage ? baggagePerSpot : peoplePerSpot;
    }
    public void OnFullSpot(int amountBaggage, int amountPeople)
    {
        currentBaggageOnSpot += amountBaggage;
        currentPeopleOnSpot += amountPeople;

        if (currentBaggageOnSpot >= startBaggageToSpawn * roundCount &&
            currentPeopleOnSpot >= startPeopleToSpawn * roundCount)
        {
            shopTimeRef.SetActive(true);
            shopTimeRef.GetComponent<ShopTimeAni>().Play();
            IntermissionTime();
        }
    }
    bool intermissionON;
    private async void IntermissionTime()
    {
        currentTimer = intermissionTimer;
        intermissionON = true;
        OnIntermissionCall?.Invoke(true);
        await Task.Delay(intermissionTimer * 1000);
        StartGame();
        intermissionON = false;
        OnIntermissionCall?.Invoke(false);
    }
}

public struct EntityInfo
{
    public int count;
    public ColorType color;
}
