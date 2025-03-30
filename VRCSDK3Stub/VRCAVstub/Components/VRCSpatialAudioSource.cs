using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

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

  [CustomEditor(typeof(VRCSpatialAudioSource))]
  public class VRCSpatialAudioSourceEditor : Editor
  {
    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This component is not compatible for data import to CVRFury and should be removed, please click the below button to do so"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var removeButton = new Button(() =>
      {
        DestroyImmediate(target);
      })
      {
        text = "Remove VRCSpatialAudioSource Component"
      };
      removeButton.style.marginTop = new StyleLength(10);

      root.Add(warningBox);
      root.Add(removeButton);

      return root;
    }
  }
}
