#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CosmicChaosCat
{
    [InitializeOnLoad]
    public static class CatalogAutoCreator
    {
        static CatalogAutoCreator()
        {
            EditorApplication.delayCall += EnsureCatalogsExist;
        }

        // [MenuItem("Tools/Ensure Catalogs Exist")]
        public static void EnsureCatalogsExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            EnsureCatalogAsset<BackgroundCatalogSO>("Assets/ScriptableObjects/BackgroundCatalog.asset");
            EnsureCatalogAsset<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
        }

        private static void EnsureCatalogAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CatalogAutoCreator] ✅ {path} 카탈로그 에셋 파일이 자동 생성되었습니다.");
            }
        }
    }
}
#endif
