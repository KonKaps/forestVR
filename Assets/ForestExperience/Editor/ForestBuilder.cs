using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using ForestExperience;
using Random=UnityEngine.Random;
public static class ForestBuilder {
 const string D="Assets/ForestExperience/",Pack="Assets/VIVID_Arts/Assets/MountainLake/";
 static string Reports=>Directory.GetParent(Application.dataPath).FullName+"/ForestReports/";
 static Material bark,bases,fronds,farForest,grass,fern,rock,ground,mountain,glove;
 static Transform trees,plants,stones; static Terrain terrain;
 static T Load<T>(string p) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(p);
 static void Save(UnityEngine.Object o,string p){var existing=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);if(existing){EditorUtility.CopySerialized(o,existing);UnityEngine.Object.DestroyImmediate(o);}else AssetDatabase.CreateAsset(o,p);}
 static Texture2D Tex(string name)=>Load<Texture2D>(Pack+"Textures/"+name+".png");
 static Material Mat(string name,string texture,string normal,bool foliage=false,Color? tint=null){
  var m=new Material(Shader.Find(foliage?"ForestExperience/Touch Foliage":"Universal Render Pipeline/Lit"));m.name=name;m.enableInstancing=true;
  m.SetTexture("_BaseMap",Tex(texture));m.SetColor("_BaseColor",tint??Color.white);m.SetFloat("_Smoothness",.12f);
  if(normal!=null){m.SetTexture("_BumpMap",Tex(normal));m.SetFloat("_BumpScale",.65f);m.EnableKeyword("_NORMALMAP");}
  if(foliage){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.38f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
  Save(m,D+"Materials/"+name+".mat");return Load<Material>(D+"Materials/"+name+".mat");
 }
 static void Materials(){
  Directory.CreateDirectory(D+"Materials");AssetDatabase.Refresh();
  bark=Mat("Scanned bark","ML_Tree_005_Bark_BaseColor","ML_Tree_005_Bark_Normal");
  bases=Mat("Mossy roots","MLRT_TreeBases_BaseMap","MLRT_TreeBases_Normal");
  fronds=Mat("Canopy","MLRT_SugarMaple_Frond_Albedo","MLRT_SugarMaple_Frond_Normal",true,new Color(.38f,.65f,.16f));fronds.SetFloat("_WindStrength",.013f);fronds.SetFloat("_TouchStrength",0);
  farForest=Mat("Distant forest foliage","MLRT_Tree_Far_Frond_Albedo","MLRT_Tree_Far_Frond_Normal",true,new Color(.31f,.48f,.19f));farForest.SetFloat("_WindStrength",.008f);farForest.SetFloat("_TouchStrength",0);
  grass=Mat("Reachable grass","MLRT_Grass_BaseColor","MLRT_Grass_Normal",true,new Color(.82f,.9f,.68f));grass.SetFloat("_WindStrength",.075f);
  fern=Mat("Fern leaves","MLRT_Fern_Albedo","MLRT_Fern_Normal",true);fern.SetFloat("_WindStrength",.03f);
  rock=Mat("Weathered rock","MT_Rocks_Optimized_DefaultMaterial_BaseMap","MT_Rocks_Optimized_DefaultMaterial_Normal");
  ground=Mat("Leaf litter","South Hills Forest Floor Terrain_Optimized_DefaultMaterial_BaseColor","South Hills Forest Floor Terrain_Optimized_DefaultMaterial_Normal");
  mountain=Mat("Distant alpine ridge","MLRT_Mountainrange_BaseMap","MLRT_Mountainrange_Normal",false,new Color(.39f,.51f,.62f));
  glove=new Material(Shader.Find("Universal Render Pipeline/Lit"));glove.name="Soft glove";glove.SetColor("_BaseColor",new Color(.43f,.35f,.27f));glove.SetFloat("_Smoothness",.2f);Save(glove,D+"Materials/Soft glove.mat");glove=Load<Material>(D+"Materials/Soft glove.mat");
 }
 static Transform Group(string n){return new GameObject(n).transform;}
 static float Height(float x,float z){float r=Mathf.Sqrt(x*x+z*z);float noise=(Mathf.PerlinNoise(x*.07f+42,z*.07f+33)-.5f)*1.6f;float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01((r-1.6f)/8));return fade*(noise+.018f*x-.022f*Mathf.Max(z,0));}
 static void Ground(){
  var data=Load<TerrainData>(D+"Ground/Forest floor.asset");if(!data){data=new TerrainData();AssetDatabase.CreateAsset(data,D+"Ground/Forest floor.asset");}data.heightmapResolution=257;data.size=new Vector3(300,15,300);data.alphamapResolution=256;data.baseMapResolution=1024;
  var h=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)h[z,x]=(3+Height(x*300f/256-150,z*300f/256-150))/15;
  data.SetHeights(0,0,h);
  var litter=new TerrainLayer{diffuseTexture=Tex("South Hills Forest Floor Terrain_Optimized_DefaultMaterial_BaseColor"),normalMapTexture=Tex("South Hills Forest Floor Terrain_Optimized_DefaultMaterial_Normal"),tileSize=new Vector2(3,3),smoothness=.08f};
  var moss=new TerrainLayer{diffuseTexture=Tex("ML_Moss_Green_Ground_BaseColor"),normalMapTexture=Tex("ML_Moss_Green_Ground_Normal"),tileSize=new Vector2(2,2),smoothness=.08f};
  Save(litter,D+"Ground/Leaf litter.terrainlayer");Save(moss,D+"Ground/Moss.terrainlayer");data.terrainLayers=new[]{Load<TerrainLayer>(D+"Ground/Leaf litter.terrainlayer"),Load<TerrainLayer>(D+"Ground/Moss.terrainlayer")};
  var alpha=new float[256,256,2];for(int z=0;z<256;z++)for(int x=0;x<256;x++){float wx=x*300f/255-150,wz=z*300f/255-150;float m=Mathf.Clamp01((Mathf.PerlinNoise(wx*.22f+6,wz*.22f+21)-.33f)*2.5f);float center=Mathf.Exp(-(wx*wx+wz*wz)/5);m=Mathf.Lerp(m,.92f,center);alpha[z,x,0]=1-m;alpha[z,x,1]=m;}
  data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);
  var go=Terrain.CreateTerrainGameObject(data);go.name="Soft moss and leaf litter • touchable terrain";go.transform.position=new Vector3(-150,-3,-150);terrain=go.GetComponent<Terrain>();terrain.heightmapPixelError=5;terrain.basemapDistance=100;terrain.drawInstanced=true;
  var tm=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));Save(tm,D+"Materials/Terrain.mat");terrain.materialTemplate=Load<Material>(D+"Materials/Terrain.mat");
 }
 static GameObject Spawn(string file,Transform parent,Vector3 p,float scale,bool touch=false){
  var asset=Load<GameObject>(Pack+"Mesh/"+file+".fbx");if(!asset)throw new Exception("Missing "+file);
  var go=UnityEngine.Object.Instantiate(asset,parent);go.name=file;go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,Random.Range(0,360),0);go.transform.localScale=Vector3.one*scale;if(file.Contains("SugarMaple")&&!file.Contains("Base"))go.transform.localScale=new Vector3(scale*.7f,scale*1.15f,scale*.7f);
  foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(b);
  foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true)){
   string n=r.name; if(n.Contains("Emission")||n.Contains("Collider")){r.gameObject.SetActive(false);continue;}
   Material mat=rock;if(file.Contains("SugarMaple"))mat=n.Contains("Branches")?fronds:bark;
   if(file.Contains("Tree_Far"))mat=farForest;if(file.Contains("Grass"))mat=grass;if(file.Contains("Fern"))mat=fern;if(file.Contains("GroundRock"))mat=rock;if(file.Contains("Mountain"))mat=mountain;
   int count=r.GetComponent<MeshFilter>().sharedMesh.subMeshCount;var ms=Enumerable.Repeat(mat,count).ToArray();if(n.Contains("Trunk")&&count>1)ms[1]=bases;r.sharedMaterials=ms;
   r.shadowCastingMode=file.Contains("Mountain")?ShadowCastingMode.Off:ShadowCastingMode.On;r.receiveShadows=true;
  }
  // Imported FBX files already contain LOD groups. Retain them, removing emission-only children.
  if(go.GetComponentsInChildren<LODGroup>().Length==0){
   var renderers=go.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.Contains("Emission")&&!r.name.Contains("Collider")).ToArray();
   var levels=new List<LOD>();for(int i=0;i<4;i++){var rs=renderers.Where(r=>r.name.Contains("LOD"+i)).Cast<Renderer>().ToArray();if(rs.Length>0)levels.Add(new LOD(new[]{.3f,.12f,.035f,.009f}[i],rs));}
   if(levels.Count>0){var lg=go.AddComponent<LODGroup>();lg.SetLODs(levels.ToArray());lg.RecalculateBounds();}
  }
  foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
  if(touch){var col=go.AddComponent<BoxCollider>();col.isTrigger=true;col.center=new Vector3(0,.19f,0);col.size=new Vector3(.6f,.45f,.6f);go.AddComponent<ForestTouchSurface>();}
  else if(file.Contains("SugarMaple")&&p.magnitude<25){var c=go.AddComponent<CapsuleCollider>();c.radius=.3f;c.height=10;c.center=new Vector3(0,5,0);}
  if(!touch)go.isStatic=true;return go;
 }
 static void Forest(){
  trees=Group("01 • Tall forest around the clearing");plants=Group("02 • Grass and fern understory");stones=Group("03 • Mossy forest stones");
  var positions=new List<Vector2>();
  for(int i=0;i<1500&&positions.Count<150;i++){
   var p=Random.insideUnitCircle*64;float radius=p.magnitude;if(radius<4.2f)continue;
   // Leave an irregular mountain window beyond the clearing, facing forward.
   if(p.y>3&&Mathf.Abs(p.x)<3+p.y*.12f)continue;
   float spacing=radius<25?3.8f:5f;if(positions.Any(q=>Vector2.Distance(p,q)<spacing))continue;
   positions.Add(p);float s=Random.Range(.88f,1.28f);Spawn("MLRT_SugarMaple_0"+(positions.Count%4+1)+"_Var1",trees,new Vector3(p.x,Height(p.x,p.y),p.y),s);
  }
  for(int i=0;i<125;i++){
   var p=Random.insideUnitCircle*28;if(p.magnitude<2.2f)continue;Spawn("MLRT_Fern_0"+(i%3+1),plants,new Vector3(p.x,Height(p.x,p.y),p.y),Random.Range(.7f,1.4f),p.magnitude<2.8f);
  }
  for(int i=0;i<360;i++){
   var p=Random.insideUnitCircle*28;if(p.magnitude<1.6f)continue;Spawn("MLRT_Grass_01_Var3",plants,new Vector3(p.x,Height(p.x,p.y),p.y),Random.Range(.45f,.95f),p.magnitude<3);
  }
  // Small tufts within arm's reach in every direction; leave the centre clear for reclining.
  for(int i=0;i<76;i++){
   float a=i*2.399963f;float r=Random.Range(.48f,1.55f);float x=Mathf.Sin(a)*r,z=Mathf.Cos(a)*r;
   Spawn("MLRT_Grass_01_Var1",plants,new Vector3(x,Height(x,z),z),Random.Range(.55f,.95f),true);
  }
  for(int i=0;i<18;i++){float a=i*2.399963f;float r=Random.Range(1.1f,3.2f);float x=Mathf.Sin(a)*r,z=Mathf.Cos(a)*r;Spawn("MLRT_Fern_0"+(i%3+1),plants,new Vector3(x,Height(x,z),z),Random.Range(.65f,.95f),true);}
  for(int i=0;i<34;i++){var p=Random.insideUnitCircle*30;if(p.magnitude<2.4f)continue;Spawn("MT_Rock_Small_0"+(i%3+1),stones,new Vector3(p.x,Height(p.x,p.y)-.09f,p.y),Random.Range(.3f,.9f));}
  for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;float x=Mathf.Sin(a)*98,z=Mathf.Cos(a)*98;Spawn("MLRT_Tree_Far_Group_04",trees,new Vector3(x,Height(x,z),z),Random.Range(.9f,1.2f));}
  var ridge=Spawn("MLRT_MountainRange",Group("04 • Distant alpine horizon"),new Vector3(0,160,0),1f);ridge.transform.rotation=Quaternion.Euler(0,22,0);ridge.transform.localScale=new Vector3(1f,1.4f,1f);
 }
 static void Light(){
  foreach(string rp in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"}){var rd=Load<UniversalRendererData>(rp);if(rd){rd.renderingMode=RenderingMode.Forward;EditorUtility.SetDirty(rd);}}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.45f,.57f,.68f);RenderSettings.ambientEquatorColor=new Color(.3f,.37f,.3f);RenderSettings.ambientGroundColor=new Color(.13f,.17f,.12f);RenderSettings.ambientIntensity=1;
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogColor=new Color(.57f,.67f,.74f);RenderSettings.fogDensity=.002f;
  var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.48f,.58f,.7f));sky.SetFloat("_AtmosphereThickness",.85f);sky.SetFloat("_Exposure",1.1f);Save(sky,D+"Materials/Alpine sky.mat");RenderSettings.skybox=Load<Material>(D+"Materials/Alpine sky.mat");
  var sun= new GameObject("Sun • filtered afternoon daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.color=new Color(1,.94f,.81f);sun.intensity=2.1f;sun.shadows=LightShadows.Soft;sun.shadowBias=.04f;sun.shadowNormalBias=.25f;sun.shadowStrength=.8f;RenderSettings.sun=sun;
  var wind=new GameObject("Gentle canopy breeze").AddComponent<WindZone>();wind.mode=WindZoneMode.Directional;wind.windMain=.18f;wind.windTurbulence=.12f;wind.windPulseFrequency=.12f;wind.windPulseMagnitude=.08f;
  var vol=new GameObject("Natural exposure and colour").AddComponent<Volume>();vol.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);var color=profile.Add<ColorAdjustments>();color.postExposure.Override(.35f);color.saturation.Override(-4);var bloom=profile.Add<Bloom>();bloom.intensity.Override(.08f);bloom.threshold.Override(1.4f);Save(profile,D+"Natural light.asset");vol.sharedProfile=Load<VolumeProfile>(D+"Natural light.asset");
  foreach(var path in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Mobile_RPAsset.asset"}){var pipeline=Load<UniversalRenderPipelineAsset>(path);if(pipeline){var so=new SerializedObject(pipeline);so.FindProperty("m_GPUResidentDrawerMode").intValue=0;so.ApplyModifiedPropertiesWithoutUndo();pipeline.msaaSampleCount=4;pipeline.shadowDistance=45;pipeline.mainLightShadowmapResolution=2048;pipeline.shadowCascadeCount=2;EditorUtility.SetDirty(pipeline);}}
 }
 static void Sphere(Transform parent,string n,Vector3 p,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<MeshRenderer>().sharedMaterial=glove;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
 static ForestHand Hand(Transform rig,bool left){
  var g=new GameObject(left?"Left touch hand":"Right touch hand");g.transform.SetParent(rig,false);var h=g.AddComponent<ForestHand>();h.isLeft=left;h.rustle=Load<AudioClip>(D+"Audio/SoftGrassRustle.wav");var visual=new GameObject("Hand visual").transform;visual.SetParent(g.transform,false);h.visual=visual;
  Sphere(visual,"Palm",Vector3.zero,new Vector3(.075f,.034f,.095f));
  for(int i=0;i<4;i++)Sphere(visual,"Finger "+i,new Vector3((i-1.5f)*.018f,0,.069f),new Vector3(.014f,.017f,.065f-(i==3?.015f:0)));
  Sphere(visual,"Thumb",new Vector3(left?.046f:-.046f,-.002f,.02f),new Vector3(.032f,.02f,.048f));visual.gameObject.SetActive(false);
  var dust=new GameObject("Leaf litter touch motes").AddComponent<ParticleSystem>();dust.transform.SetParent(g.transform,false);var main=dust.main;main.startLifetime=.55f;main.startSpeed=.08f;main.startSize=.012f;main.startColor=new Color(.32f,.27f,.14f,.35f);main.maxParticles=45;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=dust.emission;emission.enabled=false;var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.035f;var pr=dust.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=glove;dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);h.soilParticles=dust;return h;
 }
 static void Rig(){
  var root=new GameObject("05 • Stationary VR viewer • height and rotation only");var rig=root.AddComponent<ForestRig>();
  var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.SetParent(root.transform,false);cam.transform.localPosition=new Vector3(0,1.65f,0);cam.transform.localRotation=Quaternion.Euler(0,0,0);cam.nearClipPlane=.03f;cam.farClipPlane=2400;cam.fieldOfView=78;cam.allowHDR=true;cam.gameObject.AddComponent<AudioListener>();var extra=cam.GetUniversalAdditionalCameraData();extra.renderPostProcessing=true;extra.antialiasing=AntialiasingMode.None;rig.view=cam;rig.leftHand=Hand(root.transform,true);rig.rightHand=Hand(root.transform,false);
  var audio=new GameObject("06 • Calm forest soundscape");var wind=audio.AddComponent<AudioSource>();wind.clip=Load<AudioClip>(Pack+"Audio/MLRT_Wind.wav");wind.loop=true;wind.playOnAwake=true;wind.volume=.2f;wind.spatialBlend=0;var ambience=audio.AddComponent<ForestAmbience>();ambience.wind=wind;ambience.birds=Load<AudioClip>(D+"Audio/DistantBirdsong.wav");
 }
 static void XR(){
  const string p=D+"XRGeneralSettings.asset";var per=Load<XRGeneralSettingsPerBuildTarget>(p);
  if(!per){per=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(per,p);}EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey,per,true);
  foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android}){
   var general=per.SettingsForBuildTarget(group);if(!general){per.CreateDefaultSettingsForBuildTarget(group);general=per.SettingsForBuildTarget(group);}
   if(!general.Manager){var mgr=ScriptableObject.CreateInstance<XRManagerSettings>();mgr.name=group+" XR manager";AssetDatabase.AddObjectToAsset(mgr,per);general.Manager=mgr;}
   general.InitManagerOnStart=true;general.Manager.automaticLoading=true;general.Manager.automaticRunning=true;
   if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group))throw new Exception("OpenXR loader assignment failed "+group);
   var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
   if(settings){settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;foreach(var f in settings.GetFeatures())if(f!=null){string n=f.GetType().Name;if(n.Contains("ControllerProfile")||n=="HandTracking"||n=="HandTrackingFeature"||n=="OpenXRHandTracking"||n=="OpenXRHandTrackingFeature")f.enabled=true;}EditorUtility.SetDirty(settings);}
   EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
  }EditorUtility.SetDirty(per);
 }
 [MenuItem("Forest Experience/Build forest clearing")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before building");
  // Preserve the user's currently open scene before opening our separate scene.
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)EditorSceneManager.SaveOpenScenes();
  Directory.CreateDirectory(D+"Ground");Directory.CreateDirectory(D+"Scenes");Random.InitState(151026);Materials();
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Ground();Forest();Light();Rig();XR();
  QualitySettings.vSyncCount=0;Application.targetFrameRate=90;PlayerSettings.colorSpace=ColorSpace.Linear;
  string path=D+"Scenes/Forest Sanctuary.unity";EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};AssetDatabase.SaveAssets();
  SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,10),Quaternion.Euler(10,0,0),20);
  File.WriteAllText(Reports+"built.txt","Forest Sanctuary saved: "+path);Preview();Validate();AssetDatabase.SaveAssets();
 }
 [MenuItem("Forest Experience/Save preview images")]
 public static void Preview(){
  var pipe=GraphicsSettings.currentRenderPipeline; if(pipe){var so=new SerializedObject(pipe);var property=so.FindProperty("m_GPUResidentDrawerMode");if(property!=null){property.intValue=0;so.ApplyModifiedPropertiesWithoutUndo();}}
  var cam=Camera.main;if(!cam)throw new Exception("No camera");var original=cam.transform.localPosition;var rotation=cam.transform.localRotation;
  foreach(var pair in new[]{("standing",1.65f),("seated",.95f),("ground",.22f)}){
   cam.transform.localPosition=new Vector3(0,pair.Item2,0);cam.transform.localRotation=Quaternion.Euler(pair.Item1=="ground"?12:0,0,0);var rt=new RenderTexture(1400,900,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var t=new Texture2D(1400,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1400,900),0,0);t.Apply();File.WriteAllBytes(Reports+"preview-"+pair.Item1+".png",t.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);
  }cam.transform.localPosition=original;cam.transform.localRotation=rotation;
 }
 public static void TouchPreview(){
  var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;float fov=cam.fieldOfView;
  var tuft=UnityEngine.Object.FindObjectsByType<ForestTouchSurface>(FindObjectsSortMode.None).Where(s=>s.name.Contains("Grass_01_Var1")).OrderBy(s=>s.transform.position.magnitude).First();
  Vector3 point=tuft.transform.TransformPoint(tuft.GetComponent<BoxCollider>().center);
  cam.transform.position=new Vector3(0,.5f,0);cam.transform.LookAt(point);cam.fieldOfView=55;
  Color32[] before=null;int changed=0;
  for(int i=0;i<2;i++){
   Shader.SetGlobalVector("_ForestRightHand",i==0?new Vector4(0,-1000,0,0):new Vector4(point.x,point.y,point.z,.3f));
   var rt=new RenderTexture(960,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var t=new Texture2D(960,720,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,960,720),0,0);t.Apply();var pixels=t.GetPixels32();
   if(i==0)before=pixels;else for(int j=0;j<pixels.Length;j++)if(Mathf.Abs(pixels[j].r-before[j].r)+Mathf.Abs(pixels[j].g-before[j].g)+Mathf.Abs(pixels[j].b-before[j].b)>30)changed++;
   File.WriteAllBytes(Reports+(i==0?"touch-before.png":"touch-after.png"),t.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);
  }
  Shader.SetGlobalVector("_ForestRightHand",new Vector4(0,-1000,0,0));cam.transform.position=pos;cam.transform.rotation=rot;cam.fieldOfView=fov;
  File.WriteAllText(Reports+"touch-render-test.txt",(changed>100?"PASS":"FAIL")+" • touched grass changes "+changed+" rendered pixels.");if(changed<=100)throw new Exception("Touch shader did not visibly deform grass");
 }
 [MenuItem("Forest Experience/Validate scene")]
 public static void Validate(){
  var issues=new List<string>();var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
  foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported||ShaderUtil.ShaderHasError(m.shader))issues.Add("Material or shader failure: "+r.name);
  var rig=UnityEngine.Object.FindFirstObjectByType<ForestRig>();if(!rig||!rig.view||!rig.leftHand||!rig.rightHand)issues.Add("Rig missing references");
  var terrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);if(terrains.Length!=1)issues.Add("Terrain missing");
  var audio=UnityEngine.Object.FindFirstObjectByType<ForestAmbience>();if(!audio||!audio.wind.clip||!audio.birds)issues.Add("Audio missing");
  foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android}){var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);if(general==null||general.Manager==null||!general.Manager.activeLoaders.Any(l=>l is UnityEngine.XR.OpenXR.OpenXRLoader))issues.Add("OpenXR missing "+group);}
  int touch=UnityEngine.Object.FindObjectsByType<ForestTouchSurface>(FindObjectsSortMode.None).Length;if(touch<60)issues.Add("Insufficient touch vegetation");
  File.WriteAllText(Reports+"validation.txt",$"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}\nRenderers: {renderers.Length}\nTouch vegetation: {touch}\nOpenXR: Standalone and Android\n"+(issues.Count==0?"PASS • materials, terrain, audio, rig, touch surfaces and XR settings":string.Join("\n",issues)));
  if(issues.Count>0)throw new Exception(string.Join("\n",issues.Take(20)));
 }
}
