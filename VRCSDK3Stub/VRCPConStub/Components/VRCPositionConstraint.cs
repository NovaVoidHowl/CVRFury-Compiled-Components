using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Animations;
using VRC.Dynamics.ManagedTypes;
using System;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCPConStub.Common.StubVersion;

namespace VRC.SDK3.Dynamics.Constraint.Components
{
  public sealed class VRCPositionConstraint : VRCPositionConstraintBase 
  {
    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCPConStub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCPConStub.Common.StubVersion.CurrentVersion; }
    }
  }

  [CustomEditor(typeof(VRCPositionConstraint))]
  public class VRCPositionConstraintEditor : Editor
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
        "This VRChat constraint component should be converted to a standard Unity PositionConstraint for compatibility with CVRFury."
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        ConvertToUnityConstraint();
      })
      {
        text = "Convert to Unity PositionConstraint"
      };
      convertButton.style.marginTop = new StyleLength(10);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }

    private void ConvertToUnityConstraint()
    {
      VRCPositionConstraint vrcConstraint = (VRCPositionConstraint)target;
      GameObject gameObject = vrcConstraint.gameObject;

      // Add Unity PositionConstraint component
      PositionConstraint unityConstraint = Undo.AddComponent<PositionConstraint>(gameObject);

      // Transfer basic properties
      unityConstraint.weight = vrcConstraint.GlobalWeight;
      unityConstraint.constraintActive = vrcConstraint.IsActive;

      // Set at rest position
      unityConstraint.translationAtRest = vrcConstraint.PositionAtRest;

      // Set offset
      unityConstraint.translationOffset = vrcConstraint.PositionOffset;

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
      unityConstraint.translationAxis = 0;
      if (vrcConstraint.AffectsPositionX)
        unityConstraint.translationAxis |= Axis.X;
      if (vrcConstraint.AffectsPositionY)
        unityConstraint.translationAxis |= Axis.Y;
      if (vrcConstraint.AffectsPositionZ)
        unityConstraint.translationAxis |= Axis.Z;

      // Remove VRC component after conversion
      Undo.DestroyObjectImmediate(vrcConstraint);

      EditorUtility.DisplayDialog("Conversion Complete", "Successfully converted to Unity PositionConstraint", "OK");
    }
  }
}
