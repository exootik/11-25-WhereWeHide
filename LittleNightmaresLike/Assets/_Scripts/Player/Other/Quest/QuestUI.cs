using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI questProgressText;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float delayBeforeHide = 2f;

    private CanvasGroup canvasGroup;
    private bool isSubscribed = false;

    private void Awake()
    {
        canvasGroup = questPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = questPanel.AddComponent<CanvasGroup>();
        }

        HideQuestPanel();
    }

    private void Start()
    {
        SubscribeToQuestEvents();
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null && !isSubscribed)
        {
            SubscribeToQuestEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromQuestEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromQuestEvents();
    }

    private void SubscribeToQuestEvents()
    {
        if (QuestManager.Instance != null && !isSubscribed)
        {
            QuestManager.Instance.OnQuestStarted += ShowNewQuest;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestManager.Instance.OnQuestProgressUpdated += UpdateQuestProgress;
            isSubscribed = true;
        }
        else if (QuestManager.Instance == null)
        {
            Debug.LogWarning("l'instance du questManager est null");
        }
    }

    private void UnsubscribeFromQuestEvents()
    {
        if (QuestManager.Instance != null && isSubscribed)
        {
            QuestManager.Instance.OnQuestStarted -= ShowNewQuest;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestManager.Instance.OnQuestProgressUpdated -= UpdateQuestProgress;
            isSubscribed = false;
        }
    }

    private void ShowNewQuest(Quest quest)
    {
        StopAllCoroutines();

        questTitleText.text = $"<b><color=#FFD700>{quest.questTitle}</color></b>"; //jaune (test en code je trouvais ça cool)
        questDescriptionText.text = quest.questDescription; //changer la couleur si besoin 

        if (quest.ShowProgress())
        {
            questProgressText.gameObject.SetActive(true);
            questProgressText.text = $"<color=#00FF00>{quest.currentProgress}</color>/<color=#FFFFFF>{quest.targetProgress}</color>"; //vert et blanc
        }
        else
        {
            questProgressText.gameObject.SetActive(false);
        }

        ShowQuestPanel();
    }

    private void UpdateQuestProgress(Quest quest, int current, int target)
    {
        if (quest.ShowProgress())
        {
            questProgressText.text = $"<color=#00FF00>{current}</color>/<color=#FFFFFF>{target}</color>";
        }
    }

    private void OnQuestCompleted(Quest quest)
    {
        if (quest.ShowProgress())
        {
            questProgressText.text = $"<color=#00FF00>{quest.targetProgress}</color>/<color=#FFFFFF>{quest.targetProgress}</color>";
        }

        Quest nextQuest = QuestManager.Instance.GetCurrentQuest();
        if (nextQuest == null || nextQuest.isCompleted)
        {
            StartCoroutine(HideQuestPanelWithDelay());
        }
    }

    private void ShowQuestPanel()
    {
        questPanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
    }

    private void HideQuestPanel()
    {
        StopAllCoroutines();
        StartCoroutine(FadePanel(1f, 0f, fadeOutDuration));
    }

    private System.Collections.IEnumerator HideQuestPanelWithDelay()
    {
        yield return new WaitForSeconds(delayBeforeHide);
        yield return StartCoroutine(FadePanel(1f, 0f, fadeOutDuration));

        questPanel.SetActive(false);
        Debug.Log("panel de quête desactivé toutes les quetes sont terminés");
    }

    private System.Collections.IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;

        if (endAlpha == 0f)
        {
            questPanel.SetActive(false);
        }
    }
}