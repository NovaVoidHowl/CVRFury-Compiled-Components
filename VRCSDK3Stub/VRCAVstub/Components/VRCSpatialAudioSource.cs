using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion;

namespace VRC.SDK3.Avatars.Components
{
  [ExecuteInEditMode]
  [RequireComponent(typeof(AudioSource))]
  [DisallowMultipleComponent]
  public class VRCSpatialAudioSource : VRC_SpatialAudioSource { }

  [ExecuteInEditMode]
  [RequireComponent(typeof(AudioSource))]
  [DisallowMultipleComponent]
  public abstract class VRC_SpatialAudioSource : MonoBehaviour
  {
    public delegate void InitializationDelegate(VRC_SpatialAudioSource obj);

    public float Gain = 10f;

    public float Far = 40f;

    public float Near;

    public float VolumetricRadius;

    public bool EnableSpatialization = true;

    public bool UseAudioSourceVolumeCurve;

    public static InitializationDelegate Initialize;

    private AudioSource _source;

    private void Awake()
    {
      _source = GetComponent<AudioSource>();
      if (_source == null)
      {
        Debug.LogErrorFormat("[{0}:VRC_SpatialAudioSource without an AudioSource component!", base.gameObject.name);
      }
      else if (Initialize != null)
      {
        Initialize(this);
        if (!EnableSpatialization)
        {
          _source.spatialize = false;
        }
      }
    }

    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion; }
    }

    private void OnDrawGizmosSelected()
    {
      if (EnableSpatialization && !UseAudioSourceVolumeCurve)
      {
        Color color = default(Color);
        color.r = 1f;
        color.g = 0.5f;
        color.b = 0f;
        color.a = 1f;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(base.transform.position, Near);
        color.a = 0.1f;
        Gizmos.color = color;
        Gizmos.DrawSphere(base.transform.position, Near);
        color.r = 1f;
        color.g = 0f;
        color.b = 0f;
        color.a = 1f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(base.transform.position, Far);
        color.a = 0.1f;
        Gizmos.color = color;
        Gizmos.DrawSphere(base.transform.position, Far);
        color.r = 1f;
        color.g = 0f;
        color.b = 1f;
        color.a = 1f;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(base.transform.position, VolumetricRadius);
        color.a = 0.1f;
        Gizmos.color = color;
        Gizmos.DrawSphere(base.transform.position, VolumetricRadius);
      }
    }
  }
}
