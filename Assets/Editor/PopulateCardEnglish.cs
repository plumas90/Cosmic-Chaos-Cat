using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CosmicChaosCat;

namespace CosmicChaosCat.EditorTools
{
    [InitializeOnLoad]
    public static class PopulateCardEnglish
    {
        static PopulateCardEnglish()
        {
            EditorApplication.delayCall += RunPopulate;
        }

        [MenuItem("CosmicChaosCat/Populate All English Data")]
        public static void RunPopulate()
        {
            PopulateCards();
            PopulateSets();
            PopulateDecorations();
        }

        [MenuItem("CosmicChaosCat/Prepare Set Reward Test (Unlock Cards x5, Lock BGs & Decos)")]
        public static void PrepareSetRewardTest()
        {
            var gm = Object.FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                gm.ResetBackgroundsAndDecorationsForTest();
                Debug.Log("[PrepareSetRewardTest] Successfully updated live GameManager instance & PlayerPrefs save file!");
            }
            else
            {
                var cat = AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
                var setCat = AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");

                var data = new GameSaveData
                {
                    Money = 100000d,
                    Shards = 5000,
                    EquippedCardId = "0001",
                    EquippedBackgroundId = "bg",
                    EquippedDecorationId = "deco-none",
                    SelectedLanguage = "KR"
                };

                if (cat != null)
                {
                    foreach (var card in cat.Cards)
                    {
                        if (card != null && !string.IsNullOrEmpty(card.Id))
                        {
                            data.Cards.Add(new CardProgress { CardId = card.Id, Copies = 5, Unlocked = true, BreakthroughCount = 0 });
                        }
                    }
                }

                if (setCat != null)
                {
                    foreach (var s in setCat.Sets)
                    {
                        if (s != null && !string.IsNullOrEmpty(s.SetId))
                        {
                            data.CompletedSets.Add(s.SetId);
                        }
                    }
                }

                data.UnlockedBackgrounds.Add("bg");
                data.UnlockedBackgrounds.Add("bg-none");
                data.UnlockedDecorations.Add("deco-none");

                PlayerPrefs.SetString("ccc_save_v3", JsonUtility.ToJson(data));
                PlayerPrefs.SetString("CosmicChaosCat_SaveData", JsonUtility.ToJson(data));
                PlayerPrefs.Save();
                Debug.Log("[PrepareSetRewardTest] Successfully generated & saved test save file to PlayerPrefs (Key: ccc_save_v3)!");
            }
        }

