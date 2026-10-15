using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
[InitializeOnLoad]
public static class ForestBridge {
 static string Root => Directory.GetParent(Application.dataPath).FullName;
 static ForestBridge(){ EditorApplication.update += Tick; }
 static void Tick(){
  string p=Root+"/ForestReports/command.txt";
  if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating) return;
  string cmd=File.ReadAllText(p).Trim(); File.Delete(p);
  try {
   if(cmd=="inspect") Inspect();
   else if(cmd=="build") ForestBuilder.Build();
   else if(cmd=="preview") ForestBuilder.Preview();
   else if(cmd=="validate") ForestBuilder.Validate();
   else if(cmd=="finalize"){ForestBuilder.Validate();UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();var t=typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");t?.GetMethod("Clear",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public)?.Invoke(null,null);}
   else if(cmd=="touch") ForestBuilder.TouchPreview();
   else if(cmd=="smoke") ForestSmoke.Start();
   else if(cmd=="play") EditorApplication.isPlaying=true;
   else if(cmd=="stop") EditorApplication.isPlaying=false;
   File.WriteAllText(Root+"/ForestReports/last-command.txt",cmd+" OK "+System.DateTime.Now);
  } catch(System.Exception e){File.WriteAllText(Root+"/ForestReports/error.txt",e.ToString());Debug.LogException(e);}
 }
 static void Inspect(){
  var s=new StringBuilder();
  foreach(string guid in AssetDatabase.FindAssets("t:GameObject",new[]{"Assets/VIVID_Arts/Assets/MountainLake/Mesh"})){
   string p=AssetDatabase.GUIDToAssetPath(guid); var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);
   if(!go)continue;s.AppendLine(p);
   foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true)){
    var f=r.GetComponent<MeshFilter>(); s.AppendLine("  "+r.name+" bounds "+(f&&f.sharedMesh?f.sharedMesh.bounds.ToString():"?"));
    foreach(var m in r.sharedMaterials)s.AppendLine("    material "+(m?m.name:"MISSING"));
   }
  }
  File.WriteAllText(Root+"/ForestReports/assets.txt",s.ToString());
 }
}
