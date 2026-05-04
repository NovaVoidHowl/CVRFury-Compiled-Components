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

    /// <summary>
    /// Converts VRCHeadChop component to CVR FPRExclusion components
    /// </summary>
    private void ConvertToFPRExclusion()
    {
      var vrcHeadChop = (VRCHeadChop)target;
      var fprExclusionType = FindFPRExclusionType();

      if (fprExclusionType != null)
      {
        ConvertTargetBones(vrcHeadChop, fprExclusionType);
        DestroyImmediate(vrcHeadChop);
        ShowConversionSuccessDialog(vrcHeadChop.targetBones.Length);
      }
      else
      {
        ShowConversionFailedDialog();
      }
    }

    /// <summary>
    /// Finds the FPRExclusion component type from loaded assemblies
    /// </summary>
    /// <returns>The FPRExclusion type if found, null otherwise</returns>
    private static Type FindFPRExclusionType()
    {
      return AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "FPRExclusion");
    }

    /// <summary>
    /// Converts each target bone to individual FPRExclusion components
    /// </summary>
    /// <param name="vrcHeadChop">The VRCHeadChop component to convert from</param>
    /// <param name="fprExclusionType">The FPRExclusion component type</param>
    private static void ConvertTargetBones(VRCHeadChop vrcHeadChop, Type fprExclusionType)
    {
      foreach (var targetBone in vrcHeadChop.targetBones)
      {
        if (ShouldSkipTargetBone(targetBone, fprExclusionType))
          continue;

        var fprComponent = targetBone.transform.gameObject.AddComponent(fprExclusionType);
        ConfigureFPRComponent(fprComponent, targetBone, vrcHeadChop, fprExclusionType);
      }
    }

    /// <summary>
    /// Determines if a target bone should be skipped during conversion
    /// </summary>
    /// <param name="targetBone">The target bone to check</param>
    /// <param name="fprExclusionType">The FPRExclusion component type</param>
    /// <returns>True if the bone should be skipped, false otherwise</returns>
    private static bool ShouldSkipTargetBone(VRCHeadChop.HeadChopBone targetBone, Type fprExclusionType)
    {
      return targetBone?.transform == null || targetBone.transform.GetComponent(fprExclusionType) != null;
    }

    /// <summary>
    /// Configures the FPRExclusion component with mapped properties from VRCHeadChop
    /// </summary>
    /// <param name="fprComponent">The FPRExclusion component to configure</param>
    /// <param name="targetBone">The source target bone</param>
    /// <param name="vrcHeadChop">The source VRCHeadChop component</param>
    /// <param name="fprExclusionType">The FPRExclusion component type</param>
    private static void ConfigureFPRComponent(
      Component fprComponent,
      VRCHeadChop.HeadChopBone targetBone,
      VRCHeadChop vrcHeadChop,
      Type fprExclusionType
    )
    {
      var isShownField = fprExclusionType.GetField("isShown");
      var targetField = fprExclusionType.GetField("target");
      var shrinkToZeroField = fprExclusionType.GetField("shrinkToZero");

      if (isShownField != null)
      {
        var effectiveScaleFactor = targetBone.scaleFactor * vrcHeadChop.globalScaleFactor;
        var shouldShow = effectiveScaleFactor > 0.1f; // Show if scale > 10%
        isShownField.SetValue(fprComponent, shouldShow);
      }

      if (targetField != null)
      {
        targetField.SetValue(fprComponent, targetBone.transform);
      }

      if (shrinkToZeroField != null)
      {
        shrinkToZeroField.SetValue(fprComponent, true);
      }
    }

    /// <summary>
    /// Shows the conversion success dialog
    /// </summary>
    /// <param name="convertedCount">Number of components converted</param>
    private static void ShowConversionSuccessDialog(int convertedCount)
    {
      EditorUtility.DisplayDialog(
        "Conversion Complete",
        $"Successfully converted VRCHeadChop to {convertedCount} FPRExclusion component(s). "
          + "Note: FPRExclusion works differently than VRCHeadChop - each bone gets its own component. "
          + "You may need to adjust the settings manually for fine-tuning.",
        "OK"
      );
    }

    /// <summary>
    /// Shows the conversion failed dialog
    /// </summary>
    private static void ShowConversionFailedDialog()
    {
      EditorUtility.DisplayDialog(
        "Conversion Failed",
        "FPRExclusion component not found. Make sure CVR CCK is properly imported.",
        "OK"
      );
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

      var convertButton = new Button(ConvertToFPRExclusion) { text = "Convert to CVR FPR Exclusion Components" };
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