        private static void PopulateCards()
        {
            var cat = AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
            if (cat == null) return;

            var dict = new Dictionary<string, string[]>()
            {
                { "0001", new string[] { "Cat in the Box", "It's cleaner inside than expected." } },
                { "0002", new string[] { "Tin Hat Cat", "Wears a tin pot like a helmet and bravely roams the alley." } },
                { "0003", new string[] { "Umbrella Cat", "An emotional cat who enjoys walks with an umbrella on rainy days." } },
                { "0004", new string[] { "Fishbone Cat", "Guards delicious fishbones with a satisfied expression." } },
                { "0005", new string[] { "Spray Cat", "A street artist who paints awesome graffiti on walls." } },
                { "0006", new string[] { "Night Delivery Cat", "Cuts through the night sky to deliver secret parcels." } },
                { "0007", new string[] { "Padlock Cat", "A mysterious cat guarding sturdy padlocks." } },
                { "0008", new string[] { "Pickpocket Cat", "A nimble alley cat swift at stealth and pickpocketing." } },
                { "0009", new string[] { "Neon Hacker Cat", "A legendary hacker cat infiltrating neon cyber networks." } },
                { "0010", new string[] { "Pipe Boss Cat", "The formidable boss cat ruling the underground pipes." } },
                { "0011", new string[] { "Shadow Acrobat Cat", "An agile acrobat cat performing stunts in the shadows." } },
                { "0012", new string[] { "Eclipse Boss Cat", "The dark ruler cat governing during total solar eclipses." } },
                { "0013", new string[] { "Alley Witch Cat", "A mystic witch cat brewing magic potions in back alleys." } },
                { "0014", new string[] { "City Ghost Cat", "A phantom cat floating quietly through the night city." } },

                { "0015", new string[] { "Sand Footprint Cat", "Lightly steps across the hot desert sands." } },
                { "0016", new string[] { "Papyrus Cat", "Preciously holds ancient papyrus scrolls." } },
                { "0017", new string[] { "Jar Cat", "Enjoys a cozy nap inside a cool ceramic jar." } },
                { "0018", new string[] { "Scarab Cat", "Wears a lucky scarab ornament that brings good fortune." } },
                { "0019", new string[] { "Temple Scribe Cat", "Carefully inscribes secret hieroglyphs on temple walls." } },
                { "0020", new string[] { "Desert Guide Cat", "Guides lost travelers across the desert to lush oases." } },
                { "0021", new string[] { "Nile Fisher Cat", "An expert fisher cat along the banks of the Nile." } },
                { "0022", new string[] { "Little Mummy Cat", "A cute mummy cat wrapped in ancient bandages." } },
                { "0023", new string[] { "Sphinx Guardian Cat", "A noble guardian cat guarding ancient pyramid treasures." } },
                { "0024", new string[] { "Sun Priest Cat", "Performs sacred rituals to dispel droughts with solar power." } },
                { "0025", new string[] { "Desert Chariot Cat", "A brave warrior cat driving a desert war chariot." } },
                { "0026", new string[] { "Bastet Avatar Cat", "The divine avatar cat inheriting the power of goddess Bastet." } },
                { "0027", new string[] { "Eye of Ra Cat", "A sacred cat seeing through truth using the Eye of Ra." } },
                { "0028", new string[] { "Anubis Judge Cat", "A divine judge weighing souls to guide them to eternity." } },

                { "0029", new string[] { "Flour Cat", "Strolls around the bakery dusted with savory flour." } },
                { "0030", new string[] { "Sugar Cat", "A sweet cutie melting softly like powdered sugar." } },
                { "0031", new string[] { "Dough Cat", "Takes a loaf pose on top of fluffy bread dough." } },
                { "0032", new string[] { "Cookie Cat", "Smells sweet like freshly baked crispy cookies." } },
                { "0033", new string[] { "Macaron Cat", "A lovely cat wearing a colorful, chewy macaron cone." } },
                { "0034", new string[] { "Cupcake Cat", "Carries smooth whipped cream and a cherry on its head." } },
                { "0035", new string[] { "Chocolate Fountain Cat", "Enjoys fondue overflowing with rich, dripping chocolate." } },
                { "0036", new string[] { "Jelly Courier Cat", "Delivers bouncy, colorful jellies to neighbors." } },
                { "0037", new string[] { "Croissant Knight Cat", "A steadfast knight clad in flaky croissant armor." } },
                { "0038", new string[] { "Cake Wizard Cat", "Casts sweet magic spells to make everyone happy." } },
                { "0039", new string[] { "Ice Cream Alchemist Cat", "Mixes cool ice cream ingredients to create alchemy formulas." } },
                { "0040", new string[] { "Confectionery Queen Cat", "An elegant queen ruling the dessert castle realm." } },
                { "0041", new string[] { "Star Candy Angel Cat", "Gathers star candies from the night sky to bless people." } },
                { "0042", new string[] { "Infinite Dessert Dragon Cat", "A legendary dragon cat guarding the endless dessert spring." } },

                { "0043", new string[] { "Bubble Scout Cat", "A scout cat swimming gracefully inside water bubbles." } },
                { "0044", new string[] { "Shell Collector Cat", "Gathers pretty seashells and arranges them on the beach." } },
                { "0045", new string[] { "Coral Gardener Cat", "Trims colorful coral reefs to build a comfortable home." } },
                { "0046", new string[] { "Jellyfish Lantern Cat", "Illuminates deep waters using glowing jellyfish as lanterns." } },
                { "0047", new string[] { "Golden Seahorse Herald Cat", "Rides a golden seahorse to deliver joyful news from the ocean kingdom." } },
                { "0048", new string[] { "Crab Armor Cat", "A brave sentinel armed with sturdy crab pincers and armor." } },
                { "0049", new string[] { "Pearl Harp Cat", "Plays a shimmering pearl harp to compose tranquil sea melodies." } },
                { "0050", new string[] { "Anchor Swimmer Cat", "Directs undersea exploration effortlessly carrying a heavy anchor." } },
                { "0051", new string[] { "Submarine Captain Cat", "A leader operating high-tech submarines for deep sea expeditions." } },
                { "0052", new string[] { "Ocean Archmage Cat", "Masterfully wields blue whirlpools and water elemental magic." } },
                { "0053", new string[] { "Coral Guardian Knight Cat", "Swings a coral blade to protect undersea castle walls." } },
                { "0054", new string[] { "Mermaid Empress Cat", "An empress governing all ocean creatures with her clear voice." } },
                { "0055", new string[] { "Sapphire Dragon Cat", "A sea dragon cat guarding sapphire mines in the deep abyss." } },
                { "0056", new string[] { "Poseidon Cat", "A sea god cat wielding a trident to command seven sea waves." } },

                { "0057", new string[] { "Mask Dance Cat", "Dances cheerfully wearing a traditional mask to wish for good fortune." } },
                { "0058", new string[] { "Mackerel Vendor Cat", "Carries fresh salted mackerel in a A-frame carrier across the market." } },
                { "0059", new string[] { "Shield Kite Cat", "Flies a colorful traditional kite high into the cool breeze." } },
                { "0060", new string[] { "Blacksmith Cat", "Crafts hoes and sickles diligently in front of a red forge." } },
                { "0061", new string[] { "Bamboo Hat Farmer Cat", "Works hard in golden rice fields praying for a bountiful harvest." } },
                { "0062", new string[] { "Harvest Manager Cat", "Inspects rice sacks filling storehouses to the brim in autumn." } },
                { "0063", new string[] { "Market Officer Cat", "A dependable officer keeping peace in the bustling marketplace." } },
                { "0064", new string[] { "Herbal Physician Cat", "Brews warm herbal medicine to care for neighbors' health." } },
                { "0065", new string[] { "Moonlight Storyteller Cat", "Tells thrilling ancient folktales under candlelight every night." } },
                { "0066", new string[] { "Mountain Spirit Cat", "A wise mountain spirit dwelling deep in misty mountains." } },
                { "0067", new string[] { "Rice Cake Pounding Master Cat", "A master pounding chewy rice dough to make sweet rice cakes." } },
                { "0068", new string[] { "Samulnori Drummer Cat", "Heightens festival excitement with grand drums and spinning hat ribbons." } },
                { "0069", new string[] { "Tiger Mask Cat", "Wears a sacred tiger mask to ward off evil and boost spirits." } },
                { "0070", new string[] { "White Tiger Guardian Cat", "A guardian spirit inheriting divine powers of the sacred White Tiger." } },

                { "0071", new string[] { "Red Ranger Hero Cat", "A passionate leader of the justice-seeking ranger team." } },
                { "0072", new string[] { "Cobalt Brain Hero Cat", "Analyzes tactics with brilliant intellect and high-tech visors." } },
                { "0073", new string[] { "Yellow Speed Hero Cat", "Chases villains with lightning speed faster than the wind." } },
                { "0074", new string[] { "Emerald Rescue Hero Cat", "Protects teammates with a steadfast shield and healing suit." } },
                { "0075", new string[] { "Rose Flight Hero Cat", "Equipped with wing engines to dominate aerial combat." } },
                { "0076", new string[] { "Black Stealth Hero Cat", "A covert infiltration expert vanishing into night shadows." } },
                { "0077", new string[] { "Orange Burst Hero Cat", "Strikes enemy bases with powerful flame blasters." } },
                { "0078", new string[] { "Crimson Claw Captain Cat", "A captain wielding energetic sharp claws." } },
                { "0079", new string[] { "Blue Shark Marine Hero Cat", "A streamlined aquatic combat hero guarding underwater bases." } },
                { "0080", new string[] { "White Blizzard Hero Cat", "Instantly freezes enemy movements with glacial frost." } },
                { "0081", new string[] { "Purple Psychic Hero Cat", "A telekinetic psychic freely moving objects with energy beams." } },
                { "0082", new string[] { "Golden Lion Commander Cat", "A commander piloting a golden lion robot to lead entire fleets." } },
                { "0083", new string[] { "Silver Scarlet Jet Hero Cat", "A hero roaming outer space in a supersonic jet craft." } },
                { "0084", new string[] { "Galactic Prism Supreme Cat", "The ultimate guardian focusing prism energy to vanquish evil." } },

                { "0085", new string[] { "Steampunk Apprentice Sailor Cat", "Sweeps airship decks wearing a cogwheel hat." } },
                { "0086", new string[] { "Coal Boiler Cat", "Shovels coal continuously into giant steam boilers." } },
                { "0087", new string[] { "Airship Lens Scout Cat", "Checks flight routes beyond clouds using telescope gear." } },
                { "0088", new string[] { "Scrap Cog Collector Cat", "Gathers rolling gears and cogs for handy recycling." } },
                { "0089", new string[] { "Steel Welder Cat", "Firmly welds airship hulls while emitting blue sparks." } },
                { "0090", new string[] { "Cloud Meterologist Cat", "Studies sky weather with altimeters and barometers." } },
                { "0091", new string[] { "Propeller Duelist Cat", "A master engaging in aerial dueling with a high-speed propeller." } },
                { "0092", new string[] { "Steam Medic Cat", "Treats crew members' wounds using sterilizing steam packs." } },
                { "0093", new string[] { "Storm Tracker Cat", "A veteran navigator safely sailing airships through thunderstorms." } },
                { "0094", new string[] { "Ironwing Pilot Cat", "An ace performing aerobatics in a steel-winged plane." } },
                { "0095", new string[] { "Steam Engine Engineer Cat", "A genius mechanic understanding complex steam engines perfectly." } },
                { "0096", new string[] { "Sky Pirate Queen Cat", "The ruler of steampunk airship fleets seeking sky treasures." } },
                { "0097", new string[] { "Golden Airship Flagship Captain Cat", "A captain steering giant flagships toward legendary Utopias." } },
                { "0098", new string[] { "Eternal Clockwork Cat", "Governs unstoppable time across dimensions of gears and clocks." } },

                { "0099", new string[] { "Dandelion Seed Cat", "Blows dandelion seeds and travels on spring breezes." } },
                { "0100", new string[] { "Yellow Raincoat Cat", "Paddles through rain puddles wearing yellow boots and raincoat." } },
                { "0101", new string[] { "Silver Daisy Cat", "Takes a gentle nap in fragrant daisy fields." } },
                { "0102", new string[] { "Maple Leaf Cat", "Builds a warm nest gathering red autumn maple leaves." } },
                { "0103", new string[] { "Cherry Blossom Calico Cat", "Enjoys romance under fluttering pink cherry blossom petals." } },
                { "0104", new string[] { "Navy Collar Black Cat", "A charming black cat wearing a mysterious blue collar." } },
                { "0105", new string[] { "Midsummer Thunderstorm Cat", "Chases rainbows across fields after sudden summer rain showers." } },
                { "0106", new string[] { "Chuseok Full Moon Cat", "Shares warm hearts with neighbors under the bright full moon." } },
                { "0107", new string[] { "Spring Breeze Spirit Cat", "Brings cozy spring winds to turn winter mountains into flower fields." } },
                { "0108", new string[] { "First Snow Guardian Cat", "Steps softly on first snow and builds snowmen with friends." } },
                { "0109", new string[] { "Winter Aurora Queen Cat", "Draped in a radiant aurora veil shimmering across night skies." } },
                { "0110", new string[] { "Maple Mountain Spirit Cat", "A deity solemnly decorating whole mountains in burning red autumn colors." } },
                { "0111", new string[] { "Eternal Sky Spirit Cat", "Holds blue sky energy to peacefully cycle natural seasons." } },
                { "0112", new string[] { "Four Seasons Sacred Beast Cat", "A noble beast cat protecting nature across spring, summer, autumn, and winter." } }
            };

            int updatedCount = 0;
            foreach (var card in cat.CardsList)
            {
                if (card != null && dict.TryGetValue(card.Id, out var val))
                {
                    card.DisplayName_EN = val[0];
                    card.Description_EN = val[1];
                    updatedCount++;
                }
            }

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PopulateCardEnglish] Populated {updatedCount} cards in CardCatalog.asset!");
        }

        private static void PopulateSets()
        {
            var cat = AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            if (cat == null) return;

            var dict = new Dictionary<string, string[]>()
            {
                { "1", new string[] { "Alley Cats", "Bonus for collecting all Alley Cats!" } },
                { "2", new string[] { "Desert & Egyptian Cats", "Bonus for collecting all Desert Cats!" } },
                { "3", new string[] { "Dessert & Sweets Cats", "Bonus for collecting all Dessert Cats!" } },
                { "4", new string[] { "Ocean & Deep Sea Cats", "Bonus for collecting all Ocean Cats!" } },
                { "5", new string[] { "Traditional & Folklore Cats", "Bonus for collecting all Traditional Cats!" } },
                { "6", new string[] { "Ranger & Sci-Fi Hero Cats", "Bonus for collecting all Hero Cats!" } },
                { "7", new string[] { "Steampunk & Airship Cats", "Bonus for collecting all Steampunk Cats!" } },
                { "8", new string[] { "Four Seasons & Nature Spirit Cats", "Bonus for collecting all Seasons Cats!" } },
                { "9", new string[] { "Special Cats", "Bonus for collecting Special Cats!" } },
                { "10", new string[] { "Cosmic & Alien Cats", "Bonus for collecting Cosmic Cats!" } },
                { "11", new string[] { "Fantasy Adventurer Cats", "Bonus for collecting Fantasy Cats!" } },
                { "12", new string[] { "Cyberpunk Neon Cats", "Bonus for collecting Cyberpunk Cats!" } },
                { "13", new string[] { "Masterpiece & Artist Cats", "Bonus for collecting Art Cats!" } },
                { "14", new string[] { "Music & Orchestra Cats", "Bonus for collecting Music Cats!" } }
            };

            var rewardBgs = new Dictionary<string, string[]>()
            {
                { "1", new string[] { "bg_s1", "deco-cat-house" } },
                { "2", new string[] { "bg_s2", "deco-pyramid" } },
                { "3", new string[] { "bg_s3", "deco-cake" } },
                { "4", new string[] { "bg_s4", "deco-aquarium" } },
                { "5", new string[] { "bg_s5", "deco-drum" } },
                { "6", new string[] { "bg_s6", "deco-robot" } },
                { "7", new string[] { "bg_s7", "deco-airship" } },
                { "8", new string[] { "bg_s8", "deco-aurora" } }
            };

            int updatedCount = 0;
            foreach (var set in cat.SetsList)
            {
                if (set != null)
                {
                    if (dict.TryGetValue(set.SetId, out var val))
                    {
                        set.SetName_EN = val[0];
                        set.EffectDesc_EN = val[1];
                    }
                    if (rewardBgs.TryGetValue(set.SetId, out var rw))
                    {
                        set.RewardBackgroundId = rw[0];
                        set.RewardDecorationId = rw[1];
                    }
                    updatedCount++;
                }
            }

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PopulateSetEnglish] Populated {updatedCount} sets in SetCatalog.asset!");
        }

        private static void PopulateDecorations()
        {
            var cat = AssetDatabase.LoadAssetAtPath<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
            if (cat == null) return;

            var dict = new Dictionary<string, string[]>()
            {
                { "deco-none", new string[] { "No Decoration", "Does not place decorations on background." } },
                { "deco-cat-house", new string[] { "Fluffy Cat Tower", "Fluffy cat tower decoration loved by cats." } },
                { "deco-pyramid", new string[] { "Mini Pyramid", "Desert oasis guardian pyramid object." } },
                { "deco-cake", new string[] { "3-Tier Dessert Cake", "Sweet whipped cream cake decoration." } },
                { "deco-aquarium", new string[] { "Rainbow Coral Aquarium", "Radiant undersea coral aquarium ornament." } },
                { "deco-drum", new string[] { "Samulnori Drum", "Traditional Samulnori drum heightening festival excitement." } },
                { "deco-robot", new string[] { "Golden Lion Robot", "Majestic commander transforming robot figure." } },
                { "deco-airship", new string[] { "Steampunk Golden Airship", "Elaborate clockwork airship model decoration." } },
                { "deco-aurora", new string[] { "Four Seasons Aurora Crystal", "Crystal sphere shimmering with mysterious four seasons aurora." } }
            };

            int updatedCount = 0;
            foreach (var deco in cat.DecorationsList)
            {
                if (deco != null && dict.TryGetValue(deco.Id, out var val))
                {
                    deco.DisplayName_EN = val[0];
                    deco.Description_EN = val[1];
                    updatedCount++;
                }
            }

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PopulateDecorationEnglish] Populated {updatedCount} decorations in DecorationCatalog.asset!");
        }
    }
}
