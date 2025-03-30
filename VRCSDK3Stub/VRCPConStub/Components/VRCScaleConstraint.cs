using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Animations;
using VRC.Dynamics.ManagedTypes;

namespace VRC.SDK3.Dynamics.Constraint.Components
{
  public sealed class VRCScaleConstraint : VRCScaleConstraintBase { }

  [CustomEditor(typeof(VRCScaleConstraint))]
  public class VRCScaleConstraintEditor : Editor
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
        "This VRChat constraint component should be converted to a standard Unity ScaleConstraint for compatibility with CVRFury."
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        ConvertToUnityConstraint();
      })
      {
        text = "Convert to Unity ScaleConstraint"
      };
      convertButton.style.marginTop = new StyleLength(10);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }

    private void ConvertToUnityConstraint()
    {
      VRCScaleConstraint vrcConstraint = (VRCScaleConstraint)target;
      GameObject gameObject = vrcConstraint.gameObject;

      // Add Unity ScaleConstraint component
      ScaleConstraint unityConstraint = Undo.AddComponent<ScaleConstraint>(gameObject);

      // Transfer basic properties
      unityConstraint.weight = vrcConstraint.GlobalWeight;
      unityConstraint.constraintActive = vrcConstraint.IsActive;

      // Set at rest scale
      unityConstraint.scaleAtRest = vrcConstraint.ScaleAtRest;

      // Set offset
      unityConstraint.scaleOffset = vrcConstraint.ScaleOffset;

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

      // Set axes
      unityConstraint.scalingAxis = 0;
      if (vrcConstraint.AffectsScaleX)
        unityConstraint.scalingAxis |= Axis.X;
      if (vrcConstraint.AffectsScaleY)
        unityConstraint.scalingAxis |= Axis.Y;
      if (vrcConstraint.AffectsScaleZ)
        unityConstraint.scalingAxis |= Axis.Z;

      // Remove VRC component after conversion
      Undo.DestroyObjectImmediate(vrcConstraint);

      EditorUtility.DisplayDialog("Conversion Complete", "Successfully converted to Unity ScaleConstraint", "OK");
    }
  }
}
