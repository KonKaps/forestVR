using UnityEngine;
using UnityEngine.XR;
namespace ForestExperience {
 public class ForestHand:MonoBehaviour {
  public bool isLeft;public Transform visual; public AudioClip rustle;public ParticleSystem soilParticles;
  public Vector3 touchPoint; public bool active; bool simulated;AudioSource source; float lastFeedback;Vector3 previous;
  static readonly int Left=Shader.PropertyToID("_ForestLeftHand"),Right=Shader.PropertyToID("_ForestRightHand");
  void Awake(){source=gameObject.AddComponent<AudioSource>();source.spatialBlend=1;source.minDistance=.2f;source.maxDistance=3;source.playOnAwake=false;}
  public void SetTracked(Vector3 position,Quaternion rotation,Vector3 tip,bool bareHand){simulated=false;active=true;transform.localPosition=position;transform.localRotation=rotation;touchPoint=transform.parent.TransformPoint(tip);visual.gameObject.SetActive(true);}
  public void HideTracking(){if(!simulated){active=false;visual.gameObject.SetActive(false);}}
  public void SetSimulation(Vector3 p){simulated=true;active=true;transform.position=p;touchPoint=p;visual.gameObject.SetActive(true);}
  public void EndSimulation(){if(simulated){simulated=false;active=false;visual.gameObject.SetActive(false);}}
  void LateUpdate(){
   Shader.SetGlobalVector(isLeft?Left:Right,active?new Vector4(touchPoint.x,touchPoint.y,touchPoint.z,.3f):new Vector4(0,-1000,0,0));
   if(!active)return;
   var contacts=Physics.OverlapSphere(touchPoint,.075f,~0,QueryTriggerInteraction.Collide);
   bool vegetation=false,ground=false;
   foreach(var c in contacts){if(c.GetComponent<ForestTouchSurface>())vegetation=true;if(c is TerrainCollider)ground=true;}
   float speed=(touchPoint-previous).magnitude/Mathf.Max(Time.deltaTime,.001f);previous=touchPoint;
   if((vegetation||ground)&&Time.time-lastFeedback>.28f&&speed>.02f){
    lastFeedback=Time.time;if(rustle)source.PlayOneShot(rustle,ground?.12f:.18f);
    var device=InputDevices.GetDeviceAtXRNode(isLeft?XRNode.LeftHand:XRNode.RightHand);
    var controller=isLeft?UnityEngine.InputSystem.XR.XRController.leftHand:UnityEngine.InputSystem.XR.XRController.rightHand;
    if(controller is UnityEngine.InputSystem.XR.XRControllerWithRumble rumble)rumble.SendImpulse(ground?.16f:.08f,.05f);
    else if(device.TryGetHapticCapabilities(out var cap)&&cap.supportsImpulse)device.SendHapticImpulse(0,ground?.16f:.08f,.05f);
    if(ground&&soilParticles){soilParticles.transform.position=touchPoint;soilParticles.Emit(5);}
   }
  }
 }

}
