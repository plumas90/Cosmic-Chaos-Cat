using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 게임 씬 메인 HUD.
    /// 메뉴 버튼은 좌측 상단에 배치. 눌렀을 때 저장 후 메인메뉴 이동 확인 다이얼로그를 코드로 생성.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GameManager       gameManager;
        [SerializeField] private ClickEffectPlayer effectPlayer;

        [Header("HUD Labels")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private GameObject comboImage;
        [SerializeField] private TMP_Text equippedText;
        [SerializeField] private TMP_Text logText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button gachaButton;
        [SerializeField] private Button encyclopediaButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button exchangeButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button collectionButton;

        [Header("Test Cheat Buttons (Auto-Built next to Gacha Button)")]
        [SerializeField] private Button cardFullCollectionButton;
        [SerializeField] private Button resourceCheatButton;

        [Header("HUD Localized Text Fields (Inspector Explicit Bindings)")]
        [SerializeField] private TMP_Text gachaButtonLabel;
        [SerializeField] private TMP_Text encyclopediaButtonLabel;
        [SerializeField] private TMP_Text upgradeButtonLabel;
        [SerializeField] private TMP_Text exchangeButtonLabel;
        [SerializeField] private TMP_Text shopButtonLabel;
        [SerializeField] private TMP_Text collectionButtonLabel;

        [Header("Menu Window Localized Text Fields (Inspector Explicit Bindings)")]
        [SerializeField] private TMP_Text menuTitleLabel;
        [SerializeField] private TMP_Text bgmVolumeLabel;
        [SerializeField] private TMP_Text sfxVolumeLabel;
        [SerializeField] private TMP_Text languageLabel;
        [SerializeField] private TMP_Text saveProgressLabel;
        [SerializeField] private TMP_Text mainMenuLabel;
        [SerializeField] private TMP_Text closeMenuLabel;

        [Header("Panels — pre-placed, start inactive")]
        [SerializeField] private GachaPanel         gachaPanel;
        [SerializeField] private EncyclopediaPanel  encyclopediaPanel;
        [SerializeField] private UpgradePanel       upgradePanel;
        [SerializeField] private ShardExchangePanel exchangePanel;
        [SerializeField] private ShopPanel          shopPanel;
        [SerializeField] private CollectionPanel    collectionPanel;
        [SerializeField] private GameObject         menuWindow;

        [Header("Ending Screen — pre-placed, starts inactive")]
        [SerializeField] private GameObject endingScreen;
        [SerializeField] private TMP_Text   endingTimerText;
        [SerializeField] private TMP_Text   endingMessageText;
        [SerializeField] private Button     endingButton;

        // 확인 다이얼로그 (코드로 생성)
        private GameObject confirmDialog;

        // ─── Style constants ───────────────────────────────────────────────
        private static readonly Color DlgBG     = new Color(0.07f, 0.09f, 0.15f, 0.97f);
        private static readonly Color BtnYes    = new Color(0.18f, 0.52f, 0.28f, 1.00f);
        private static readonly Color BtnNo     = new Color(0.50f, 0.15f, 0.15f, 1.00f);

        // ─── Lifecycle ─────────────────────────────────────────────────────
        private void Awake()
        {
            Debug.Log("[GameHud] Awake started");
            try
            {
                var encys = FindObjectsOfType<EncyclopediaPanel>(true);
                Debug.Log($"[GameHud] Found {encys.Length} EncyclopediaPanels in the scene!");
                for (int i = 0; i < encys.Length; i++)
                {
                    Debug.Log($"[GameHud] Ency {i}: name={encys[i].name}, path={GetGameObjectPath(encys[i].gameObject)}, active={encys[i].gameObject.activeSelf}");
                }

                if (!IsSceneInstance(gachaPanel))        gachaPanel        = FindObjectOfType<GachaPanel>(true);
                if (!IsSceneInstance(encyclopediaPanel)) encyclopediaPanel = FindObjectOfType<EncyclopediaPanel>(true);
                if (!IsSceneInstance(upgradePanel))      upgradePanel      = FindObjectOfType<UpgradePanel>(true);
                if (!IsSceneInstance(exchangePanel))     exchangePanel     = FindObjectOfType<ShardExchangePanel>(true);
                if (!IsSceneInstance(shopPanel))         shopPanel         = FindObjectOfType<ShopPanel>(true);
                if (shopPanel == null)
                {
                    Debug.Log("[GameHud] ShopPanel is missing in the scene. Creating dynamically.");
                    var shopGO = new GameObject("ShopPanel", typeof(RectTransform));
                    shopPanel = shopGO.AddComponent<ShopPanel>();
                }
                if (!IsSceneInstance(gameManager))       gameManager       = FindObjectOfType<GameManager>(true);

                if (collectionPanel == null) collectionPanel = FindObjectOfType<CollectionPanel>(true);
                if (collectionPanel == null)
                {
                    Debug.Log("[GameHud] CollectionPanel is missing in the scene. Creating dynamically.");
                    var colGO = new GameObject("CollectionPanel", typeof(RectTransform));
                    collectionPanel = colGO.AddComponent<CollectionPanel>();
                }

                if (!IsSceneInstance(gachaButton) || !IsSceneInstance(encyclopediaButton) || !IsSceneInstance(upgradeButton) || !IsSceneInstance(exchangeButton) || !IsSceneInstance(menuButton) || !IsSceneInstance(shopButton) || !IsSceneInstance(collectionButton))
                {
                    TryAssignButtons();
                }

                EnsureShopButtonBuilt();
                EnsureCollectionButtonBuilt();
                EnsureCheatButtonsBuilt();

                if (FindObjectOfType<CardListTestButton>(true) == null)
                {
                    var testGO = new GameObject("CardListTestAutoWire", typeof(CardListTestButton));
                }

                Debug.Log($"[GameHud] Panels: gacha={gachaPanel!=null}, ency={encyclopediaPanel!=null}, shop={shopPanel!=null}, mgr={gameManager!=null}");

                SetPanelActive(gachaPanel,         false);
                SetPanelActive(encyclopediaPanel,  false);
                SetPanelActive(upgradePanel,       false);
                SetPanelActive(exchangePanel,      false);
                SetPanelActive(shopPanel,          false);
                SetPanelActive(collectionPanel,    false);
                if (endingScreen != null) endingScreen.SetActive(false);

                EnsureMenuWindowBound();
                Debug.Log($"[GameHud] Menu settings window bound: {menuWindow!=null}");

                if (gachaButton != null) { gachaButton.onClick = new Button.ButtonClickedEvent(); gachaButton.onClick.AddListener(OpenGacha); }
                if (encyclopediaButton != null) { encyclopediaButton.onClick = new Button.ButtonClickedEvent(); encyclopediaButton.onClick.AddListener(ToggleEncyclopedia); }
                if (upgradeButton != null) { upgradeButton.onClick = new Button.ButtonClickedEvent(); upgradeButton.onClick.AddListener(ToggleUpgrade); }
                if (exchangeButton != null) { exchangeButton.onClick = new Button.ButtonClickedEvent(); exchangeButton.onClick.AddListener(ToggleExchange); }
                if (shopButton != null) { shopButton.onClick = new Button.ButtonClickedEvent(); shopButton.onClick.AddListener(ToggleShop); }
                if (collectionButton != null) { collectionButton.onClick = new Button.ButtonClickedEvent(); collectionButton.onClick.AddListener(ToggleCollection); }
                if (menuButton != null) { menuButton.onClick = new Button.ButtonClickedEvent(); menuButton.onClick.AddListener(GoToMenu); }

                BindHudButtonLocalizations();
                Debug.Log($"[GameHud] Buttons bound: gacha={gachaButton!=null}, ency={encyclopediaButton!=null}, shop={shopButton!=null}, menu={menuButton!=null}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameHud] Exception in Awake: {e}");
            }
        }
        private static bool IsSceneInstance(Component comp)
        {
            return comp != null && comp.gameObject != null && comp.gameObject.scene.IsValid();
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "null";
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        private void TryAssignButtons()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string n = btn.name.ToLower();
                if (n.Contains("gacha") || n.Contains("가챠")) gachaButton = btn;
                else if (n.Contains("encyclopedia") || n.Contains("book") || n.Contains("도감")) encyclopediaButton = btn;
                else if (n.Contains("upgrade") || n.Contains("업그레이드")) upgradeButton = btn;
                else if (n.Contains("exchange") || n.Contains("교환")) exchangeButton = btn;
                else if (n.Contains("menu") || n.Contains("메뉴")) menuButton = btn;
                else if (n.Contains("shop") || n.Contains("상점")) shopButton = btn;
                else if (n.Contains("collection") || n.Contains("수집품")) collectionButton = btn;
            }
        }
        private void Start()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
                gameManager.StateChanged += Refresh;
                gameManager.LogUpdated   -= OnLog;
                gameManager.LogUpdated   += OnLog;
                gameManager.CriticalHit  -= OnCriticalHit;
                gameManager.CriticalHit  += OnCriticalHit;
                gameManager.GameEnded    -= OnGameEnded;
                gameManager.GameEnded    += OnGameEnded;
            }
            Refresh();
        }

        private void OnEnable()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager == null) return;
            gameManager.StateChanged += Refresh;
            gameManager.LogUpdated   += OnLog;
            gameManager.CriticalHit  += OnCriticalHit;
            gameManager.GameEnded    += OnGameEnded;
            Refresh();
        }

        private void OnDisable()
        {
            gameManager?.SaveGame();
            if (gameManager == null) return;
            gameManager.StateChanged -= Refresh;
            gameManager.LogUpdated   -= OnLog;
            gameManager.CriticalHit  -= OnCriticalHit;
            gameManager.GameEnded    -= OnGameEnded;
        }

        private void OnApplicationQuit() => gameManager?.SaveGame();
        private void OnApplicationPause(bool pausing) { if (pausing) gameManager?.SaveGame(); }

        private void Update()
        {
            if (gameManager != null && timerText != null)
            {
                timerText.text = gameManager.GetTimerText();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoToMenu();
            }
        }

        // ─── Refresh ───────────────────────────────────────────────────────
        private void Refresh()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager == null) return;

            if (timerText    != null) timerText.text    = gameManager.GetTimerText();
            if (coinText     != null) coinText.text     = GameManager.FormatNumber(gameManager.Money);
            if (shardText    != null) shardText.text    = GameManager.FormatNumber(gameManager.Shards);
            if (progressText != null) progressText.text =
                $"도감  {gameManager.UnlockedCount} / {gameManager.TotalCardCount}" +
                $"  ({gameManager.Completion01 * 100f:0.0}%)";

            Component comboComp = GetComboTextComponent();
            if (comboComp != null)
            {
                bool hasCombo = gameManager.ComboCount > 0;
                string comboStr = hasCombo ? $"{gameManager.ComboCount} Combo!!" : string.Empty;
                if (comboComp is TMP_Text tmp) tmp.text = comboStr;
                else if (comboComp is UnityEngine.UI.Text leg) leg.text = comboStr;

                GameObject comboTargetObj = comboImage != null ? comboImage : (comboComp.transform.parent != null && comboComp.transform.parent.gameObject != gameObject ? comboComp.transform.parent.gameObject : comboComp.gameObject);
                if (comboTargetObj != null && comboTargetObj.activeSelf != hasCombo)
                {
                    comboTargetObj.SetActive(hasCombo);
                }
            }

            var card = gameManager.GetEquippedCard();
            if (equippedText != null)
            {
                string lang = gameManager != null ? gameManager.SelectedLanguage : "KR";
                bool isEN = lang == "EN";
                equippedText.text = card != null
                    ? (isEN ? $"Equipped: {card.GetDisplayName()} [{card.Rarity}]" : $"장착: {card.GetDisplayName()}  [{card.Rarity}]")
                    : (isEN ? "Equipped: Basic" : "장착: 기본");
            }

            // Ending Button visibility (Active ONLY when 100% unlocked and game has NOT ended yet)
            EnsureEndingButtonBuilt();
            if (endingButton != null)
            {
                bool showEndingBtn = gameManager != null && gameManager.Is100PercentUnlocked && !gameManager.IsGameEnded;
                if (endingButton.gameObject.activeSelf != showEndingBtn)
                {
                    endingButton.gameObject.SetActive(showEndingBtn);
                }

                var endingTxt = endingButton.GetComponentInChildren<TMP_Text>(true);
                if (endingTxt != null)
                {
                    string lang = gameManager != null ? gameManager.SelectedLanguage : "KR";
                    endingTxt.text = (lang == "EN") ? "🏆 View Ending" : "🏆 엔딩 보기";
                }
            }
        }

        private void EnsureEndingButtonBuilt()
        {
            if (endingButton != null) return;

            var canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Search for existing ending button in scene if pre-placed
            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.name.IndexOf("ending", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.name.IndexOf("엔딩", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    endingButton = b;
                    break;
                }
            }

            if (endingButton != null)
            {
                endingButton.onClick.RemoveAllListeners();
                endingButton.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                    {
                        gameManager.TriggerEnding();
                    }
                });
            }
        }

        private Component cachedComboComponent;

        public Component GetComboTextComponent()
        {
            if (cachedComboComponent != null && cachedComboComponent.gameObject != null)
                return cachedComboComponent;

            if (comboText != null)
            {
                cachedComboComponent = comboText;
                return cachedComboComponent;
            }

            // 1. Search all TMP_Text in scene (including inactive ones)
            var tmpTexts = FindObjectsOfType<TMPro.TMP_Text>(true);
            foreach (var t in tmpTexts)
            {
                if (t != null && t.name.IndexOf("combo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cachedComboComponent = t;
                    return cachedComboComponent;
                }
            }

            // 2. Search all Legacy Text in scene (including inactive ones)
            var legTexts = FindObjectsOfType<UnityEngine.UI.Text>(true);
            foreach (var t in legTexts)
            {
                if (t != null && t.name.IndexOf("combo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cachedComboComponent = t;
                    return cachedComboComponent;
                }
            }

            // 3. Fallback: Search any GameObject in scene with "combo" in name
            var allGOs = FindObjectsOfType<GameObject>(true);
            foreach (var go in allGOs)
            {
                if (go != null && go.name.IndexOf("combo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var comp = (Component)go.GetComponent<TMPro.TMP_Text>() ?? (Component)go.GetComponent<UnityEngine.UI.Text>();
                    if (comp != null)
                    {
                        cachedComboComponent = comp;
                        return cachedComboComponent;
                    }
                }
            }

            return null;
        }

        private void OnLog(string msg)           { if (logText != null) logText.text = msg; }
        private void OnCriticalHit()             => effectPlayer?.PlayCriticalEffect();

        private void OnGameEnded()
        {
            if (endingScreen     != null) endingScreen.SetActive(true);
            if (endingTimerText  != null) endingTimerText.text  = $"클리어 타임\n{gameManager.GetTimerText()}";
            if (endingMessageText!= null) endingMessageText.text=
                $"총 뽑기 {gameManager.TotalRolls}회\n총 클릭 {gameManager.TotalClicks}번";
        }

        // ─── Menu & Settings Window Binding (Prefab / Scene Object) ──────────────
        private void EnsureMenuWindowBound()
        {
            if (menuWindow == null)
            {
                var canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var existing = canvas.transform.Find("MenuSettingsWindow")
                                ?? canvas.transform.Find("MenuPanel")
                                ?? canvas.transform.Find("MenuWindow");
                    if (existing != null) menuWindow = existing.gameObject;
                }
            }

            if (menuWindow == null)
            {
                var found = GameObject.Find("MenuSettingsWindow")
                         ?? GameObject.Find("MenuPanel")
                         ?? GameObject.Find("MenuWindow");
                if (found != null) menuWindow = found;
            }

            if (menuWindow != null)
            {
                BindPreplacedMenuWindow(menuWindow);
            }
        }

        private void OnSaveClicked()
        {
            gameManager?.SaveGame();
            OnLog("게임 진행 상황이 안전하게 저장되었습니다.");
            Debug.Log("[GameHud] 게임 진행 상황 수동 저장 완료");
        }

        private void OnGoToMainMenuClicked()
        {
            if (gameManager == null) gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>(true);
            gameManager?.SaveGame();
            Debug.Log("[GameHud] 게임 진행 상황 저장 완료. 메인 메뉴로 이동합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }

        private void OnResumeClicked()
        {
            if (menuWindow != null) menuWindow.SetActive(false);
        }

        [SerializeField] private TMP_Dropdown langDropdown;
        [SerializeField] private TMP_Text soundStatusText;

        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmValueText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValueText;

        private RectMask2D bgmFillMask;
        private RectTransform bgmFillAreaRect;
        private RectMask2D sfxFillMask;
        private RectTransform sfxFillAreaRect;

        private void OnBgmSliderChanged(float val)
        {
            if (gameManager != null) gameManager.SetBgmVolume(val);
            if (bgmValueText != null) bgmValueText.text = Mathf.RoundToInt(val * 100f).ToString();
            UpdateSliderMask(bgmFillMask, bgmFillAreaRect, val);
        }

        private void OnSfxSliderChanged(float val)
        {
            if (gameManager != null) gameManager.SetSfxVolume(val);
            if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(val * 100f).ToString();
            UpdateSliderMask(sfxFillMask, sfxFillAreaRect, val);
        }

        private static void UpdateSliderMask(RectMask2D mask, RectTransform fillArea, float val)
        {
            if (mask == null || fillArea == null) return;
            val = Mathf.Clamp01(val);

            float totalWidth = 300f; // 슬라이더 가로폭 최대 300px 기준
            if (fillArea.rect.width > totalWidth) totalWidth = fillArea.rect.width;

            var parentRT = fillArea.parent as RectTransform;
            if (parentRT != null)
            {
                float pWidth = parentRT.rect.width > 0f ? parentRT.rect.width : Mathf.Abs(parentRT.sizeDelta.x);
                if (pWidth > totalWidth) totalWidth = pWidth;
            }

            float rightPadding = totalWidth * (1f - val);
            mask.padding = new Vector4(0f, 0f, rightPadding, 0f);
        }

        private void BindPreplacedMenuWindow(GameObject go)
        {
            if (go == null) return;
            menuWindow = go;

            var bgmSld = FindChildRecursive(go.transform, "Slider_BGM")?.GetComponent<Slider>()
                      ?? FindChildRecursive(go.transform, "BGMSlider")?.GetComponent<Slider>()
                      ?? FindChildWithSubstring(go.transform, "bgm")?.GetComponent<Slider>();
            if (bgmSld != null)
            {
                bgmSlider = bgmSld;
                bgmSlider.fillRect = null;
                bgmSlider.onValueChanged.RemoveAllListeners();
                bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
                bgmValueText = FindChildRecursive(bgmSld.transform, "Text_Value")?.GetComponent<TMP_Text>()
                            ?? FindChildWithSubstring(bgmSld.transform, "val")?.GetComponent<TMP_Text>()
                            ?? bgmSld.GetComponentInChildren<TMP_Text>();

                var fillArea = FindChildRecursive(bgmSld.transform, "Fill Area")
                            ?? FindChildWithSubstring(bgmSld.transform, "fill");
                if (fillArea != null)
                {
                    bgmFillAreaRect = fillArea.GetComponent<RectTransform>();
                    if (bgmFillAreaRect != null)
                    {
                        bgmFillAreaRect.anchorMin = Vector2.zero;
                        bgmFillAreaRect.anchorMax = Vector2.one;
                        bgmFillAreaRect.offsetMin = Vector2.zero;
                        bgmFillAreaRect.offsetMax = Vector2.zero;
                    }
                    bgmFillMask = fillArea.GetComponent<RectMask2D>();
                    if (bgmFillMask == null) bgmFillMask = fillArea.gameObject.AddComponent<RectMask2D>();

                    var fillChild = fillArea.Find("Fill") ?? (fillArea.childCount > 0 ? fillArea.GetChild(0) : null);
                    if (fillChild != null)
                    {
                        var fillRT = fillChild.GetComponent<RectTransform>();
                        if (fillRT != null)
                        {
                            fillRT.anchorMin = Vector2.zero;
                            fillRT.anchorMax = Vector2.one;
                            fillRT.offsetMin = Vector2.zero;
                            fillRT.offsetMax = Vector2.zero;
                        }
                    }
                }
            }

            var sfxSld = FindChildRecursive(go.transform, "Slider_SFX")?.GetComponent<Slider>()
                      ?? FindChildRecursive(go.transform, "SFXSlider")?.GetComponent<Slider>()
                      ?? FindChildWithSubstring(go.transform, "sfx")?.GetComponent<Slider>();
            if (sfxSld != null)
            {
                sfxSlider = sfxSld;
                sfxSlider.fillRect = null;
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
                sfxValueText = FindChildRecursive(sfxSld.transform, "Text_Value")?.GetComponent<TMP_Text>()
                            ?? FindChildWithSubstring(sfxSld.transform, "val")?.GetComponent<TMP_Text>()
                            ?? sfxSld.GetComponentInChildren<TMP_Text>();

                var fillArea = FindChildRecursive(sfxSld.transform, "Fill Area")
                            ?? FindChildWithSubstring(sfxSld.transform, "fill");
                if (fillArea != null)
                {
                    sfxFillAreaRect = fillArea.GetComponent<RectTransform>();
                    if (sfxFillAreaRect != null)
                    {
                        sfxFillAreaRect.anchorMin = Vector2.zero;
                        sfxFillAreaRect.anchorMax = Vector2.one;
                        sfxFillAreaRect.offsetMin = Vector2.zero;
                        sfxFillAreaRect.offsetMax = Vector2.zero;
                    }
                    sfxFillMask = fillArea.GetComponent<RectMask2D>();
                    if (sfxFillMask == null) sfxFillMask = fillArea.gameObject.AddComponent<RectMask2D>();

                    var fillChild = fillArea.Find("Fill") ?? (fillArea.childCount > 0 ? fillArea.GetChild(0) : null);
                    if (fillChild != null)
                    {
                        var fillRT = fillChild.GetComponent<RectTransform>();
                        if (fillRT != null)
                        {
                            fillRT.anchorMin = Vector2.zero;
                            fillRT.anchorMax = Vector2.one;
                            fillRT.offsetMin = Vector2.zero;
                            fillRT.offsetMax = Vector2.zero;
                        }
                    }
                }
            }

            var drop = FindChildRecursive(go.transform, "Dropdown_Language")?.GetComponent<TMP_Dropdown>()
                    ?? FindChildRecursive(go.transform, "LanguageDropdown")?.GetComponent<TMP_Dropdown>()
                    ?? go.GetComponentInChildren<TMP_Dropdown>(true);
            if (drop != null)
            {
                langDropdown = drop;
                langDropdown.onValueChanged.RemoveAllListeners();
                langDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);

                if (langDropdown.captionText != null) langDropdown.captionText.color = Color.white;
                if (langDropdown.itemText != null) langDropdown.itemText.color = Color.white;

                // Ensure Template Canvas & Raycaster exist so popup list renders cleanly on top
                if (langDropdown.template != null)
                {
                    var tmpl = langDropdown.template.gameObject;
                    var cv = tmpl.GetComponent<Canvas>();
                    if (cv == null) cv = tmpl.AddComponent<Canvas>();
                    cv.overrideSorting = true;
                    cv.sortingOrder = 30000;
                    if (tmpl.GetComponent<GraphicRaycaster>() == null) tmpl.AddComponent<GraphicRaycaster>();

                    var viewport = tmpl.transform.Find("Viewport");
                    if (viewport != null)
                    {
                        var oldMask = viewport.GetComponent<Mask>();
                        if (oldMask != null) Destroy(oldMask);
                        if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
                    }
                }
            }

            var saveBtn = FindChildRecursive(go.transform, "Btn_Save")?.GetComponent<Button>()
                       ?? FindChildRecursive(go.transform, "SaveBtn")?.GetComponent<Button>()
                       ?? FindChildWithSubstring(go.transform, "save")?.GetComponent<Button>();
            if (saveBtn != null)
            {
                saveBtn.onClick.RemoveAllListeners();
                saveBtn.onClick.AddListener(OnSaveClicked);
            }

            Button mainBtn = null;
            if (mainMenuLabel != null) mainBtn = mainMenuLabel.GetComponentInParent<Button>();
            if (mainBtn == null)
            {
                mainBtn = FindChildRecursive(go.transform, "Btn_MainMenu")?.GetComponent<Button>()
                       ?? FindChildRecursive(go.transform, "MainMenuBtn")?.GetComponent<Button>()
                       ?? FindChildRecursive(go.transform, "Btn_Main")?.GetComponent<Button>()
                       ?? FindChildWithSubstring(go.transform, "main")?.GetComponent<Button>()
                       ?? FindChildWithSubstring(go.transform, "메인")?.GetComponent<Button>();
            }

            if (mainBtn != null)
            {
                mainBtn.onClick.RemoveAllListeners();
                mainBtn.onClick.AddListener(OnGoToMainMenuClicked);
            }

            var resumeBtn = FindChildRecursive(go.transform, "Btn_Resume")?.GetComponent<Button>()
                         ?? FindChildRecursive(go.transform, "ResumeBtn")?.GetComponent<Button>()
                         ?? FindChildWithSubstring(go.transform, "resume")?.GetComponent<Button>()
                         ?? FindChildWithSubstring(go.transform, "close")?.GetComponent<Button>();
            if (resumeBtn != null)
            {
                resumeBtn.onClick.RemoveAllListeners();
                resumeBtn.onClick.AddListener(OnResumeClicked);
            }

            menuWindow.SetActive(false);
            BuildConfirmDialog();
        }

        private Transform FindChildRecursive(Transform parent, string targetName)
        {
            if (parent == null) return null;
            if (parent.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private Transform FindChildWithSubstring(Transform parent, string sub)
        {
            if (parent == null || string.IsNullOrEmpty(sub)) return null;
            if (parent.name.IndexOf(sub, System.StringComparison.OrdinalIgnoreCase) >= 0) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChildWithSubstring(child, sub);
                if (found != null) return found;
            }
            return null;
        }

        private void OnToggleSoundClicked()
        {
            if (gameManager == null) return;
            bool nextMute = !gameManager.IsMuted;
            gameManager.SetMuted(nextMute);
            AudioListener.pause = nextMute;
            AudioListener.volume = nextMute ? 0f : 1f;

            if (soundStatusText != null)
            {
                soundStatusText.text = nextMute ? "음소거" : "소리 켬";
            }
            OnLog(nextMute ? "전체 사운드가 음소거되었습니다." : "사운드가 활성화되었습니다.");
        }

        private void OnLanguageDropdownChanged(int index)
        {
            string selectedLang = (index == 1) ? "EN" : "KR";
            OnSelectLanguage(selectedLang);
        }

        private void OnSelectLanguage(string lang)
        {
            if (gameManager == null) return;
            gameManager.SetLanguage(lang);
            OnLog(lang == "KR" ? "언어가 한국어로 설정되었습니다." : "Language set to English.");
        }

        private void BuildConfirmDialog()
        {
            // 최상위 캔버스를 찾아서 다이얼로그를 붙임
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            Transform dialogParent = canvas != null ? canvas.transform : transform;

            // 어두운 오버레이
            confirmDialog = new GameObject("MenuConfirmDialog");
            confirmDialog.transform.SetParent(dialogParent, false);
            var rt = confirmDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            confirmDialog.transform.localPosition = Vector3.zero;
            confirmDialog.transform.localScale = Vector3.one;

            // 클릭 블로커
            var blocker = confirmDialog.AddComponent<Image>();
            blocker.color = new Color(0, 0, 0, 0.55f);

            // 다이얼로그 패널
            var panel = new GameObject("DlgPanel");
            panel.transform.SetParent(confirmDialog.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta        = new Vector2(420, 200);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = Vector3.one;
            panel.AddComponent<Image>().color = DlgBG;

            // 폰트 찾기
            var font = FindObjectOfType<TextMeshProUGUI>()?.font;

            // 메시지
            var msgGO = new GameObject("Msg");
            msgGO.transform.SetParent(panel.transform, false);
            var mrt = msgGO.AddComponent<RectTransform>();
            mrt.anchoredPosition = new Vector2(0, 45);
            mrt.sizeDelta        = new Vector2(380, 80);
            msgGO.transform.localPosition = new Vector3(0, 45, 0);
            msgGO.transform.localScale = Vector3.one;
            var msg = msgGO.AddComponent<TextMeshProUGUI>();
            if (font != null) msg.font = font;
            msg.text      = "메인 메뉴로 돌아가시겠습니까?\n현재 게임이 안전하게 저장됩니다.";
            msg.fontSize  = 17;
            msg.color     = Color.white;
            msg.alignment = TextAlignmentOptions.Center;

            // 예 버튼
            MakeDlgButton(panel.transform, "예", new Vector2(-90, -55), BtnYes, OnConfirmYes, font);
            // 아니오 버튼
            MakeDlgButton(panel.transform, "아니오", new Vector2(90, -55), BtnNo, OnConfirmNo, font);

            confirmDialog.SetActive(false);
        }

        private static void MakeDlgButton(Transform parent, string label, Vector2 pos, Color bg,
            UnityEngine.Events.UnityAction onClick, TMP_FontAsset font)
        {
            var go = new GameObject("DlgBtn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(155, 48);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one;

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            var cs  = btn.colors;
            cs.highlightedColor = bg * 1.3f;
            cs.pressedColor     = bg * 0.7f;
            btn.colors          = cs;
            btn.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            labelGO.transform.localPosition = Vector3.zero;
            labelGO.transform.localScale = Vector3.one;
            var tx  = labelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tx.font = font;
            tx.text      = label;
            tx.fontSize  = 16;
            tx.alignment = TextAlignmentOptions.Center;
            tx.color     = Color.white;
        }

        private int lastGachaFrame = -1;
        private int lastEncyFrame = -1;
        private int lastUpgradeFrame = -1;
        private int lastExchangeFrame = -1;
        private int lastShopFrame = -1;
        private int lastMenuFrame = -1;

        public bool IsAnyMajorWindowOpen(MonoBehaviour exceptWindow = null)
        {
            if (shopPanel != null && shopPanel != exceptWindow && shopPanel.gameObject.activeSelf) return true;
            if (collectionPanel != null && collectionPanel != exceptWindow && collectionPanel.gameObject.activeSelf) return true;
            if (encyclopediaPanel != null && encyclopediaPanel != exceptWindow && encyclopediaPanel.gameObject.activeSelf) return true;
            if (gachaPanel != null && gachaPanel != exceptWindow && gachaPanel.gameObject.activeSelf) return true;
            return false;
        }

        // ─── Button Callbacks ──────────────────────────────────────────────
        public void OpenGacha()
        {
            if (Time.frameCount == lastGachaFrame) return;
            lastGachaFrame = Time.frameCount;

            if (gachaPanel == null) return;
            bool targetActive = !gachaPanel.gameObject.activeSelf;
            if (targetActive && IsAnyMajorWindowOpen(gachaPanel))
            {
                Debug.Log("[GameHud] 다른 창이 이미 열려 있어 가챠 창을 열 수 없습니다. 현재 창을 먼저 닫아주세요.");
                return;
            }
            gachaPanel.transform.SetAsLastSibling();
            gachaPanel.gameObject.SetActive(targetActive);
        }

        public void ToggleEncyclopedia()
        {
            if (Time.frameCount == lastEncyFrame) return;
            lastEncyFrame = Time.frameCount;

            if (encyclopediaPanel == null) return;
            bool targetActive = !encyclopediaPanel.gameObject.activeSelf;
            if (targetActive && IsAnyMajorWindowOpen(encyclopediaPanel))
            {
                Debug.Log("[GameHud] 다른 창이 이미 열려 있어 도감 창을 열 수 없습니다. 현재 창을 먼저 닫아주세요.");
                return;
            }
            encyclopediaPanel.transform.SetAsLastSibling();
            encyclopediaPanel.gameObject.SetActive(targetActive);
        }

        public void ToggleUpgrade()
        {
            if (Time.frameCount == lastUpgradeFrame) return;
            lastUpgradeFrame = Time.frameCount;
            Debug.Log("[GameHud] ToggleUpgrade"); Toggle(upgradePanel);
        }

        public void ToggleExchange()
        {
            if (Time.frameCount == lastExchangeFrame) return;
            lastExchangeFrame = Time.frameCount;
            Debug.Log("[GameHud] ToggleExchange"); Toggle(exchangePanel);
        }

        public void ToggleShop()
        {
            if (Time.frameCount == lastShopFrame) return;
            lastShopFrame = Time.frameCount;

            if (shopPanel == null) return;
            bool targetActive = !shopPanel.gameObject.activeSelf;
            if (targetActive && IsAnyMajorWindowOpen(shopPanel))
            {
                Debug.Log("[GameHud] 다른 창이 이미 열려 있어 상점 창을 열 수 없습니다. 현재 창을 먼저 닫아주세요.");
                return;
            }
            shopPanel.transform.SetAsLastSibling();
            shopPanel.gameObject.SetActive(targetActive);
        }
        
        private int lastCollectionFrame;
        public void ToggleCollection()
        {
            if (Time.frameCount == lastCollectionFrame) return;
            lastCollectionFrame = Time.frameCount;

            if (collectionPanel == null) return;
            bool targetActive = !collectionPanel.gameObject.activeSelf;
            if (targetActive && IsAnyMajorWindowOpen(collectionPanel))
            {
                Debug.Log("[GameHud] 다른 창이 이미 열려 있어 수집품 창을 열 수 없습니다. 현재 창을 먼저 닫아주세요.");
                return;
            }
            collectionPanel.transform.SetAsLastSibling();
            collectionPanel.gameObject.SetActive(targetActive);
        }
 
        /// <summary>메뉴 버튼 또는 ESC 키 클릭 → 설정 및 메뉴 창 표시.</summary>
        public void GoToMenu()
        {
            if (Time.frameCount == lastMenuFrame) return;
            lastMenuFrame = Time.frameCount;

            if (menuWindow == null) EnsureMenuWindowBound();
                menuWindow.transform.SetAsLastSibling();
                bool nextActive = !menuWindow.activeSelf;
                menuWindow.SetActive(nextActive);
                if (nextActive)
                {
                    Canvas.ForceUpdateCanvases();
                    if (gameManager != null)
                    {
                        if (bgmSlider != null)
                        {
                            bgmSlider.value = gameManager.BgmVolume;
                            OnBgmSliderChanged(gameManager.BgmVolume);
                        }
                        if (sfxSlider != null)
                        {
                            sfxSlider.value = gameManager.SfxVolume;
                            OnSfxSliderChanged(gameManager.SfxVolume);
                        }
                        if (langDropdown != null)
                        {
                            langDropdown.value = (gameManager.SelectedLanguage == "EN") ? 1 : 0;
                            langDropdown.RefreshShownValue();
                        }
                    }
                }
        }

        private void LogHierarchy(GameObject go, int depth)
        {
            var rt = go.GetComponent<RectTransform>();
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}[GameHud] {go.name}: activeSelf={go.activeSelf}, scale={rt?.localScale}, size={rt?.sizeDelta}, pos3D={rt?.anchoredPosition3D}, path={GetGameObjectPath(go)}");
            foreach (Transform child in go.transform)
            {
                LogHierarchy(child.gameObject, depth + 1);
            }
        }

        private void OnConfirmYes()
        {
            // 저장 후 씬 전환
            gameManager?.SaveGame();
            SceneManager.LoadScene("MainMenuScene");
        }

        private void OnConfirmNo()
        {
            if (confirmDialog != null)
                confirmDialog.SetActive(false);
        }

        // ─── Helpers ───────────────────────────────────────────────────────
        private static void Toggle(MonoBehaviour panel)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(!panel.gameObject.activeSelf);
        }

        private static void SetPanelActive(MonoBehaviour panel, bool active)
        {
            if (panel != null) panel.gameObject.SetActive(active);
        }

        private void EnsureShopButtonBuilt()
        {
            if (shopButton != null) return;

            // Find existing shop button first
            var existing = transform.Find("Btn_상점");
            if (existing == null) existing = transform.Find("ShopButton");
            if (existing != null)
            {
                shopButton = existing.GetComponent<Button>();
                return;
            }

            // Dynamically build below encyclopediaButton
            if (encyclopediaButton == null) return;

            var encyRT = encyclopediaButton.GetComponent<RectTransform>();
            if (encyRT == null) return;

            var parent = encyclopediaButton.transform.parent;
            var go = new GameObject("Btn_상점");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            
            rt.anchorMin = encyRT.anchorMin;
            rt.anchorMax = encyRT.anchorMax;
            rt.pivot = encyRT.pivot;
            rt.sizeDelta = encyRT.sizeDelta;
            
            var layoutGroup = parent.GetComponent<LayoutGroup>();
            if (layoutGroup == null)
            {
                rt.anchoredPosition = encyRT.anchoredPosition + new Vector2(0f, -(encyRT.sizeDelta.y + 15f));
            }
            
            var encyImg = encyclopediaButton.GetComponent<Image>();
            var img = go.AddComponent<Image>();
            if (encyImg != null)
            {
                img.sprite = encyImg.sprite;
                img.type = encyImg.type;
            }
            img.color = new Color(0.18f, 0.62f, 0.30f, 1f); // Beautiful green

            var btn = go.AddComponent<Button>();
            btn.colors = encyclopediaButton.colors;
            shopButton = btn;

            var encyTxt = encyclopediaButton.GetComponentInChildren<TMP_Text>();
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            var tx = labelGO.AddComponent<TextMeshProUGUI>();
            tx.text = "상점";
            tx.alignment = TextAlignmentOptions.Center;
            tx.color = Color.white;
            if (encyTxt != null)
            {
                tx.font = encyTxt.font;
                tx.fontSize = encyTxt.fontSize;
            }
            else
            {
                tx.fontSize = 16;
            }
        }

        private void EnsureCollectionButtonBuilt()
        {
            if (collectionButton != null) return;

            var existing = transform.Find("CollectionButton") ?? transform.Find("Btn_수집품");
            if (existing != null)
            {
                collectionButton = existing.GetComponent<Button>();
                return;
            }

            if (encyclopediaButton == null) return;

            var encyRT = encyclopediaButton.GetComponent<RectTransform>();
            if (encyRT == null) return;

            var parent = encyclopediaButton.transform.parent;
            var go = new GameObject("Btn_수집품");
            go.transform.SetParent(parent, false);
            
            // Set sibling index to place it directly above the encyclopedia button
            go.transform.SetSiblingIndex(encyclopediaButton.transform.GetSiblingIndex());

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = encyRT.anchorMin;
            rt.anchorMax = encyRT.anchorMax;
            rt.pivot = encyRT.pivot;
            rt.sizeDelta = encyRT.sizeDelta;

            var encyImg = encyclopediaButton.GetComponent<Image>();
            var img = go.AddComponent<Image>();
            if (encyImg != null)
            {
                img.sprite = encyImg.sprite;
                img.type = encyImg.type;
            }
            img.color = new Color(0.6f, 0.2f, 0.6f, 1f); // Purple color theme for Collectibles

            var btn = go.AddComponent<Button>();
            btn.colors = encyclopediaButton.colors;
            collectionButton = btn;
            collectionButton.onClick.AddListener(ToggleCollection);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            var tx = labelGO.AddComponent<TextMeshProUGUI>();
            tx.text = "수집품";
            tx.alignment = TextAlignmentOptions.Center;
            tx.color = Color.white;

            var encyTxt = encyclopediaButton.GetComponentInChildren<TMP_Text>();
            if (encyTxt != null)
            {
                tx.font = encyTxt.font;
                tx.fontSize = encyTxt.fontSize;
            }

            var loc = tx.gameObject.AddComponent<LocalizeText>();
            loc.Key = "hud_btn_collection";
        }

        private void EnsureCheatButtonsBuilt()
        {
            GameObject mainGachaGO = GameObject.Find("GachaButton");
            if (mainGachaGO == null)
            {
                var buttons = FindObjectsOfType<Button>(true);
                foreach (var b in buttons)
                {
                    if (b != null && b.name.Equals("GachaButton", System.StringComparison.OrdinalIgnoreCase))
                    {
                        mainGachaGO = b.gameObject;
                        break;
                    }
                }
            }

            if (mainGachaGO == null && gachaButton != null)
            {
                mainGachaGO = gachaButton.gameObject;
            }

            if (mainGachaGO == null) return;

            var gachaRT = mainGachaGO.GetComponent<RectTransform>();
            if (gachaRT == null) return;

            var parent = gachaRT.parent;

            // 게임 메인 폰트 (Galmuri9 SDF) 명시적 로드
            TMP_FontAsset mainFont = null;
#if UNITY_EDITOR
            mainFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Galmuri9 SDF.asset");
#endif
            if (mainFont == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var f in fonts)
                {
                    if (f != null && f.name.Contains("Galmuri")) { mainFont = f; break; }
                }
            }
            if (mainFont == null)
            {
                mainFont = mainGachaGO.GetComponentInChildren<TMP_Text>(true)?.font ?? FindObjectOfType<TextMeshProUGUI>(true)?.font;
            }

            // 1. 메인 씬 카드 풀컬렉션 버튼 ("풀컬렉션")
            if (cardFullCollectionButton == null)
            {
                var existing = parent.Find("Btn_CardFullCollection") ?? transform.Find("Btn_CardFullCollection");
                if (existing != null)
                {
                    cardFullCollectionButton = existing.GetComponent<Button>();
                }
                else
                {
                    var go = new GameObject("Btn_CardFullCollection");
                    go.transform.SetParent(parent, false);
                    var rt = go.AddComponent<RectTransform>();
                    rt.anchorMin = gachaRT.anchorMin;
                    rt.anchorMax = gachaRT.anchorMax;
                    rt.pivot = gachaRT.pivot;
                    rt.sizeDelta = new Vector2(140f, 55f);
                    // 메인 씬 GachaButton 좌측에 정밀 배치
                    rt.anchoredPosition = gachaRT.anchoredPosition + new Vector2(-295f, 0f);

                    var img = go.AddComponent<Image>();
                    var gachaImg = mainGachaGO.GetComponent<Image>();
                    if (gachaImg != null && gachaImg.sprite != null)
                    {
                        img.sprite = gachaImg.sprite;
                        img.type = gachaImg.type;
                    }
                    img.color = new Color(0.55f, 0.25f, 0.85f, 0.95f); // 보라/골드 테마

                    var btn = go.AddComponent<Button>();
                    cardFullCollectionButton = btn;

                    var labelGO = new GameObject("Label");
                    labelGO.transform.SetParent(go.transform, false);
                    var lrt = labelGO.AddComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

                    var tx = labelGO.AddComponent<TextMeshProUGUI>();
                    if (mainFont != null) tx.font = mainFont;
                    tx.text = "풀컬렉션";
                    tx.alignment = TextAlignmentOptions.Center;
                    tx.color = Color.white;
                    tx.fontSize = 18;
                    tx.fontWeight = FontWeight.Bold;
                }
            }
            else
            {
                var tx = cardFullCollectionButton.GetComponentInChildren<TMP_Text>(true);
                if (tx != null && mainFont != null) tx.font = mainFont;
            }

            // 2. 메인 씬 자원 재화 버튼 ("자원재화")
            if (resourceCheatButton == null)
            {
                var existing = parent.Find("Btn_ResourceCheat") ?? transform.Find("Btn_ResourceCheat");
                if (existing != null)
                {
                    resourceCheatButton = existing.GetComponent<Button>();
                }
                else
                {
                    var go = new GameObject("Btn_ResourceCheat");
                    go.transform.SetParent(parent, false);
                    var rt = go.AddComponent<RectTransform>();
                    rt.anchorMin = gachaRT.anchorMin;
                    rt.anchorMax = gachaRT.anchorMax;
                    rt.pivot = gachaRT.pivot;
                    rt.sizeDelta = new Vector2(140f, 55f);
                    // 메인 씬 GachaButton 우측에 정밀 배치
                    rt.anchoredPosition = gachaRT.anchoredPosition + new Vector2(295f, 0f);

                    var img = go.AddComponent<Image>();
                    var gachaImg = mainGachaGO.GetComponent<Image>();
                    if (gachaImg != null && gachaImg.sprite != null)
                    {
                        img.sprite = gachaImg.sprite;
                        img.type = gachaImg.type;
                    }
                    img.color = new Color(0.15f, 0.65f, 0.85f, 0.95f); // 청록/시안 테마

                    var btn = go.AddComponent<Button>();
                    resourceCheatButton = btn;

                    var labelGO = new GameObject("Label");
                    labelGO.transform.SetParent(go.transform, false);
                    var lrt = labelGO.AddComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

                    var tx = labelGO.AddComponent<TextMeshProUGUI>();
                    if (mainFont != null) tx.font = mainFont;
                    tx.text = "자원재화";
                    tx.alignment = TextAlignmentOptions.Center;
                    tx.color = Color.white;
                    tx.fontSize = 18;
                    tx.fontWeight = FontWeight.Bold;
                }
            }
            else
            {
                var tx = resourceCheatButton.GetComponentInChildren<TMP_Text>(true);
                if (tx != null && mainFont != null) tx.font = mainFont;
            }

            // Click Handlers
            if (cardFullCollectionButton != null)
            {
                cardFullCollectionButton.onClick.RemoveAllListeners();
                cardFullCollectionButton.onClick.AddListener(OnCardFullCollectionClicked);
            }
            if (resourceCheatButton != null)
            {
                resourceCheatButton.onClick.RemoveAllListeners();
                resourceCheatButton.onClick.AddListener(OnResourceCheatClicked);
            }
        }

        private void OnCardFullCollectionClicked()
        {
            if (gameManager == null) gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>(true);
            gameManager?.GrantCardFullCollection();
            OnLog("모든 카드 5장 풀컬렉션 지급 완료!");
        }

        private void OnResourceCheatClicked()
        {
            if (gameManager == null) gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>(true);
            gameManager?.GrantResources100k();
            OnLog("골드 +100k, 조각 +100k 재화 지급 완료!");
        }

        private void BindHudButtonLocalizations()
        {
            void BindField(TMP_Text txt, string key)
            {
                if (txt == null) return;
                var loc = txt.GetComponent<LocalizeText>() ?? txt.gameObject.AddComponent<LocalizeText>();
                loc.Key = key;
                loc.Refresh();
            }

            void BindBtn(Button btn, TMP_Text explicitTxt, string key)
            {
                if (explicitTxt != null)
                {
                    BindField(explicitTxt, key);
                    return;
                }
                if (btn == null) return;
                var txts = btn.GetComponentsInChildren<TMP_Text>(true);
                if (txts != null && txts.Length > 0)
                {
                    foreach (var txt in txts) BindField(txt, key);
                }
                var legacyTxts = btn.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                if (legacyTxts != null && legacyTxts.Length > 0)
                {
                    foreach (var legacyTxt in legacyTxts) legacyTxt.text = LocalizationManager.Get(key);
                }
            }

            if (encyclopediaButton == null)
            {
                var encyTF = transform.Find("EncyclopediaButton") ?? FindChildWithSubstring(transform, "encyclopedia");
                if (encyTF != null) encyclopediaButton = encyTF.GetComponent<Button>();
            }

            BindBtn(gachaButton, gachaButtonLabel, "hud_btn_gacha");
            BindBtn(encyclopediaButton, encyclopediaButtonLabel, "hud_btn_encyclopedia");
            BindBtn(upgradeButton, upgradeButtonLabel, "hud_btn_upgrade");
            BindBtn(exchangeButton, exchangeButtonLabel, "hud_btn_exchange");
            BindBtn(shopButton, shopButtonLabel, "hud_btn_shop");
            BindBtn(collectionButton, collectionButtonLabel, "hud_btn_collection");

            BindField(menuTitleLabel, "menu_title");
            BindField(bgmVolumeLabel, "menu_bgm_vol");
            BindField(sfxVolumeLabel, "menu_sfx_vol");
            BindField(languageLabel, "menu_lang_label");
            BindField(saveProgressLabel, "menu_btn_save");
            BindField(mainMenuLabel, "menu_btn_main_menu");
            BindField(closeMenuLabel, "menu_btn_close");
        }
    }
}
