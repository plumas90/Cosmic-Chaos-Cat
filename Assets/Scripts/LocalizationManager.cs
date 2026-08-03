using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    /// <summary>
    /// UI static & dynamic text localization manager.
    /// Supports KR (Korean) and EN (English), easily expandable to 5+ languages.
    /// </summary>
    public static class LocalizationManager
    {
        public static event Action OnLanguageChanged;

        private static readonly Dictionary<string, Dictionary<string, string>> Table = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Settings & Menu Window ---
            { "menu_title", new Dictionary<string, string> { { "KR", "설정 및 메뉴" }, { "EN", "Settings & Menu" } } },
            { "menu_bgm_vol", new Dictionary<string, string> { { "KR", "BGM 볼륨" }, { "EN", "BGM Volume" } } },
            { "menu_sfx_vol", new Dictionary<string, string> { { "KR", "SFX 볼륨" }, { "EN", "SFX Volume" } } },
            { "menu_lang_label", new Dictionary<string, string> { { "KR", "언어 (Language)" }, { "EN", "Language" } } },
            { "menu_btn_save", new Dictionary<string, string> { { "KR", "진행상황 저장" }, { "EN", "Save Progress" } } },
            { "menu_btn_main_menu", new Dictionary<string, string> { { "KR", "메인 메뉴로 이동" }, { "EN", "Main Menu" } } },
            { "menu_btn_close", new Dictionary<string, string> { { "KR", "닫기" }, { "EN", "Close" } } },

            // --- Confirm Dialogs & Popups ---
            { "dialog_confirm_save_title", new Dictionary<string, string> { { "KR", "저장 완료" }, { "EN", "Save Complete" } } },
            { "dialog_confirm_save_msg", new Dictionary<string, string> { { "KR", "게임 진행 상황이 성공적으로 저장되었습니다." }, { "EN", "Game progress saved successfully." } } },
            { "dialog_confirm_main_menu_title", new Dictionary<string, string> { { "KR", "메인 메뉴" }, { "EN", "Main Menu" } } },
            { "dialog_confirm_main_menu_msg", new Dictionary<string, string> { { "KR", "메인 메뉴 화면으로 이동하시겠습니까?" }, { "EN", "Return to main menu screen?" } } },
            { "dialog_btn_yes", new Dictionary<string, string> { { "KR", "예" }, { "EN", "Yes" } } },
            { "dialog_btn_no", new Dictionary<string, string> { { "KR", "아니오" }, { "EN", "No" } } },
            { "dialog_btn_ok", new Dictionary<string, string> { { "KR", "확인" }, { "EN", "OK" } } },

            // --- HUD & Main Navigation ---
            { "hud_btn_gacha", new Dictionary<string, string> { { "KR", "뽑기" }, { "EN", "Gacha" } } },
            { "hud_btn_shop", new Dictionary<string, string> { { "KR", "상점" }, { "EN", "Shop" } } },
            { "hud_btn_upgrade", new Dictionary<string, string> { { "KR", "업그레이드" }, { "EN", "Upgrade" } } },
            { "hud_btn_exchange", new Dictionary<string, string> { { "KR", "교환소" }, { "EN", "Exchange" } } },
            { "hud_btn_encyclopedia", new Dictionary<string, string> { { "KR", "도감" }, { "EN", "Catalog" } } },
            { "hud_btn_collection", new Dictionary<string, string> { { "KR", "수집품" }, { "EN", "Collection" } } },

            // --- Encyclopedia & Catalog Tabs ---
            { "catalog_tab_all", new Dictionary<string, string> { { "KR", "전체" }, { "EN", "All" } } },
            { "catalog_search_placeholder", new Dictionary<string, string> { { "KR", "카드 이름 검색..." }, { "EN", "Search card name..." } } },
            { "catalog_set_reward_claim", new Dictionary<string, string> { { "KR", "보상 획득" }, { "EN", "Claim Reward" } } },
            { "catalog_set_reward_claimed", new Dictionary<string, string> { { "KR", "획득 완료" }, { "EN", "Claimed" } } },

            // --- Collection & Shop Tabs ---
            { "collection_tab_bg", new Dictionary<string, string> { { "KR", "배경" }, { "EN", "Backgrounds" } } },
            { "collection_tab_deco", new Dictionary<string, string> { { "KR", "장식" }, { "EN", "Decorations" } } },
            { "shop_title", new Dictionary<string, string> { { "KR", "상점" }, { "EN", "Shop" } } },
            { "shop_tab_upgrades", new Dictionary<string, string> { { "KR", "업그레이드" }, { "EN", "Upgrades" } } },
            { "shop_tab_shard_exchange", new Dictionary<string, string> { { "KR", "조각 교환" }, { "EN", "Shard Exchange" } } },
            { "shop_tab_products", new Dictionary<string, string> { { "KR", "상품" }, { "EN", "Products" } } },
            { "shop_sechdr_click", new Dictionary<string, string> { { "KR", "클릭 수익 업그레이드" }, { "EN", "Click Income Upgrades" } } },
            { "shop_sechdr_gacha", new Dictionary<string, string> { { "KR", "가챠 확률 / 비용 업그레이드" }, { "EN", "Gacha Upgrades" } } },
            { "shop_sechdr_economy", new Dictionary<string, string> { { "KR", "골드 / 조각 보너스 업그레이드" }, { "EN", "Economy Upgrades" } } },
            { "shop_sechdr_special", new Dictionary<string, string> { { "KR", "특수 기능 업그레이드" }, { "EN", "Special Upgrades" } } },

            // --- Encyclopedia Panel ---
            { "encyclopedia_title", new Dictionary<string, string> { { "KR", "도감" }, { "EN", "Catalog" } } },
            { "encyclopedia_search_placeholder", new Dictionary<string, string> { { "KR", "카드 이름 검색..." }, { "EN", "Search card name..." } } },
            { "encyclopedia_tab_all", new Dictionary<string, string> { { "KR", "전체" }, { "EN", "All" } } },
            { "encyclopedia_claim_reward", new Dictionary<string, string> { { "KR", "보상 받기" }, { "EN", "Claim Reward" } } },
            { "encyclopedia_set_representative", new Dictionary<string, string> { { "KR", "대표 설정" }, { "EN", "Set Main" } } },
            { "encyclopedia_breakthrough", new Dictionary<string, string> { { "KR", "한계 돌파" }, { "EN", "Breakthrough" } } },
            { "encyclopedia_claimed_reward", new Dictionary<string, string> { { "KR", "획득 완료" }, { "EN", "Claimed" } } },
            { "encyclopedia_equip_btn", new Dictionary<string, string> { { "KR", "장착하기" }, { "EN", "Equip Card" } } },
            { "encyclopedia_equipped_btn", new Dictionary<string, string> { { "KR", "장착중" }, { "EN", "Equipped" } } },
            { "encyclopedia_breakthrough_btn", new Dictionary<string, string> { { "KR", "돌파하기" }, { "EN", "Breakthrough" } } },

            // --- Common Labels ---
            { "common_level", new Dictionary<string, string> { { "KR", "레벨" }, { "EN", "Level" } } },
            { "common_coin", new Dictionary<string, string> { { "KR", "코인" }, { "EN", "Coins" } } },
            { "common_shard", new Dictionary<string, string> { { "KR", "조각" }, { "EN", "Shards" } } },
            { "common_equip", new Dictionary<string, string> { { "KR", "장착" }, { "EN", "Equip" } } },
            { "common_equipped", new Dictionary<string, string> { { "KR", "장착중" }, { "EN", "Equipped" } } },
            { "common_unlock", new Dictionary<string, string> { { "KR", "해금" }, { "EN", "Unlock" } } },
            { "common_locked", new Dictionary<string, string> { { "KR", "미해금" }, { "EN", "Locked" } } }
        };

        public static string Get(string key, string lang = null)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }

            if (Table.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var val) && !string.IsNullOrEmpty(val))
                    return val;
                if (dict.TryGetValue("KR", out var fallback))
                    return fallback;
            }
            return key;
        }

        public static void NotifyLanguageChanged()
        {
            OnLanguageChanged?.Invoke();
        }
    }
}
