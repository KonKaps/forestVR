using UnityEngine;
namespace ForestExperience {
 public class ForestAmbience:MonoBehaviour {
  public AudioSource wind; public AudioClip birds;AudioSource song;float nextSong;float fade;
  void Start(){song=gameObject.AddComponent<AudioSource>();song.spatialBlend=1;song.minDistance=8;song.maxDistance=45;song.volume=.13f;nextSong=Time.time+6;}
  void Update(){fade=Mathf.MoveTowards(fade,1,Time.deltaTime/5);if(wind)wind.volume=fade*(.2f+.025f*Mathf.Sin(Time.time*.12f));if(Time.time>nextSong){nextSong=Time.time+Random.Range(18f,38f);song.transform.position=new Vector3(Random.Range(-16,16),Random.Range(6,12),Random.Range(8,25));song.pitch=Random.Range(.87f,1.12f);if(birds)song.PlayOneShot(birds);}}
 }
}
