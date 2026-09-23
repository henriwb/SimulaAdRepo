using System;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteInEditMode]
public class EndCardController : MonoBehaviour
{
    [Header("Used in Playground to view the end card at start")]
    [SerializeField]
    public bool alwaysShowEndcardTESTING;

    [Header("zEndCard Settings")] [SerializeField]
    private Color _backgroundColor = Color.white;
    
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundAsset( "background texture", 0, "end card" )]
#endif
    [SerializeField]
    private Texture _backgroundTexture;
    
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundAsset( "icon texture", 1, "end card" )]
#endif
    [SerializeField]
    private Texture _iconTexture;
    
    [SerializeField]
    private Color _iconColor = Color.white;
    
    [SerializeField]
    private Texture _buttonTexture;
    
    [SerializeField]
    private Color _buttonColor = Color.white;
    
    [SerializeField] [TextArea(3, 5)]
    private string _endCardDescriptionText = "An awesome description\nof my awesome game!";
    
    [SerializeField] [TextArea(1, 2)]
    private string _CTAButtonText = "Download\nNow!";
    
    [SerializeField]
    private bool _useBestFitOnCTA;
    
    [SerializeField]
    private Color _CTAFontColor = Color.white;
    
    [SerializeField]
    private Color _descriptionFontColor = Color.white;
    
    [SerializeField]
    private Font _CTAFont;
    
    [SerializeField]
    private Font _descriptionFont;

    [Header("zEndCard Settings Portrait")]
    [SerializeField]
    [Range(10, 100)]
    private int _CTAFontSize = 10;
    
    [SerializeField]
    [Range(10, 100)]
    private int _descriptionFontSize = 10;
    
    [SerializeField]
    private bool _centerDescriptionX = false;
    
    [SerializeField]
    private bool _centerDescriptionY = false;
    
    [SerializeField]
    private bool _centerButtonX = false;
    
    [SerializeField]
    private bool _centerButtonY = false;
    
    [SerializeField]
    [Range(0, 1)]
    private float _descriptionPositionX = 0.5f;
    
    [SerializeField]
    [Range(0, 1)]
    private float _descriptionPositionY = 0.5f;
    
    [SerializeField]
    [Range(0, 1)]
    private float _buttonPositionX = 0.5f;
    
    [SerializeField]
    [Range(0, 1)]
    private float _buttonPositionY = 0.5f;

    [Header("zEndCard Settings Landscape")]
    [SerializeField]
    [Range(10, 100)]
    private int _CTAFontSizeLandscape = 10;
    
    [SerializeField]
    [Range(10, 100)]
    private int _descriptionFontSizeLandscape = 10;
    
    [SerializeField]
    private bool _centerDescriptionXLandscape = false;
    
    [SerializeField]
    private bool _centerDescriptionYLandscape = false;
    
    [SerializeField]
    private bool _centerButtonXLandscape = false;
    
    [SerializeField]
    private bool _centerButtonYLandscape = false;
    
    [SerializeField]
    [Range(0, 1)]
    private float _descriptionPositionXLandscape = 0.5f;

    [SerializeField]
    [Range(0, 1)]
    private float _descriptionPositionYLandscape = 0.5f;
    
    [SerializeField]
    [Range(0, 1)]
    private float _buttonPositionXLandscape = 0.5f;

    [SerializeField]
    [Range(0, 1)]
    private float _buttonPositionYLandscape = 0.5f;


    [Header("Icon Settings Portrait")]
    [SerializeField]
    private bool _centerIconX = false;
    
    [SerializeField]
    private bool _centerIconY = false;
    
    [SerializeField]
    [Range(0, 1)]
    private float _iconPositionX = 0.5f;
    
    [SerializeField]
    [Range(0, 1)]
    private float _iconPositionY = 0.5f;
    
    [SerializeField]
    [Range(0.1f, 1)]
    private float _iconSize = 0.5f;
    
    [SerializeField]
    private bool _roundEdgesOnIcon = false;

    [Header("Icon Settings Landscape")]
    [SerializeField]
    private bool _centerIconXLandscape = false;

    [SerializeField]
    private bool _centerIconYLandscape = false;
    
    [SerializeField]
    [Range(0, 1)]
    private float _iconPositionXLandscape = 0.5f;
    
    [SerializeField]
    [Range(0, 1)]
    private float _iconPositionYLandscape = 0.5f;
    
    [SerializeField]
    [Range(0.1f, 1)]
    private float _iconSizeLandscape = 0.5f;
    
    [SerializeField]
    private bool _roundEdgesOnIconLandscape = false;
    
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
[LunaPlaygroundField("Clickable Option", 3, "Endcard Clickable Options")] 
#endif
    [Header("Clickable options")] [SerializeField]
    private EndCardClickableOptionsType _endCardClickableOptions = EndCardClickableOptionsType.CTAButtonOnly;

    [Header("zEndCard Animation Settings")]
    [SerializeField]
    private ButtonAnimationType _buttonAnimationType;
    
    [SerializeField]
    private EndCardPortraitAnimationType _endCardPortraitAnimationType;
    
    [SerializeField]
    private EndCardLandscapeAnimationType _endCardLandscapeAnimationType;
    
    [Header("Drag and drops")]
    [SerializeField]
    private RawImage _backgroundImage;

    [SerializeField] private Button _CTAButton;
    [SerializeField] private Button _ScreenCTAButton;
    [SerializeField] private Text _CTAButtonTextText;
    [SerializeField] private Text _EndCardDescriptionTextText;
    [SerializeField] private Mask _maskIcon;
    [SerializeField] private RectTransform _iconRect;
    [SerializeField] private RectTransform _iconRectMask;
    [SerializeField] private RectTransform _descriptionRect;
    [SerializeField] private RectTransform _buttonRect;
    [SerializeField] private Animator _CTAButtonAnimator;
    [SerializeField] private Animator _endCardAnimator;
    [SerializeField] private RawImage _iconImage;
    [SerializeField] private RawImage _CTAButtonImage;
    [Tooltip("Shown when the CTA is clicked (demo build: no store redirect).")]
    [SerializeField] private GameObject _ctaConfirmation;

    private int _CTAButtonAnimationScale;
    private int _CTAButtonAnimationWobble;
    private int _CTAButtonAnimationOpacity;

    private int _endCardAnimationScale;
    private int _endCardAnimationLeftToRight;
    private int _endCardAnimationRightToLeft;
    private int _endCardAnimationBottomToTop;
    private int _endCardAnimationTopToBottom;


    public UnityEvent EndCardOpenedEvent = new UnityEvent();

    //-------------
    private float _iconSizeScaler = 500;
    //-------------

    public static EndCardController Instance;

    private ScreenOrientationType _getCurrentScreenOrientationType 
    {
        get
        {
            return Screen.width < Screen.height ? ScreenOrientationType.Portrait : ScreenOrientationType.Landscape;
        }
    }

