using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent (typeof(AudioSource))]
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private List<Quest> questSequence = new List<Quest>();
    [SerializeField] private AudioClip questCompletedSound;
    private int currentQuestIndex = 0;
    private Quest currentQuest;
    private AudioSource questCompletedAudio;

    public event Action<Quest> OnQuestStarted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest, int, int> OnQuestProgressUpdated;

    private void Awake()
    {
        questCompletedAudio = GetComponent<AudioSource>();
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (questSequence.Count > 0)
        {
            StartQuest(0);
        }
    }

    private void StartQuest(int index)
    {
        if (index >= questSequence.Count) return;

        currentQuestIndex = index;
        currentQuest = questSequence[currentQuestIndex];
        currentQuest.Initialize();
        OnQuestStarted?.Invoke(currentQuest);
        Debug.Log($"quete started : {currentQuest.questTitle}");
    }

    public void UpdateQuestProgress(string questId, int amount = 1)
    {
        if (currentQuest == null || currentQuest.questId != questId || currentQuest.isCompleted) return;

        currentQuest.currentProgress += amount;
        currentQuest.currentProgress = Mathf.Min(currentQuest.currentProgress, currentQuest.targetProgress);

        OnQuestProgressUpdated?.Invoke(currentQuest, currentQuest.currentProgress, currentQuest.targetProgress);
        Debug.Log($"progress de la quête : {currentQuest.currentProgress}/{currentQuest.targetProgress}");

        if (currentQuest.currentProgress >= currentQuest.targetProgress)
        {
            CompleteQuest();
        }
    }

    private void CompleteQuest()
    {
        if (currentQuest == null) return;

        currentQuest.isCompleted = true;
        OnQuestCompleted?.Invoke(currentQuest);
        Debug.Log($"Quête terminée :  {currentQuest.questTitle}");
        if (questCompletedAudio != null) {
            questCompletedAudio.PlayOneShot(questCompletedSound);
        }

        if (currentQuestIndex + 1 < questSequence.Count)
        {
            StartQuest(currentQuestIndex + 1);
        }
        else
        {
            Debug.Log("Quêtes terminés");
        }
    }

    public Quest GetCurrentQuest()
    {
        return currentQuest;
    }
}

[System.Serializable]
public class Quest
{
    public string questId;
    public string questTitle;
    public string questDescription;
    public QuestType questType;
    public int targetProgress = 1;
    [HideInInspector] public int currentProgress = 0;
    [HideInInspector] public bool isCompleted = false;

    public void Initialize()
    {
        currentProgress = 0;
        isCompleted = false;
    }

    public bool ShowProgress()
    {
        return questType == QuestType.CollectMultipleItems ||
               questType == QuestType.CollectItemAndUse;
    }
}

public enum QuestType
{
    ReachTrigger,           // atteindre un trigger
    CollectMultipleItems,   // ramasser plusieurs objets (0/x)
    CollectItemAndUse,      // ramasser objet et l'utiliser (0/2) premiere partie ramasser l'objet deuxieme l'utiliser d'où le 0/2
    TalkToNPC,              // parler à un pnj
    PushObject,             // pousser un objet vers un trigger
    ClimbWall               // grimper à un mur c'est juste atteindre un trigger pour l'instant
}