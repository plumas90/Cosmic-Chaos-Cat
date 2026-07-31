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

        [Header("Panels — pre-placed, start inactive")]
        [SerializeField] private GachaPanel         gachaPanel;
        [SerializeField] private EncyclopediaPanel  encyclopediaPanel;
        [SerializeField] private UpgradePanel       upgradePanel;
        [SerializeField] private ShardExchangePanel exchangePanel;
        [SerializeField] private ShopPanel          shopPanel;
        [SerializeField] private CollectionPanel    collectionPanel;

        [Header("Ending Screen — pre-placed, starts inactive")]
        [SerializeField] private GameObject endingScreen;
        [SerializeField] private TMP_Text   endingTimerText;
        [SerializeField] private TMP_Text   endingMessageText;

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

                Debug.Log($"[GameHud] Panels: gacha={gachaPanel!=null}, ency={encyclopediaPanel!=null}, shop={shopPanel!=null}, mgr={gameManager!=null}");

                SetPanelActive(gachaPanel,         false);
                SetPanelActive(encyclopediaPanel,  false);
                SetPanelActive(upgradePanel,       false);
                SetPanelActive(exchangePanel,      false);
                SetPanelActive(shopPanel,          false);
                SetPanelActive(collectionPanel,    false);
                if (endingScreen != null) endingScreen.SetActive(false);

                BuildMenuWindowUI();
                Debug.Log($"[GameHud] Menu settings window built: {menuWindow!=null}");

                if (gachaButton != null) { gachaButton.onClick = new Button.ButtonClickedEvent(); gachaButton.onClick.AddListener(OpenGacha); }
                if (encyclopediaButton != null) { encyclopediaButton.onClick = new Button.ButtonClickedEvent(); encyclopediaButton.onClick.AddListener(ToggleEncyclopedia); }
                if (upgradeButton != null) { upgradeButton.onClick = new Button.ButtonClickedEvent(); upgradeButton.onClick.AddListener(ToggleUpgrade); }
                if (exchangeButton != null) { exchangeButton.onClick = new Button.ButtonClickedEvent(); exchangeButton.onClick.AddListener(ToggleExchange); }
                if (shopButton != null) { shopButton.onClick = new Button.ButtonClickedEvent(); shopButton.onClick.AddListener(ToggleShop); }
                if (collectionButton != null) { collectionButton.onClick = new Button.ButtonClickedEvent(); collectionButton.onClick.AddListener(ToggleCollection); }
                if (menuButton != null) { menuButton.onClick = new Button.ButtonClickedEvent(); menuButton.onClick.AddListener(GoToMenu); }

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
                equippedText.text = card != null
                    ? $"장착: {card.DisplayName}  [{card.Rarity}]"
                    : "장착: 기본";
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

        // ─── Confirm Dialog UI Builder ──────────────────────────────────────
        private GameObject menuWindow;
        private TMP_Text soundStatusText;

        // ─── Settings & Menu Window Builder ─────────────────────────────────
        private void BuildMenuWindowUI()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            // Search for pre-placed MenuSettingsWindow in scene
            if (menuWindow == null && parent != null)
            {
                var existing = parent.Find("MenuSettingsWindow") ?? parent.Find("MenuPanel") ?? parent.Find("MenuWindow");
                if (existing != null) menuWindow = existing.gameObject;
            }

            if (menuWindow != null)
            {
                BindPreplacedMenuWindow(menuWindow);
                return;
            }

            // 1. Root & Dark Overlay Blocker
            menuWindow = new GameObject("MenuSettingsWindow");
            menuWindow.transform.SetParent(parent, false);
            var rt = menuWindow.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            menuWindow.transform.localPosition = Vector3.zero;
            menuWindow.transform.localScale = Vector3.one;

            var blocker = menuWindow.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.65f);

            // 2. Main Dialog Panel
            var panel = new GameObject("MenuPanel");
            panel.transform.SetParent(menuWindow.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(460, 480);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = Vector3.one;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);

            var font = FindObjectOfType<TextMeshProUGUI>()?.font;

            // 3. Title Text
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);
            var trt = titleGO.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 200);
            trt.sizeDelta = new Vector2(400, 45);
            var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            if (font != null) titleTxt.font = font;
            titleTxt.text = "⚙️ 설정 및 메뉴";
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;

            // 4. Sound Control Section
            var soundLabelGO = new GameObject("SoundLabel");
            soundLabelGO.transform.SetParent(panel.transform, false);
            var slrt = soundLabelGO.AddComponent<RectTransform>();
            slrt.anchoredPosition = new Vector2(-110, 135);
            slrt.sizeDelta = new Vector2(180, 35);
            var slTxt = soundLabelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) slTxt.font = font;
            slTxt.text = "🎵 사운드 설정";
            slTxt.fontSize = 16;
            slTxt.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            // Mute / Unmute Button
            MakeMenuButton(panel.transform, "SoundToggle", "🔊 소리 켬", new Vector2(80, 135), new Vector2(140, 38),
                new Color(0.2f, 0.45f, 0.7f, 1f), OnToggleSoundClicked, font, out soundStatusText);

            // 5. Language Control Section
            var langLabelGO = new GameObject("LangLabel");
            langLabelGO.transform.SetParent(panel.transform, false);
            var llrt = langLabelGO.AddComponent<RectTransform>();
            llrt.anchoredPosition = new Vector2(-110, 75);
            llrt.sizeDelta = new Vector2(180, 35);
            var llTxt = langLabelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) llTxt.font = font;
            llTxt.text = "🌐 언어 (Language)";
            llTxt.fontSize = 16;
            llTxt.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            // KR / EN Language Buttons
            MakeMenuButton(panel.transform, "LangKR", "🇰🇷 한국어", new Vector2(25, 75), new Vector2(95, 38),
                new Color(0.18f, 0.52f, 0.28f, 1f), () => OnSelectLanguage("KR"), font, out _);
            MakeMenuButton(panel.transform, "LangEN", "🇺🇸 English", new Vector2(130, 75), new Vector2(95, 38),
                new Color(0.25f, 0.3f, 0.4f, 1f), () => OnSelectLanguage("EN"), font, out _);

            // Divider Line
            var divGO = new GameObject("Divider");
            divGO.transform.SetParent(panel.transform, false);
            var divRT = divGO.AddComponent<RectTransform>();
            divRT.anchoredPosition = new Vector2(0, 30);
            divRT.sizeDelta = new Vector2(400, 2);
            divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            // 6. Action Buttons: Save, Return to Main Menu, Resume
            MakeMenuButton(panel.transform, "SaveBtn", "💾 진행상황 저장하기", new Vector2(0, -25), new Vector2(380, 48),
                new Color(0.18f, 0.52f, 0.28f, 1f), OnSaveClicked, font, out _);

            MakeMenuButton(panel.transform, "MainMenuBtn", "🏠 메인 메뉴로 이동", new Vector2(0, -90), new Vector2(380, 48),
                new Color(0.70f, 0.35f, 0.15f, 1f), OnGoToMainMenuClicked, font, out _);

            MakeMenuButton(panel.transform, "ResumeBtn", "❌ 계속하기 (닫기)", new Vector2(0, -165), new Vector2(380, 48),
                new Color(0.35f, 0.35f, 0.40f, 1f), OnResumeClicked, font, out _);

            menuWindow.SetActive(false);
            BuildConfirmDialog();
        }

        private static void MakeMenuButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bg,
            UnityEngine.Events.UnityAction onClick, TMP_FontAsset font, out TMP_Text labelComponent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one;

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = bg * 1.3f;
            cs.pressedColor = bg * 0.7f;
            btn.colors = cs;
            btn.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            labelGO.transform.localPosition = Vector3.zero;
            labelGO.transform.localScale = Vector3.one;
            var tx = labelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tx.font = font;
            tx.text = label;
            tx.fontSize = 15;
            tx.alignment = TextAlignmentOptions.Center;
            tx.color = Color.white;

            labelComponent = tx;
        }

        private void OnSaveClicked()
        {
            gameManager?.SaveGame();
            OnLog("💾 게임 진행 상황이 안전하게 저장되었습니다.");
            Debug.Log("[GameHud] 게임 진행 상황 수동 저장 완료");
        }

        private void OnGoToMainMenuClicked()
        {
            if (confirmDialog != null)
            {
                confirmDialog.transform.SetAsLastSibling();
                confirmDialog.SetActive(true);
            }
        }

        private void OnResumeClicked()
        {
            if (menuWindow != null) menuWindow.SetActive(false);
        }

        private void BindPreplacedMenuWindow(GameObject go)
        {
            if (go == null) return;
            menuWindow = go;

            var soundToggle = FindChildRecursive(go.transform, "Btn_SoundToggle")?.GetComponent<Button>()
                           ?? FindChildRecursive(go.transform, "SoundToggle")?.GetComponent<Button>();
            if (soundToggle != null)
            {
                soundToggle.onClick.RemoveAllListeners();
                soundToggle.onClick.AddListener(OnToggleSoundClicked);
                soundStatusText = soundToggle.GetComponentInChildren<TMP_Text>();
            }

            var langKR = FindChildRecursive(go.transform, "Btn_LangKR")?.GetComponent<Button>()
                      ?? FindChildRecursive(go.transform, "LangKR")?.GetComponent<Button>();
            if (langKR != null)
            {
                langKR.onClick.RemoveAllListeners();
                langKR.onClick.AddListener(() => OnSelectLanguage("KR"));
            }

            var langEN = FindChildRecursive(go.transform, "Btn_LangEN")?.GetComponent<Button>()
                      ?? FindChildRecursive(go.transform, "LangEN")?.GetComponent<Button>();
            if (langEN != null)
            {
                langEN.onClick.RemoveAllListeners();
                langEN.onClick.AddListener(() => OnSelectLanguage("EN"));
            }

            var saveBtn = FindChildRecursive(go.transform, "Btn_Save")?.GetComponent<Button>()
                       ?? FindChildRecursive(go.transform, "SaveBtn")?.GetComponent<Button>();
            if (saveBtn != null)
            {
                saveBtn.onClick.RemoveAllListeners();
                saveBtn.onClick.AddListener(OnSaveClicked);
            }

            var mainBtn = FindChildRecursive(go.transform, "Btn_MainMenu")?.GetComponent<Button>()
                       ?? FindChildRecursive(go.transform, "MainMenuBtn")?.GetComponent<Button>();
            if (mainBtn != null)
            {
                mainBtn.onClick.RemoveAllListeners();
                mainBtn.onClick.AddListener(OnGoToMainMenuClicked);
            }

            var resumeBtn = FindChildRecursive(go.transform, "Btn_Resume")?.GetComponent<Button>()
                         ?? FindChildRecursive(go.transform, "ResumeBtn")?.GetComponent<Button>();
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
            if (parent.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, targetName);
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
                soundStatusText.text = nextMute ? "🔇 음소거" : "🔊 소리 켬";
            }
            OnLog(nextMute ? "🔇 전체 사운드가 음소거되었습니다." : "🔊 사운드가 활성화되었습니다.");
        }

        private void OnSelectLanguage(string lang)
        {
            if (gameManager == null) return;
            gameManager.SetLanguage(lang);
            OnLog(lang == "KR" ? "🇰🇷 언어가 한국어로 설정되었습니다." : "🇺🇸 Language set to English.");
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

            if (menuWindow == null) BuildMenuWindowUI();
            if (menuWindow != null)
            {
                if (soundStatusText != null && gameManager != null)
                {
                    soundStatusText.text = gameManager.IsMuted ? "🔇 음소거" : "🔊 소리 켬";
                }
                menuWindow.transform.SetAsLastSibling();
                menuWindow.SetActive(!menuWindow.activeSelf);
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
        }
    }
}