#if !UNITY_EDITOR
    private ScreenOrientationType _currentScreenType = ScreenOrientationType.Undefined;
#endif

    void Awake()
    {
        CreateInstance();
        _ScreenCTAButton.gameObject.SetActive(false);

        // Register once: OpenEndCard runs on every replay and must not stack listeners.
        _CTAButton.onClick.AddListener(ClickCTA);
        _ScreenCTAButton.onClick.AddListener(ClickCTA);
        if (_ctaConfirmation != null)
            _ctaConfirmation.SetActive(false);

        _CTAButtonAnimationScale = Animator.StringToHash("Scale");
        _CTAButtonAnimationWobble = Animator.StringToHash("Wobble");
        _CTAButtonAnimationOpacity = Animator.StringToHash("Opacity");

        _endCardAnimationScale = Animator.StringToHash("Scale");
        _endCardAnimationLeftToRight = Animator.StringToHash("LeftToRight");
        _endCardAnimationRightToLeft = Animator.StringToHash("RightToLeft");
        _endCardAnimationBottomToTop = Animator.StringToHash("BottomToTop");
        _endCardAnimationTopToBottom = Animator.StringToHash("TopToBottom");

        //TESTING:
        if (alwaysShowEndcardTESTING)
        {
            _endCardAnimator.SetBool("TEST", true);
        }
    }

    void SetClickableOptions()
    {
        _ScreenCTAButton.gameObject.SetActive(false); //Always turn off
        EndCardOpenedEvent.RemoveListener(ClickCTA);
        switch (_endCardClickableOptions)
        {
            case EndCardClickableOptionsType.CTAButtonOnly:
                // CTA button listener is registered once in Awake
                break;
            case EndCardClickableOptionsType.Fullscreen:
                _ScreenCTAButton.gameObject.SetActive(true);
                break;
            case EndCardClickableOptionsType.FireAppStoreClickOnShow:
                EndCardOpenedEvent.AddListener(ClickCTA);
                break;
        }
    }

    void CreateInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"There are 2 instances of EndCardController.cs on the {name} object. Destroying...");
            Destroy(this);
        }
    }

    public void OpenEndCard()
    {
        SetClickableOptions();
        EndCardOpenedEvent.Invoke();

        SetButtonAnimation();
        SetEndCardAnimationType();
    }

    /// <summary>
    /// Hides the end card and returns its animators to their initial state, so the next
    /// OpenEndCard (after a replay) animates in again.
    /// </summary>
    public void CloseEndCard()
    {
        _ScreenCTAButton.gameObject.SetActive(false);
        if (_ctaConfirmation != null)
            _ctaConfirmation.SetActive(false);

        _endCardAnimator.Rebind();
        _endCardAnimator.Update(0f);
        _CTAButtonAnimator.Rebind();
        _CTAButtonAnimator.Update(0f);
    }

    void SetCTAButtonPosition()
    {
        var buttonPositionX = IsPortrait() ? _buttonPositionX : _buttonPositionXLandscape;
        var buttonPositionY = IsPortrait() ? _buttonPositionXLandscape : _buttonPositionYLandscape;

        if (IsPortrait())
        {
            buttonPositionX = IsPortrait() && !_centerButtonX ? _buttonPositionX : 0.5f;
            buttonPositionY = IsPortrait() && !_centerButtonY ? _buttonPositionY : 0.5f;
        }
        else
        {
            buttonPositionX = !IsPortrait() && !_centerButtonXLandscape ? _buttonPositionXLandscape : 0.5f;
            buttonPositionY = !IsPortrait() && !_centerButtonYLandscape ? _buttonPositionYLandscape : 0.5f;
        }

        _buttonRect.anchorMin = new Vector2(buttonPositionX, buttonPositionY);
        _buttonRect.anchorMax = new Vector2(buttonPositionX, buttonPositionY);
        _buttonRect.pivot = new Vector2(buttonPositionX, buttonPositionY);
    }

    // Demo build: confirm locally instead of Luna.Unity.Playable.InstallFullGame() (no navigation).
    public void ClickCTA()
    {
        Debug.Log("CTA clicked — demo only");
        if (_ctaConfirmation != null)
            _ctaConfirmation.SetActive(true);
    }

    void Start()
    {
        SetBackgroundImage();
        SetBackgroundColor();
        SetText();
        SetMask();
        SetFontSize();
        SetIconSize();
        SetPositionIcon();
        SetFontColor();
        SetPositionDescriptionText();
        SetFont();
        SetCTAButtonPosition();
        SetBestFitCTA();
        SetIconImage();
        SetIconColor();
        SetCTAButtonImage();
        SetCTAButtonColor();
    }

    private void SetIconImage()
    {
        if (_iconImage == null) return;
        _iconImage.texture = _iconTexture;
    }


    void Update()
    {
#if UNITY_EDITOR
        SetBackgroundImage();
        SetBackgroundColor();
        SetText();
        SetMask();
        SetFontSize();
        SetIconSize();
        SetPositionIcon();
        SetFontColor();
        SetPositionDescriptionText();
        SetFont();
        SetCTAButtonPosition();
        SetBestFitCTA();
        SetIconImage();
        SetIconColor();
        SetCTAButtonImage();
        SetCTAButtonColor();
#endif
#if !UNITY_EDITOR
    if (_currentScreenType != _getCurrentScreenOrientationType)
        {
            _currentScreenType = _getCurrentScreenOrientationType;
            SetCTAButtonPosition();
            SetFontSize();
            SetPositionIcon();
            SetPositionDescriptionText();
        }
#endif
    }
    

    void SetIconColor()
    {
        if (_iconImage == null) return;
        _iconImage.color = _iconColor;
    }

    private void SetBestFitCTA()
    {
        _CTAButtonTextText.resizeTextForBestFit = _useBestFitOnCTA;
    }

    void SetFont()
    {
        if (_CTAFont != null)
        {
            _CTAButtonTextText.font = _CTAFont;
        }

        if (_descriptionFont != null)
        {
            _EndCardDescriptionTextText.font = _descriptionFont;
        }
    }

    void SetFontColor()
    {
        _CTAButtonTextText.color = _CTAFontColor;
        _EndCardDescriptionTextText.color = _descriptionFontColor;
    }

    void SetCTAButtonImage()
    {
        if (_CTAButtonImage == null) return;
        _CTAButtonImage.texture = _buttonTexture;
    }

    void SetCTAButtonColor()
    {
        if (_CTAButtonImage == null) return;
        _CTAButtonImage.color = _buttonColor;
    }

    void SetButtonAnimation()
    {
        switch (_buttonAnimationType)
        {
            case ButtonAnimationType.None:
                break;
            case ButtonAnimationType.Opacity:
                _CTAButtonAnimator.SetBool(_CTAButtonAnimationOpacity, true);
                break;
            case ButtonAnimationType.Wobble:
                _CTAButtonAnimator.SetBool(_CTAButtonAnimationWobble, true);
                break;
            case ButtonAnimationType.Scale:
                _CTAButtonAnimator.SetBool(_CTAButtonAnimationScale, true);
                break;
        }
    }

    void SetEndCardAnimationType()
    {
        if (!alwaysShowEndcardTESTING)
        {
            if (IsPortrait())
            {
                switch (_endCardPortraitAnimationType)
                {
                    case EndCardPortraitAnimationType.None:
                        break;
                    case EndCardPortraitAnimationType.Scale:
                        _endCardAnimator.SetBool(_endCardAnimationScale, true);
                        break;
                    case EndCardPortraitAnimationType.SlideBottomToTop:
                        _endCardAnimator.SetBool(_endCardAnimationBottomToTop, true);
                        break;
                    case EndCardPortraitAnimationType.SlideTopToBottom:
                        _endCardAnimator.SetBool(_endCardAnimationTopToBottom, true);
                        break;
                }
            }
            else
            {
                switch (_endCardLandscapeAnimationType)
                {
                    case EndCardLandscapeAnimationType.None:
                        break;
                    case EndCardLandscapeAnimationType.Scale:
                        _endCardAnimator.SetBool(_endCardAnimationScale, true);
                        break;
                    case EndCardLandscapeAnimationType.SlideLeftToRight:
                        _endCardAnimator.SetBool(_endCardAnimationLeftToRight, true);
                        break;
                    case EndCardLandscapeAnimationType.SlideRightToLeft:
                        _endCardAnimator.SetBool(_endCardAnimationRightToLeft, true);
                        break;
                }
            }
        }
    }

    void SetPositionIcon()
    {
        var iconPosX = IsPortrait() ? _iconPositionX : _iconPositionXLandscape;
        var iconPosY = IsPortrait() ? _iconPositionY : _iconPositionYLandscape;

        if (IsPortrait())
        {
            iconPosX = IsPortrait() && !_centerIconX ? _iconPositionX : 0.5f;
            iconPosY = IsPortrait() && !_centerIconY ? _iconPositionY : 0.5f;
        }
        else
        {
            iconPosX = !IsPortrait() && !_centerIconXLandscape ? _iconPositionXLandscape : 0.5f;
            iconPosY = !IsPortrait() && !_centerIconYLandscape ? _iconPositionYLandscape : 0.5f;
        }

        _iconRectMask.anchorMin = new Vector2(iconPosX, iconPosY);
        _iconRectMask.anchorMax = new Vector2(iconPosX, iconPosY);
        _iconRectMask.pivot = new Vector2(iconPosX, iconPosY);
    }

    void SetPositionDescriptionText()
    {
        var descriptionX = IsPortrait() ? _descriptionPositionX : _descriptionPositionXLandscape;
        var descriptionY = IsPortrait() ? _descriptionPositionY : _descriptionPositionYLandscape;

        if (IsPortrait())
        {
            descriptionX = IsPortrait() && !_centerDescriptionX ? _descriptionPositionX : 0.5f;
            descriptionY = IsPortrait() && !_centerDescriptionY ? _descriptionPositionY : 0.5f;
        }
        else
        {
            descriptionX = !IsPortrait() && !_centerDescriptionXLandscape ? _descriptionPositionXLandscape : 0.5f;
            descriptionY = !IsPortrait() && !_centerDescriptionYLandscape ? _descriptionPositionYLandscape : 0.5f;

            descriptionX = Mathf.Abs(descriptionX + -1); //Fix to go left to right
        }

        _descriptionRect.anchorMin = new Vector2(descriptionX, descriptionY);
        _descriptionRect.anchorMax = new Vector2(descriptionX, descriptionY);
        _descriptionRect.pivot = new Vector2(descriptionX, descriptionY);
    }

    void SetIconSize()
    {
        var ratio = 1f;

        var size = IsPortrait() ? _iconSize : _iconSizeLandscape;

        _iconRect.sizeDelta = new Vector2(size * _iconSizeScaler * ratio, size * _iconSizeScaler);
        _iconRectMask.sizeDelta = new Vector2(size * _iconSizeScaler * ratio, size * _iconSizeScaler);
    }

    void SetFontSize()
    {
        _CTAButtonTextText.fontSize = IsPortrait() ? _CTAFontSize : _CTAFontSizeLandscape;
        _EndCardDescriptionTextText.fontSize = IsPortrait() ? _descriptionFontSize : _descriptionFontSizeLandscape;
    }

    void SetMask()
    {
        bool enableEdge = IsPortrait() ? _roundEdgesOnIcon : _roundEdgesOnIconLandscape;
        _maskIcon.enabled = enableEdge;
    }

    void SetText()
    {
        _CTAButtonTextText.text = _CTAButtonText;
        _EndCardDescriptionTextText.text = _endCardDescriptionText;
    }

    void SetBackgroundImage()
    {
        if (_backgroundTexture == null) return;
        _backgroundImage.texture = _backgroundTexture;
    }

    void SetBackgroundColor()
    {
        _backgroundImage.color = _backgroundColor;
    }

    bool IsPortrait()
    {
        if (Screen.width < Screen.height)
            return true;
        else
            return false;
    }
}