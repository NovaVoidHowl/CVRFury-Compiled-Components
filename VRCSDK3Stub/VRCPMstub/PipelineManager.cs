using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace VRC.Core
{
  public class PipelineManager : MonoBehaviour
  {
    public string blueprintId;
    public ContentType contentType;

    [Serializable]
    public enum ContentType
    {
      avatar = 0,
      world = 1
    }

    public static readonly Version VRCPMstubVersion = new Version(1, 0, 3);

    [SerializeField]
    private string _stubVersion;

    private void OnValidate()
    {
      _stubVersion = VRCPMstubVersion.ToString();
    }

    public string StubVersion => _stubVersion ?? VRCPMstubVersion.ToString();
  }

  [CustomEditor(typeof(PipelineManager))]
  public class PipelineManagerEditor : Editor
  {
    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var versionLabel = new Label($"Stub Version: {((PipelineManager)target).StubVersion}");
      versionLabel.style.marginBottom = new StyleLength(10);
      root.Add(versionLabel);

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This component is not needed for data import to CVRFury and should be removed, please click the below button to do so"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      Label blueprintLabel;

      if (((PipelineManager)target).blueprintId == null || ((PipelineManager)target).blueprintId == "")
      {
        blueprintLabel = new Label($"Blueprint ID: Not Set");
      }
      else
      {
        blueprintLabel = new Label($"Blueprint ID: {((PipelineManager)target).blueprintId}");
      }

      blueprintLabel.style.marginTop = new StyleLength(10);

      var removeButton = new Button(() =>
      {
        DestroyImmediate(target);
      })
      {
        text = "Remove PipelineManager Component"
      };
      removeButton.style.marginTop = new StyleLength(10);

      root.Add(blueprintLabel);
      root.Add(warningBox);
      root.Add(removeButton);

      return root;
    }
  }
}
