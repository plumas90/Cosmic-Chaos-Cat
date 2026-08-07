using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using CosmicChaosCat;

public class RebuildEncyclopediaSlots : MonoBehaviour
{
    // [MenuItem("Tools/Rebuild Encyclopedia Slots (2x Size)")]
    public static void RebuildSlots()
    {
        var panel = FindObjectOfType<EncyclopediaPanel>(true);
        if (panel == null)
        {
            Debug.LogError("EncyclopediaPanel not found in scene!");
            return;
        }

        // Find NoTabRoot
        Transform noTabRoot = null;
        Transform leftPage = null;
        Transform rightPage = null;

        var children = panel.GetComponentsInChildren<Transform>(true);
        foreach (var t in children)
        {
            if (t.name == "NoTabRoot")
            {
                noTabRoot = t;
                int pageCount = 0;
                foreach (Transform child in t)
                {
                    if (child.name.StartsWith("Page"))
                    {
                        if (pageCount == 0) leftPage = child;
                        else if (pageCount == 1) rightPage = child;
                        pageCount++;
                    }
                }
            }
        }

        if (noTabRoot == null || leftPage == null || rightPage == null)
        {
            Debug.LogError("Could not find NoTabRoot or Page0/Page1 inside EncyclopediaPanel.");
            return;
        }

        // Clear existing slots
        var oldSlots = new List<GameObject>();
        foreach (Transform child in leftPage) if (child.name.StartsWith("Slot_")) oldSlots.Add(child.gameObject);
        foreach (Transform child in rightPage) if (child.name.StartsWith("Slot_")) oldSlots.Add(child.gameObject);
        foreach (var go in oldSlots) DestroyImmediate(go);

        // Build new slots (x2 size, 4x4 per page = 16 per page, 32 total)
        BuildSlotGrid(leftPage, 0);
        BuildSlotGrid(rightPage, 16);

        // Apply Font
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Galmuri9 SDF.asset");
        if (font != null)
        {
            var txts = panel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in txts) t.font = font;
        }

        EditorUtility.SetDirty(panel.gameObject);
        Debug.Log("Successfully rebuilt 32 slots (2x size) in EncyclopediaPanel.");
    }

    private static void BuildSlotGrid(Transform pageTf, int startIdx)
    {
        float colW = 133f, rowH = 160f; 
        float startX = -((4 - 1) * colW) / 2f;
        float startY = ((4 - 1) * rowH) / 2f;

        for (int row = 0; row < 4; row++)
        for (int col = 0; col < 4; col++)
        {
            int si = startIdx + row * 4 + col;
            float x = startX + col * colW;
            float y = startY - row * rowH;

            var slotGO = new GameObject($"Slot_{si}");
            slotGO.transform.SetParent(pageTf, false);
            slotGO.AddComponent<Button>();

            var slotRT = slotGO.AddComponent<RectTransform>();
            slotRT.anchoredPosition = new Vector2(x, y);
            slotRT.sizeDelta        = new Vector2(120, 146); 

            var slotImg = slotGO.AddComponent<Image>();
            slotImg.color = new Color(0.15f, 0.17f, 0.25f, 1f);

            var frameGO = MakeImage(slotGO.transform, "Frame", Vector2.zero, new Vector2(114, 141), new Color(0.4f, 0.4f, 0.4f, 0.5f));
            var artGO   = MakeImage(slotGO.transform, "Art", new Vector2(0, 16), new Vector2(96, 96), Color.gray);
            
            var nameTx  = MakeText(slotGO.transform, "???", new Vector2(0, -48), new Vector2(117, 24), 12, Color.white);
            nameTx.alignment = TextAlignmentOptions.Center;
            
            var rarityTx= MakeText(slotGO.transform, "", new Vector2(0, -64), new Vector2(117, 18), 10, Color.gray);
            rarityTx.alignment = TextAlignmentOptions.Center;

            // 미해금 오버레이
            var unkGO = new GameObject("Unknown");
            unkGO.transform.SetParent(slotGO.transform, false);
            var unkRT = unkGO.AddComponent<RectTransform>();
            unkRT.anchorMin = Vector2.zero; unkRT.anchorMax = Vector2.one;
            unkRT.offsetMin = Vector2.zero; unkRT.offsetMax = Vector2.zero;
            var unkImg = unkGO.AddComponent<Image>();
            unkImg.color = new Color(0.05f, 0.05f, 0.10f, 0.85f);
            var unkTx = MakeText(unkGO.transform, "?", Vector2.zero, new Vector2(106, 106), 48, new Color(0.5f, 0.5f, 0.6f));
            unkTx.alignment = TextAlignmentOptions.Center;

            var ui = slotGO.AddComponent<CardSlotUI>();
            ui.InitUI(
                frameGO.GetComponent<Image>(),
                artGO.GetComponent<Image>(),
                nameTx,
                rarityTx,
                unkGO
            );
        }
    }

    private static GameObject MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = col;
        return go;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, Color col)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmpro = go.AddComponent<TextMeshProUGUI>();
        tmpro.text = text;
        tmpro.fontSize = fontSize;
        tmpro.color = col;
        tmpro.alignment = TextAlignmentOptions.MidlineLeft;
        return tmpro;
    }
}
