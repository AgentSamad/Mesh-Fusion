using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

    public class FuseMesh : EditorWindow
    {
        private DefaultAsset _exportDirectory = null;
        private GameObject targetMesh = null;
        private bool _exportMesh;
        
        [MenuItem("Window/FuseMesh")]
        static void Open()
        {
            GetWindow<FuseMesh>("FuseMesh").Show();
        }

        void OnGUI()
        {
            targetMesh = (GameObject)EditorGUILayout.ObjectField("TargetMesh", targetMesh, typeof(GameObject), true);
            _exportMesh = EditorGUILayout.Toggle("Export Mesh", _exportMesh);
            _exportDirectory = (DefaultAsset) EditorGUILayout.ObjectField("Export Path", _exportDirectory, typeof(DefaultAsset), true);
            if (GUILayout.Button("Combine"))
            {
                if (targetMesh == null)
                {
                    return;
                }
                CombineMesh();
            }
        }

        void CombineMesh()
        {
            var meshFilters = targetMesh.GetComponentsInChildren<MeshFilter>();
            var combineMeshInstanceDictionary = new Dictionary<Material, List<CombineInstance>>();

            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                var vertices = new List<Vector3>();
                var materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                var subMeshCount = meshFilter.sharedMesh.subMeshCount;
                mesh.GetVertices(vertices);

                for (var i = 0; i < subMeshCount; i++)
                {
                    var material = materials[i];
                    var triangles = new List<int>();
                    mesh.GetTriangles(triangles, i);

                    var newMesh = new Mesh
                    {
                        vertices = vertices.ToArray(), triangles = triangles.ToArray(), uv = mesh.uv, normals = mesh.normals
                    };

                    if (!combineMeshInstanceDictionary.ContainsKey(material))
                    {
                        combineMeshInstanceDictionary.Add(material, new List<CombineInstance>());
                    }

                    var combineInstance = new CombineInstance
                        {transform = meshFilter.transform.localToWorldMatrix, mesh = newMesh};
                    combineMeshInstanceDictionary[material].Add(combineInstance);
                }
            }
            
            targetMesh.SetActive(false);

            foreach (var kvp in combineMeshInstanceDictionary)
            {
                var newObject = new GameObject(kvp.Key.name);

                var meshRenderer = newObject.AddComponent<MeshRenderer>();
                var meshFilter = newObject.AddComponent<MeshFilter>();

                meshRenderer.material = kvp.Key;
                var mesh = new Mesh();
                mesh.CombineMeshes(kvp.Value.ToArray());
                Unwrapping.GenerateSecondaryUVSet(mesh);

                meshFilter.sharedMesh = mesh;
                newObject.transform.parent = targetMesh.transform.parent;

                if (!_exportMesh || _exportDirectory == null)
                {
                    continue;
                }

                ExportMesh(mesh, kvp.Key.name);
            }
        }

        void ExportMesh(Mesh mesh, string fileName)
        {
            var exportDirectoryPath = AssetDatabase.GetAssetPath(_exportDirectory);
            if (Path.GetExtension(fileName) != ".asset")
            {
                fileName += ".asset";
            }
            var exportPath = Path.Combine(exportDirectoryPath, fileName);
            AssetDatabase.CreateAsset(mesh, exportPath);
        }
    }
