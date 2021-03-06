using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WeChat
{
    [InitializeOnLoad]
    [DeclarePreset("ngui-asset", typeof(NGUIExportConfig))]
    internal class NGUIAssetExportPreset : ExportPreset
    {
        static NGUIAssetExportPreset()
        {
            ExportPreset.registerExportPreset("ngui-asset", new NGUIAssetExportPreset());
        }

        public NGUIAssetExportPreset() : base()
        {
            is2d = true;
        }

        public override string GetChineseName()
        {
            return "选中的ngui-prefab资源";
        }

        protected override void DoExport()
        {
            string[] assetIDs;
            Queue<string> selectionAssetGuids = new Queue<string>();
            // 导出批量文件
            assetIDs = Selection.assetGUIDs;

            foreach (string guid in assetIDs)
            {
                exportQueue.Enqueue(guid);
            }

            DequeueAndExport(exportQueue.Count);
        }

        Queue<string> exportQueue = new Queue<string>();
        // 导出一个资源
        private void DequeueAndExport(int maxCount)
        {
            string guid = exportQueue.Dequeue();
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            EditorUtility.DisplayProgressBar(
                "资源导出",
                assetPath,
                (float)(maxCount - exportQueue.Count - 1) / maxCount
            );

            // gameObject
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(GameObject))
            {
                GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(
                    assetPath,
                    AssetDatabase.GetMainAssetTypeAtPath(assetPath)
                );
                // prefab
                // 忘了为什么要加这句判断了，资源管理器里的prefab理论上这里返回都是null
                if (WXUtility.GetPrefabSource(prefab))
                {
                    DequeueAndExport(maxCount);
                    return;
                }


                // 资源管理器里的prefab，GetPrefabSource是null，这里就要实例化之后再取。
                prefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Selection.activeObject = prefab;
                prefab.transform.position = Vector3.zero;
                prefab.SetActive(true);
                //RichText.StaticTextCreator[] staicTextCreators = prefabRoot.GetComponentsInChildren<RichText.StaticTextCreator>();
                //if (staicTextCreators != null && staicTextCreators.Length > 0)
                //{
                //    for (int k = 0; k < staicTextCreators.Length; k++)
                //    {
                //        RichText.StaticTextCreator stc = staicTextCreators[k];
                //        stc.ParseStaticText(true);
                //    }
                //}

                WXNGUITree wxPrefab = new WXNGUITree(prefab, assetPath, false);
                PresetUtil.writeGroup(wxPrefab, this/*, (string)(configs.ContainsKey("exportPath") ? configs["exportPath"] : "")*/);

                UnityEngine.Object.DestroyImmediate(prefab);
            }

            if (exportQueue.Count == 0)
            {
                EditorUtility.ClearProgressBar();
            }
            else
            {
                DequeueAndExport(maxCount);
            }
        }

        public override bool WillPresetShow()
        {
            return (
                Selection.assetGUIDs.Length > 1 ||
                (Selection.assetGUIDs.Length == 1 && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject)))
            );
        }
    }
}