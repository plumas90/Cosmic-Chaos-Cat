using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using CosmicChaosCat;

namespace CosmicChaosCat.Editor
{
    public static class DuplicateSlot0
    {
        [MenuItem("Tools/Encyclopedia/Slot_0 구조를 다른 Slot들로 복사")]
        public static void SyncSlot0Structure()
        {
            var encyPanel = Object.FindObjectOfType<EncyclopediaPanel>(true);
            if (encyPanel == null)
            {
                EditorUtility.DisplayDialog("Slot 복사 에러", "EncyclopediaPanel을 찾을 수 없습니다.", "확인");
                return;
            }

            var slot0 = encyPanel.transform.Find("EncyPanel/NoPanel/Page/Slot_0");
            if (slot0 == null)
            {
                // Fallback search
                var childs = encyPanel.GetComponentsInChildren<CardSlotUI>(true);
                foreach (var c in childs)
                {
                    if (c.name == "Slot_0") { slot0 = c.transform; break; }
                }
            }

            if (slot0 == null)
            {
                EditorUtility.DisplayDialog("Slot 복사 에러", "Slot_0 오브젝트를 찾을 수 없습니다.", "확인");
                return;
            }

            Transform gridParent = slot0.parent;
            int count = 0;

            for (int i = 1; i < 9; i++)
            {
                string targetName = $"Slot_{i}";
                var target = gridParent.Find(targetName);

                if (target == null)
                {
                    // If target slot doesn't exist, instantiate copy from Slot_0
                    var dup = Object.Instantiate(slot0.gameObject, gridParent);
                    dup.name = targetName;
                    target = dup.transform;
                }
                else
                {
                    // Position preservation
                    Vector2 savedPos = target.GetComponent<RectTransform>().anchoredPosition;

                    // Re-instantiate from Slot_0 to get exact hierarchy & component setup
                    var dup = Object.Instantiate(slot0.gameObject, gridParent);
                    dup.name = targetName;
                    dup.GetComponent<RectTransform>().anchoredPosition = savedPos;

                    // Set sibling index to match target
                    dup.transform.SetSiblingIndex(target.GetSiblingIndex());

                    // Destroy old target
                    Object.DestroyImmediate(target.gameObject);
                }
                count++;
            }

            // Grid auto-layout positioning if 3x3 layout is needed
            float colW = 145f, rowH = 155f;
            float startX = -145f, startY = 90f;

            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                var slotTf = gridParent.Find($"Slot_{index}");
                if (slotTf != null)
                {
                    var rt = slotTf.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(startX + col * colW, startY - row * rowH);
                    }
                }
            }

            EditorUtility.SetDirty(encyPanel.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[DuplicateSlot0] Slot_0의 하이라키 구조를 Slot_1 ~ Slot_8 ({count}개)로 복사했습니다.");
            EditorUtility.DisplayDialog("복사 완료", $"Slot_0 구조를 Slot_1~Slot_8에 반영했습니다!", "확인");
        }
    }
}
