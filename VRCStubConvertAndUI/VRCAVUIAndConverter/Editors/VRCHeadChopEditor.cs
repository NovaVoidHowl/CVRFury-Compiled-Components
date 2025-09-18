using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles

namespace VRC.SDK3.Avatars.Editors
{
  /// <summary>
  /// Custom editor for VRCHeadChop that provides UI and CVR conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCHeadChop))]
  public class VRCHeadChopEditor : Editor
  {
    // CSS class name constants
    private const string CSS_CVR_FURY_BUTTONS_CONTAINER = "cvr-fury-buttons-container";
    private const string CSS_CVR_FURY_BUTTON = "cvr-fury-button";
    private const string CSS_INFO_BOX = "info-box";
    private const string CSS_SPACER = "spacer";
    private const string STUB_VERSION = "stub-version";
    private const string UI_VERSION = "ui-version";

    private static void ApplyCVRFuryStyles(VisualElement root)
    {
      SharedUIStyles.ApplySharedStyles(root);
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);
      var convertButtons = root.Query<Button>().Where(b => b.text.Contains("Convert")).ToList();
      foreach (var button in convertButtons)
        button.AddToClassList(CSS_CVR_FURY_BUTTON);
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      var stubVersionLabel = new Label($"Stub Version: {((VRCHeadChop)target).StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      root.Add(stubVersionLabel);

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      var warningBox = new Box();
      warningBox.AddToClassList(CSS_INFO_BOX);

      var warningLabel = new Label(
        "This component needs to be converted for use in CVR, please click the below button to run the conversion"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        var vrcHeadChop = (VRCHeadChop)target;

        // Find FPRExclusion component type
        var fprExclusionType = AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "FPRExclusion");

        if (fprExclusionType != null)
        {
          // Convert each target bone to individual FPRExclusion components
          foreach (var targetBone in vrcHeadChop.targetBones)
          {
            if (targetBone?.transform == null)
              continue;

            // Check if FPRExclusion already exists on this transform
            var existingComponent = targetBone.transform.GetComponent(fprExclusionType);
            if (existingComponent != null)
              continue;

            // Add FPRExclusion component to the target transform
            var fprComponent = targetBone.transform.gameObject.AddComponent(fprExclusionType);

            // Map VRCHeadChop properties to FPRExclusion properties
            var isShownField = fprExclusionType.GetField("isShown");
            var targetField = fprExclusionType.GetField("target");
            var shrinkToZeroField = fprExclusionType.GetField("shrinkToZero");

            if (isShownField != null)
            {
              // VRCHeadChop scaleFactor > 0 means visible, scaleFactor = 0 means hidden
              // FPRExclusion isShown = true means visible in first person
              var effectiveScaleFactor = targetBone.scaleFactor * vrcHeadChop.globalScaleFactor;
              var shouldShow = effectiveScaleFactor > 0.1f; // Show if scale > 10%
              isShownField.SetValue(fprComponent, shouldShow);
            }

            if (targetField != null)
            {
              // Set the target to the transform itself
              targetField.SetValue(fprComponent, targetBone.transform);
            }

            if (shrinkToZeroField != null)
            {
              // Use shrinkToZero mode (scale to zero) rather than Cut mode
              shrinkToZeroField.SetValue(fprComponent, true);
            }
          }

          // Remove the VRCHeadChop component
          DestroyImmediate(vrcHeadChop);

          EditorUtility.DisplayDialog(
            "Conversion Complete",
            $"Successfully converted VRCHeadChop to {vrcHeadChop.targetBones.Length} FPRExclusion component(s). "
              + "Note: FPRExclusion works differently than VRCHeadChop - each bone gets its own component. "
              + "You may need to adjust the settings manually for fine-tuning.",
            "OK"
          );
        }
        else
        {
          EditorUtility.DisplayDialog(
            "Conversion Failed",
            "FPRExclusion component not found. Make sure CVR CCK is properly imported.",
            "OK"
          );
        }
      })
      {
        text = "Convert to CVR FPR Exclusion Components"
      };
      convertButton.AddToClassList(CSS_CVR_FURY_BUTTON);

      var spacer = new VisualElement();
      spacer.AddToClassList(CSS_SPACER);

      root.Add(warningBox);
      root.Add(convertButton);
      root.Add(spacer);

      // Apply styling after all UI elements are created
      ApplyCVRFuryStyles(root);

      return root;
    }
  }
}
