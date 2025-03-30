using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Animations;
using VRC.Dynamics.ManagedTypes;

namespace VRC.SDK3.Dynamics.Constraint.Components
{
  public sealed class VRCLookAtConstraint : VRCLookAtConstraintBase { }

  [CustomEditor(typeof(VRCLookAtConstraint))]
  public class VRCLookAtConstraintEditor : Editor
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
        "This VRChat constraint component should be converted to a standard Unity LookAtConstraint for compatibility with CVRFury."
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        ConvertToUnityConstraint();
      })
      {
        text = "Convert to Unity LookAtConstraint"
      };
      convertButton.style.marginTop = new StyleLength(10);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }

    private void ConvertToUnityConstraint()
    {
      VRCLookAtConstraint vrcConstraint = (VRCLookAtConstraint)target;
      GameObject gameObject = vrcConstraint.gameObject;

      // Add Unity LookAtConstraint component
      LookAtConstraint unityConstraint = Undo.AddComponent<LookAtConstraint>(gameObject);

      // Transfer basic properties
      unityConstraint.weight = vrcConstraint.GlobalWeight;
      unityConstraint.constraintActive = vrcConstraint.IsActive;

      // Set rotation offset
      unityConstraint.roll = vrcConstraint.Roll;

      // Set world up settings
      unityConstraint.useUpObject = vrcConstraint.UseUpTransform;
      if (vrcConstraint.UseUpTransform && vrcConstraint.WorldUpTransform != null)
      {
        unityConstraint.worldUpObject = vrcConstraint.WorldUpTransform;
      }

      // Add sources
      for (int i = 0; i < vrcConstraint.Sources.Count; i++)
      {
        var vrcSource = vrcConstraint.Sources[i];
        if (vrcSource.SourceTransform != null)
        {
          ConstraintSource unitySource = new ConstraintSource
          {
            sourceTransform = vrcSource.SourceTransform,
            weight = vrcSource.Weight
          };
          unityConstraint.AddSource(unitySource);
        }
      }

      // Remove VRC component after conversion
      Undo.DestroyObjectImmediate(vrcConstraint);

      EditorUtility.DisplayDialog("Conversion Complete", "Successfully converted to Unity LookAtConstraint", "OK");
    }
  }
}
