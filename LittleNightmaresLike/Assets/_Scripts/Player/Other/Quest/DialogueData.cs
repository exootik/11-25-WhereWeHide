using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue lignes/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("NPC Name")]
    public string npcName = "????";

    [Header("Dialogue Lines")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    [Header("Dialogue Sound")]
    [Tooltip("Son joué à chaque ligne de dialogue")]
    public AudioClip npcDialogueSound;
}