using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Animations;
using VRC.Dynamics.ManagedTypes;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.Constraint.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles
using System;

namespace VRC.SDK3.Dynamics.Constraint.Editors
{
  /// <summary>
  /// Custom editor for VRCRotationConstraint that provides UI and CVRFury conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCRotationConstraint))]
  public class VRCRotationConstraintEditor : Editor
  {
    // CSS class name constants
    private const string CSS_CVR_FURY_BUTTON = "cvr-fury-button";
    private const string CSS_INFO_BOX = "info-box";
    private const string STUB_VERSION = "stub-version";
    private const string UI_VERSION = "ui-version";

    private static void ApplyCVRFuryStyles(VisualElement root)
    {
      // Use shared stylesheet and class wiring
      SharedUIStyles.ApplySharedStyles(root);
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      ApplyCVRFuryStyles(root);

      // Warning box with converter information
      var warningBox = new Box();
      warningBox.AddToClassList(CSS_INFO_BOX);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This VRChat constraint component should be converted to a standard Unity RotationConstraint for compatibility with CVRFury."
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      // Convert button
      var convertButton = new Button(() =>
      {
        ConvertToUnityConstraint();
      })
      {
        text = "Convert to Unity RotationConstraint"
      };
      convertButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      convertButton.style.marginTop = new StyleLength(10);

      // Version information
      var versionContainer = new VisualElement();
      versionContainer.style.marginTop = new StyleLength(10);
      versionContainer.style.flexDirection = FlexDirection.Row;
      versionContainer.style.justifyContent = Justify.SpaceBetween;

      var stubVersionLabel = new Label($"Stub: {((VRCRotationConstraint)target).StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      stubVersionLabel.style.fontSize = new StyleLength(10);
      stubVersionLabel.style.color = new StyleColor(Color.gray);

      var uiVersionLabel = new Label($"UI: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      uiVersionLabel.style.fontSize = new StyleLength(10);
      uiVersionLabel.style.color = new StyleColor(Color.gray);

      versionContainer.Add(stubVersionLabel);
      versionContainer.Add(uiVersionLabel);

      root.Add(warningBox);
      root.Add(convertButton);
      root.Add(versionContainer);

      return root;
    }

    private void ConvertToUnityConstraint()
    {
      VRCRotationConstraint vrcConstraint = (VRCRotationConstraint)target;
      GameObject gameObject = vrcConstraint.gameObject;

      // Add Unity RotationConstraint component
      RotationConstraint unityConstraint = Undo.AddComponent<RotationConstraint>(gameObject);

      // Transfer basic properties
      unityConstraint.weight = vrcConstraint.GlobalWeight;
      unityConstraint.constraintActive = vrcConstraint.IsActive;

      // Set at rest rotation
      unityConstraint.rotationAtRest = vrcConstraint.RotationAtRest;

      // Set offset
      unityConstraint.rotationOffset = vrcConstraint.RotationOffset;

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
      unityConstraint.rotationAxis = 0;
      if (vrcConstraint.AffectsRotationX)
        unityConstraint.rotationAxis |= Axis.X;
      if (vrcConstraint.AffectsRotationY)
        unityConstraint.rotationAxis |= Axis.Y;
      if (vrcConstraint.AffectsRotationZ)
        unityConstraint.rotationAxis |= Axis.Z;

      // Remove VRC component after conversion
      Undo.DestroyObjectImmediate(vrcConstraint);

      EditorUtility.DisplayDialog("Conversion Complete", "Successfully converted to Unity RotationConstraint", "OK");
    }
  }
}
