using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Management;
using UnityEditor;
using UnityEditor.XR.Management;
using ForestExperience;
[InitializeOnLoad]
public static class ForestSmoke {
 static bool done;static string Report=>Directory.GetParent(Application.dataPath).FullName+"/ForestReports/playmode-test.txt";
 static ForestSmoke(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
 public static void Start(){
  SessionState.SetBool("ForestSmoke",true);done=false;
  var settings=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);if(settings)settings.InitManagerOnStart=false;
  EditorApplication.isPlaying=true;
 }
 static void Mode(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("ForestSmoke",false)){
  var s=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);if(s){s.InitManagerOnStart=true;EditorUtility.SetDirty(s);AssetDatabase.SaveAssets();}SessionState.SetBool("ForestSmoke",false);
 }}
 static void Tracked(UnityEngine.InputSystem.TrackedDevice device){using(StateEvent.From(device,out var ev)){device.isTracked.WriteValueIntoEvent(1f,ev);InputSystem.QueueEvent(ev);}InputSystem.Update();}
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static void Tick(){
  if(!SessionState.GetBool("ForestSmoke",false)||!EditorApplication.isPlaying||Time.time<2||done)return;done=true;
  XRHMD head=null;XRController left=null;
  try{
   var rig=UnityEngine.Object.FindFirstObjectByType<ForestRig>();Assert(rig!=null,"Rig missing");
   head=InputSystem.AddDevice<XRHMD>();left=InputSystem.AddDevice<XRController>();InputSystem.SetDeviceUsage(left,CommonUsages.LeftHand);
   Tracked(head);InputState.Change(head.centerEyeRotation,Quaternion.Euler(17,32,0));
   foreach(float height in new[]{1.65f,.95f,.22f}){
    using(StateEvent.From(head,out var ev)){head.isTracked.WriteValueIntoEvent(1f,ev);head.centerEyePosition.WriteValueIntoEvent(new Vector3(2.4f,height-rig.fallbackEyeHeight,-1.7f),ev);head.centerEyeRotation.WriteValueIntoEvent(Quaternion.Euler(17,32,0),ev);InputSystem.QueueEvent(ev);}InputSystem.Update();rig.SendMessage("ApplyPose");
    var p=rig.view.transform.localPosition;Assert(Mathf.Abs(p.x)<.001f&&Mathf.Abs(p.z)<.001f,"Horizontal motion leaked");Assert(Mathf.Abs(p.y-height)<.001f,"Height did not follow headset: expected "+height+" got "+p+" device "+head.centerEyePosition.ReadValue()+" tracked "+head.isTracked.ReadValue());
   }
   Assert(Quaternion.Angle(rig.view.transform.localRotation,Quaternion.Euler(17,32,0))<.1f,"Head rotation failed");
   using(StateEvent.From(left,out var ev)){left.isTracked.WriteValueIntoEvent(1f,ev);left.devicePosition.WriteValueIntoEvent(new Vector3(2.1f,-1.2f,-1.5f),ev);left.deviceRotation.WriteValueIntoEvent(Quaternion.identity,ev);InputSystem.QueueEvent(ev);}InputSystem.Update();rig.SendMessage("ApplyPose");
   Assert(rig.leftHand.active,"Controller hand did not track");Assert(Vector3.Distance(rig.leftHand.transform.localPosition,new Vector3(-.3f,.45f,.2f))<.001f,"Hands misaligned with constrained head");
   var surface=UnityEngine.Object.FindFirstObjectByType<ForestTouchSurface>();var box=surface.GetComponent<BoxCollider>();var point=surface.transform.TransformPoint(box.center);
   rig.rightHand.SetSimulation(point);rig.rightHand.SendMessage("LateUpdate");Assert(rig.rightHand.GetComponent<AudioSource>().isPlaying,"Grass touch rustle failed");
   var v=Shader.GetGlobalVector("_ForestRightHand");Assert(Vector3.Distance((Vector3)v,point)<.001f&&v.w>0,"Bending shader did not receive touch");
   rig.leftHand.SetSimulation(new Vector3(0,.02f,0));rig.leftHand.SendMessage("LateUpdate");Assert(rig.leftHand.soilParticles.particleCount>0,"Terrain touch particles failed");
   var audio=UnityEngine.Object.FindFirstObjectByType<ForestAmbience>();Assert(audio.wind.isPlaying&&audio.wind.loop,"Wind ambience not playing");
   File.WriteAllText(Report,"PASS\nSimulated XR input: standing 1.65m, seated 0.95m, lying 0.22m\nHorizontal head position stays at zero despite tracked X/Z motion\nHead rotation preserved\nController position remains aligned with constrained head\nGrass contact plays rustle and updates bending shader\nTerrain contact emits leaf-litter motes\nWind ambience plays and loops\nHardware stereo, hand tracking and haptics require a headset test.\n");
  }catch(Exception e){File.WriteAllText(Report,"FAIL\n"+e);Debug.LogException(e);}
  finally{if(head!=null)InputSystem.RemoveDevice(head);if(left!=null)InputSystem.RemoveDevice(left);EditorApplication.isPlaying=false;}
 }
}
