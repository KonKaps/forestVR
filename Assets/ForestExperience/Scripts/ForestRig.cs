using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
namespace ForestExperience {
 [DefaultExecutionOrder(10000)]
 public class ForestRig : MonoBehaviour {
  public Camera view; public ForestHand leftHand,rightHand; public float fallbackEyeHeight=1.65f;
  InputAction hp,hr,ht,lp,lr,lt,rp,rr,rt;
  bool floorSet; float nextFloorAttempt;
  readonly List<XRInputSubsystem> inputs=new(); readonly List<XRHandSubsystem> hands=new();
  InputAction Action(string name,string path){var a=new InputAction(name,InputActionType.Value,path);a.Enable();return a;}
  void Awake(){Application.targetFrameRate=90;}
  void OnEnable(){
   hp=Action("Head position","<XRHMD>/centerEyePosition");hr=Action("Head rotation","<XRHMD>/centerEyeRotation");ht=Action("Head tracked","<XRHMD>/isTracked");
   lp=Action("Left position","<XRController>{LeftHand}/devicePosition");lr=Action("Left rotation","<XRController>{LeftHand}/deviceRotation");lt=Action("Left tracked","<XRController>{LeftHand}/isTracked");
   rp=Action("Right position","<XRController>{RightHand}/devicePosition");rr=Action("Right rotation","<XRController>{RightHand}/deviceRotation");rt=Action("Right tracked","<XRController>{RightHand}/isTracked");
   Application.onBeforeRender+=ApplyPose;
  }
  void OnDisable(){Application.onBeforeRender-=ApplyPose;foreach(var a in new[]{hp,hr,ht,lp,lr,lt,rp,rr,rt})a?.Dispose();}
  void Update(){
   if(!floorSet&&Time.unscaledTime>nextFloorAttempt){nextFloorAttempt=Time.unscaledTime+2;SubsystemManager.GetSubsystems(inputs);foreach(var s in inputs)if(s.running)floorSet=s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);}
   ApplyPose();
#if UNITY_EDITOR
   if(ht.ReadValue<float>()<.5f){
    var k=Keyboard.current; if(k!=null){float y=view.transform.localPosition.y; if(k.pageUpKey.isPressed)y+=Time.deltaTime*.6f;if(k.pageDownKey.isPressed)y-=Time.deltaTime*.6f;view.transform.localPosition=new Vector3(0,Mathf.Clamp(y,.15f,2.1f),0);}
    if(Mouse.current!=null&&Mouse.current.rightButton.isPressed){var d=Mouse.current.delta.ReadValue(); var e=view.transform.localEulerAngles; view.transform.localRotation=Quaternion.Euler(e.x-d.y*.12f,e.y+d.x*.12f,0);}
    if(Mouse.current!=null&&Mouse.current.leftButton.isPressed){var ray=view.ScreenPointToRay(Mouse.current.position.ReadValue());if(Physics.Raycast(ray,out var hit,4)){rightHand.SetSimulation(hit.point+hit.normal*.025f);}}else rightHand.EndSimulation();
   }
#endif
  }
  void ApplyPose(){
   bool headTracked=ht!=null&&ht.ReadValue<float>()>.5f;
   Vector3 p=headTracked?hp.ReadValue<Vector3>():Vector3.zero;
   if(headTracked){view.transform.localPosition=new Vector3(0,Mathf.Max(.08f,p.y+(floorSet?0:fallbackEyeHeight)),0);view.transform.localRotation=hr.ReadValue<Quaternion>();}
   var offset=new Vector3(-p.x,headTracked&&!floorSet?fallbackEyeHeight:0,-p.z); // Keep hands aligned with the horizontally constrained head.
   SubsystemManager.GetSubsystems(hands);XRHandSubsystem hs=null;foreach(var s in hands)if(s.running){hs=s;break;}
   ApplyHand(leftHand,hs, true,lp,lr,lt,offset);ApplyHand(rightHand,hs,false,rp,rr,rt,offset);
  }
  void ApplyHand(ForestHand hand,XRHandSubsystem hs,bool left,InputAction pos,InputAction rot,InputAction tracked,Vector3 offset){
   if(hs!=null){var h=left?hs.leftHand:hs.rightHand;if(h.isTracked&&h.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm)){
    var tip=h.GetJoint(XRHandJointID.IndexTip);tip.TryGetPose(out Pose tipPose);
    hand.SetTracked(palm.position+offset,palm.rotation,(tipPose.position==Vector3.zero?palm.position:tipPose.position)+offset,true);return;
   }}
   if(tracked.ReadValue<float>()>.5f){Vector3 p=pos.ReadValue<Vector3>()+offset;Quaternion q=rot.ReadValue<Quaternion>();hand.SetTracked(p,q,p+q*new Vector3(0,0,.1f),false);}else hand.HideTracking();
  }
  public void Recenter(){floorSet=false;foreach(var s in inputs)s.TryRecenter();}
 }
}
