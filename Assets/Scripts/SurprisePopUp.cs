using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 히든 카드/아이템 획득 시 나타나는 서프라이즈 팝업 UI.
    /// Art, Name, Desc를 업데이트하며, '획득' 버튼(Btn_get)을 누를 때에만 닫을 수 있습니다.
    /// </summary>
    public sealed class SurprisePopUp : MonoBehaviour
    {
        private static SurprisePopUp _instance;

        public static SurprisePopUp Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SurprisePopUp>(true);
                    if (_instance == null)
                    {
                        GameObject targetGo = null;
                        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        if (activeScene.isLoaded)
                        {
                            foreach (var root in activeScene.GetRootGameObjects())
                            {
                                var t = FindRecursive(root.transform, "SurprisePopUp") 
                                     ?? FindRecursive(root.transform, "SurprisePopup") 
                                     ?? FindRecursive(root.transform, "Surprise_PopUp");
                                if (t != null)
                                {
                                    targetGo = t.gameObject;
                                    break;
                                }
                            }
                        }

                        if (targetGo == null)
                        {
                            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                            foreach (var tf in allTransforms)
                            {
                                if (tf.name.Equals("SurprisePopUp", StringComparison.OrdinalIgnoreCase) ||
                                    tf.name.Equals("SurprisePopup", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (tf.gameObject.scene.isLoaded)
                                    {
                                        targetGo = tf.gameObject;
                                        break;
                                    }
                                }
                            }
                        }

                        if (targetGo != null)
                        {
                            _instance = targetGo.GetComponent<SurprisePopUp>() ?? targetGo.AddComponent<SurprisePopUp>();
                        }
                    }
                }
                return _instance;
            }
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        [SerializeField] private Image artImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Button getBtn;

        private Action currentOnConfirm;
        private bool isDisplaying;
        private readonly Queue<PopupRequest> pendingPopups = new Queue<PopupRequest>();

        private struct PopupRequest
        {
            public Sprite Art;
            public string Name;
            public string Desc;
            public Action OnConfirm;
        }

        private void Awake()
        {
            _instance = this;
            AutoWireFields();
        }

        public void AutoWireFields()
        {
            if (artImage == null)
            {
                var t = transform.Find("Art") ?? transform.Find("DetailBox/Art") ?? FindChildByName(transform, "Art");
                if (t != null) artImage = t.GetComponent<Image>();
            }

            if (nameText == null)
            {
                var t = transform.Find("Name") ?? transform.Find("DetailBox/Name") ?? FindChildByName(transform, "Name");
                if (t != null) nameText = t.GetComponent<TMP_Text>();
            }

            if (descText == null)
            {
                var t = transform.Find("Desc") ?? transform.Find("DetailBox/Desc") ?? FindChildByName(transform, "Desc");
                if (t != null) descText = t.GetComponent<TMP_Text>();
            }

            if (getBtn == null)
            {
                var t = transform.Find("Btn_get") 
                     ?? transform.Find("Btn_획득") 
                     ?? transform.Find("Btn_Btn_장착하기") 
                     ?? transform.Find("DetailBox/Btn_get")
                     ?? transform.Find("DetailBox/Btn_Btn_장착하기")
                     ?? FindChildByName(transform, "Btn_get")
                     ?? FindChildByName(transform, "Btn_Btn_장착하기");
                if (t != null) getBtn = t.GetComponent<Button>();
                if (getBtn == null) getBtn = GetComponentInChildren<Button>(true);
            }

            // Ensure button text displays '획득' and mouse hover highlight is dark/black
            if (getBtn != null)
            {
                var txt = getBtn.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) txt.text = "획득";

                var cb = getBtn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 마우스 폰트/버튼 오버 시 검정색 하이라이트
                cb.pressedColor = new Color(0.05f, 0.05f, 0.05f, 1f);
                cb.selectedColor = Color.white;
                cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                cb.colorMultiplier = 3.5f;
                cb.fadeDuration = 0.08f;
                getBtn.colors = cb;
            }

            // Ensure background raycast blocks clicking elements behind the popup
            var bgImg = GetComponent<Image>();
            if (bgImg != null) bgImg.raycastTarget = true;
        }

        public static void ShowCard(CardEntry card, Action onConfirm = null)
        {
            if (card == null) return;
            var popup = Instance;
            if (popup != null)
            {
                popup.DisplayPopup(card.CardSprite, card.GetDisplayName(), card.GetDescription(), () =>
                {
                    var gm = FindObjectOfType<GameManager>(true);
                    if (gm != null)
                    {
                        gm.GrantCard(card.Id);
                        gm.Save();
                        gm.NotifyState();
                        Debug.Log($"[SurprisePopUp] 🎁 히든 카드 [{card.DisplayName}] 획득 완료!");
                    }
                    onConfirm?.Invoke();
                });
            }
            else
            {
                Debug.LogError("[SurprisePopUp] Scene에서 SurprisePopUp 오브젝트를 찾을 수 없습니다!");
            }
        }

        public static void ShowBackground(string bgId, Sprite art, string name, string desc, Action onConfirm = null)
        {
            var popup = Instance;
            if (popup != null)
            {
                popup.DisplayPopup(art, name, desc, () =>
                {
                    var gm = FindObjectOfType<GameManager>(true);
                    if (gm != null)
                    {
                        gm.UnlockBackground(bgId);
                        gm.Save();
                        gm.NotifyState();
                        Debug.Log($"[SurprisePopUp] 🖼️ 히든 배경 [{name}] 해금 완료!");
                    }
                    onConfirm?.Invoke();
                });
            }
            else
            {
                Debug.LogError("[SurprisePopUp] Scene에서 SurprisePopUp 오브젝트를 찾을 수 없습니다!");
            }
        }

        public static void ShowDecoration(string decoId, Sprite art, string name, string desc, Action onConfirm = null)
        {
            var popup = Instance;
            if (popup != null)
            {
                popup.DisplayPopup(art, name, desc, () =>
                {
                    var gm = FindObjectOfType<GameManager>(true);
                    if (gm != null)
                    {
                        gm.UnlockDecoration(decoId);
                        gm.Save();
                        gm.NotifyState();
                        Debug.Log($"[SurprisePopUp] 🎀 히든 장식 [{name}] 해금 완료!");
                    }
                    onConfirm?.Invoke();
                });
            }
            else
            {
                Debug.LogError("[SurprisePopUp] Scene에서 SurprisePopUp 오브젝트를 찾을 수 없습니다!");
            }
        }

        public static void ShowPopup(Sprite art, string name, string desc, Action onConfirm = null)
        {
            var popup = Instance;
            if (popup != null)
            {
                popup.DisplayPopup(art, name, desc, onConfirm);
            }
            else
            {
                Debug.LogError("[SurprisePopUp] Scene에서 SurprisePopUp 오브젝트를 찾을 수 없습니다!");
            }
        }

        public void DisplayPopup(Sprite art, string name, string desc, Action onConfirm = null)
        {
            AutoWireFields();

            // A set can award more than one collectible. Keep every reward popup
            // instead of letting the last DisplayPopup call overwrite the first.
            if (isDisplaying)
            {
                pendingPopups.Enqueue(new PopupRequest
                {
                    Art = art,
                    Name = name,
                    Desc = desc,
                    OnConfirm = onConfirm
                });
                return;
            }

            if (artImage != null)
            {
                artImage.sprite = art;
                artImage.enabled = (art != null);
                artImage.preserveAspect = true;
            }

            if (nameText != null)
            {
                nameText.text = name ?? string.Empty;
            }

            if (descText != null)
            {
                descText.text = desc ?? string.Empty;
            }

            currentOnConfirm = onConfirm;
            isDisplaying = true;

            if (getBtn != null)
            {
                getBtn.onClick.RemoveAllListeners();
                getBtn.onClick.AddListener(OnGetButtonClicked);
            }

            // Activate gameObject and all children (e.g. DetailBox/DetailPopupRoot)
            gameObject.SetActive(true);
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.SetActive(true);
            }

            transform.SetAsLastSibling(); // Bring to front
            Debug.Log($"[SurprisePopUp] 팝업 표시 완료! (Name: {name})");
        }

        private void OnGetButtonClicked()
        {
            gameObject.SetActive(false);
            var cb = currentOnConfirm;
            currentOnConfirm = null;
            isDisplaying = false;
            cb?.Invoke();

            if (pendingPopups.Count > 0)
            {
                var next = pendingPopups.Dequeue();
                DisplayPopup(next.Art, next.Name, next.Desc, next.OnConfirm);
            }
        }

        private Transform FindChildByName(Transform parent, string targetName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            return null;
        }
    }
}
