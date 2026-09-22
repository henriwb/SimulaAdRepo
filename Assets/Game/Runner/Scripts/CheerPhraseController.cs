using System.Collections;
using HyperCasual.Core;
using UnityEngine;
using TMPro;
public class CheerPhraseController : MonoBehaviour, IGameEventListener
{

#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundField("Cheer phrases", 4, "Cheer Phrase Options")]
#endif
    [SerializeField] private string[] phrases = new string[] {"Good!", "Great!", "Super!"};
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundField("Cheer phrase colour", 3, "Cheer Phrase Options")]
#endif
    [SerializeField] private Color cheerPhraseColour;

    private int phraseCount = 0;
    [SerializeField] private TMP_Text cheerText;
    [SerializeField] private Animator cheerTextAnimator;
    [SerializeField] private AbstractGameEvent keyCollectEvent;

    private static readonly string textWobbleTrigger = "Wobble";
    
    private void Start()
    {
        keyCollectEvent.AddListener(this);
        cheerText.color = cheerPhraseColour;
    }

    public void OnEventRaised()
    {
        cheerText.text = phrases[ phraseCount ];
        phraseCount = ( phraseCount + 1 ) % ( phrases.Length );
        cheerTextAnimator.SetTrigger(textWobbleTrigger);
    }
}
