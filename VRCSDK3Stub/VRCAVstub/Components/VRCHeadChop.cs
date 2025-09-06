using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace VRC.SDK3.Avatars.Components
{
  public class VRCHeadChop : MonoBehaviour
  {
    [Serializable]
    public class HeadChopBone
    {
      public enum ApplyCondition
      {
        AlwaysApply,
        VrOnly,
        NonVrOnly
      }

      [Tooltip("The bone transform to apply scaling to.")]
      public Transform transform;

      [Tooltip(
        "The scale factor to apply to this specific bone, ranging from 0 (bone fully scaled away) to 1"
          + " (bone uses its usual scale)."
      )]
      [Range(0f, 1f)]
      public float scaleFactor;

      [Tooltip("A condition controlling whether this bone will be scaled away.")]
      public ApplyCondition applyCondition;

      public Transform Transform => transform;

      public bool CanApply(bool isUserInVr)
      {
        switch (applyCondition)
        {
          case ApplyCondition.VrOnly:
            return isUserInVr;
          case ApplyCondition.NonVrOnly:
            return !isUserInVr;
          default:
            return true;
        }
      }

      public float GetDesiredScaleFactor()
      {
        return Mathf.Clamp01(scaleFactor);
      }
    }

    public struct HeadChopData
    {
      public float DesiredAppliedScaleFactor;
      public Vector3 OriginalLocalPosition;
      public Vector3 OriginalRootSpacePosition;
      public Vector3 OriginalLocalScale;
    }

    [Tooltip("Lists the bones that will be scaled away for the local player.")]
    public HeadChopBone[] targetBones = Array.Empty<HeadChopBone>();

    [Tooltip(
      "A global scale applied to all bones targeted by this component, ranging from 0 "
        + "(all bones fully scaled away) to 1 (all bones use their individual scale factors)."
    )]
    [Range(0f, 1f)]
    public float globalScaleFactor = 1f;

    private const int MaxBoneCount = 32;

    public const int MaxComponentCount = 16;
  }

  [CustomEditor(typeof(VRCHeadChop))]
  public class VRCHeadChopEditorStub : Editor
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
        "This component needs to be converted for use in CVR, please click the below button to run the conversion"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        var vrcHeadChop = (VRCHeadChop)target;
        var gameObject = vrcHeadChop.gameObject;

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
      convertButton.style.marginTop = new StyleLength(10);
      convertButton.style.height = new StyleLength(30);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }
  }
}
