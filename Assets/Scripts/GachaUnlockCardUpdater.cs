using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    public class GachaUnlockCardUpdater : MonoBehaviour
    {
        public GachaType GachaType;
        public Button    BuyButton;
        public TMP_Text  BtnText;
        public Color     LockedColor;
        public Color     UnlockedColor;

        public void Refresh(GameManager gm)
        {
            if (gm == null || BuyButton == null) return;
            bool unlocked = GachaType == GachaType.Rare ? gm.UnlockedRareGacha : gm.UnlockedSuperGacha;
            double cost   = GachaType == GachaType.Rare ? 5000 : 20000;
            if (unlocked)
            {
                BuyButton.interactable = false;
                if (BtnText != null) BtnText.text = "해금됨";
                BuyButton.GetComponent<Image>().color = UnlockedColor;
            }
            else
            {
                bool afford = gm.Money >= cost;
                BuyButton.interactable = afford;
                if (BtnText != null) BtnText.text = afford ? "해금하기" : "골드 부족";
                BuyButton.GetComponent<Image>().color = afford ? LockedColor : new Color(0.22f,0.25f,0.30f);
            }
        }
    }
}
