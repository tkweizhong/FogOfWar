using System;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR

using UnityEditor;

public class MeshSave : MonoBehaviour
{
    public void SaveAsset()
    {
        Debug.Log("Begin Save Mesh");
        try
        {
            Mesh mesh = this.GetComponent<MeshFilter>().sharedMesh;
            if (mesh != null) 
            {
                mesh = Instantiate(mesh);
                mesh.name = mesh.name.Replace("(Clone)", "");

                string dir = Application.dataPath + "/Resources/SavedMesh/";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string assetPath = string.Format("{0}/{1}.asset", dir, Path.GetFileName(mesh.name) );
                if (File.Exists(assetPath))
                {
                    throw new Exception(string.Format("Asset {0} is already exist!", mesh.name) );
                }
                
                AssetDatabase.CreateAsset(mesh, "Assets/Resources/SavedMesh/" + mesh.name+ ".asset");                
                Debug.Log("save mesh success :"+mesh.name+".asset");

                return;
            }
            throw new Exception("Mesh is null");
        }
        catch (Exception e)
        {
            Debug.LogWarning("save mesh failed£º" + e.ToString());
        }
    }
}

[CustomEditor(typeof(MeshSave))]
public class MeshSaveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshSave meshSaveInstance = (MeshSave)target;

        if (GUILayout.Button("SaveMesh"))
        {
            meshSaveInstance.SaveAsset();
        }
    }
}

#endif