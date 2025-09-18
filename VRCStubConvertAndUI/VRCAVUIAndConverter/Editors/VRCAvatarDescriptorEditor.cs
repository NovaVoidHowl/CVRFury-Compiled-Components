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
  /// Custom editor for VRCAvatarDescriptor that provides UI and CVR conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCAvatarDescriptor))]
  public class VRCAvatarDescriptorEditor : Editor
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
    /// Converts a VRC Avatar Descriptor to CVR Avatar Descriptor with all necessary component mappings.
    /// </summary>
    private void ConvertVRCAvatarToCVR()
    {
      var vrcAvatar = (VRCAvatarDescriptor)target;
      var avatar = vrcAvatar.gameObject;

      var (cvrAvatarType, colliderInfoType) = FindRequiredComponentTypes();

      if (cvrAvatarType != null && colliderInfoType != null)
      {
        var cvrComponent = avatar.AddComponent(cvrAvatarType);
        var colliderComponent = avatar.AddComponent(colliderInfoType);

        CopyBasicProperties(vrcAvatar, cvrComponent, cvrAvatarType);
        CopyEyeBlinkSettings(vrcAvatar, cvrComponent, cvrAvatarType);
        CopyVisemeLipsyncSettings(vrcAvatar, cvrComponent, cvrAvatarType);
        CopyColliderConfigs(vrcAvatar, colliderComponent, colliderInfoType);

        ShowConversionCompleteDialog();
        DestroyImmediate(vrcAvatar);
      }
      else
      {
        Debug.LogError("Could not find required component types. Make sure CVR SDK is imported.");
      }
    }

    /// <summary>
    /// Finds the required CVR component types using reflection.
    /// </summary>
    private static (Type cvrAvatarType, Type colliderInfoType) FindRequiredComponentTypes()
    {
      var cvrAvatarType = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRAvatar");

      var colliderInfoType = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRFuryAvatarColliderInfoUnit");

      return (cvrAvatarType, colliderInfoType);
    }

    /// <summary>
    /// Copies basic properties like view position and body mesh from VRC to CVR component.
    /// </summary>
    private static void CopyBasicProperties(VRCAvatarDescriptor vrcAvatar, object cvrComponent, Type cvrAvatarType)
    {
      SetFieldOrProperty(cvrAvatarType, cvrComponent, "viewPosition", vrcAvatar.ViewPosition);
      SetFieldOrProperty(cvrAvatarType, cvrComponent, "bodyMesh", vrcAvatar.VisemeSkinnedMesh);
    }

    /// <summary>
    /// Sets a field or property value on an object using reflection.
    /// </summary>
    private static void SetFieldOrProperty(Type type, object instance, string memberName, object value)
    {
      var field = type.GetField(memberName);
      if (field != null)
      {
        field.SetValue(instance, value);
        return;
      }

      var property = type.GetProperty(memberName);
      if (property != null)
      {
        property.SetValue(instance, value, null);
      }
    }

    /// <summary>
    /// Copies eye blink settings from VRC to CVR component.
    /// </summary>
    private static void CopyEyeBlinkSettings(VRCAvatarDescriptor vrcAvatar, object cvrComponent, Type cvrAvatarType)
    {
      if (!HasValidBlinkSettings(vrcAvatar))
        return;

      var blinkIndex = vrcAvatar.customEyeLookSettings.eyelidsBlendshapes[0];
      var mesh = vrcAvatar.VisemeSkinnedMesh.sharedMesh;

      if (!IsValidBlinkIndex(blinkIndex, mesh))
        return;

      var blinkBlendshapeName = mesh.GetBlendShapeName(blinkIndex);
      SetFieldOrProperty(cvrAvatarType, cvrComponent, "useBlinkBlendshapes", true);

      var blinkArrayField = cvrAvatarType.GetField("blinkBlendshape");
      if (blinkArrayField != null)
      {
        var blinkArray = CreateBlinkArray(blinkBlendshapeName);
        blinkArrayField.SetValue(cvrComponent, blinkArray);
      }
    }

    /// <summary>
    /// Checks if the VRC avatar has valid blink settings.
    /// </summary>
    private static bool HasValidBlinkSettings(VRCAvatarDescriptor vrcAvatar)
    {
      return vrcAvatar.customEyeLookSettings.eyelidsBlendshapes != null
        && vrcAvatar.customEyeLookSettings.eyelidsBlendshapes.Length > 0
        && vrcAvatar.VisemeSkinnedMesh != null
        && vrcAvatar.VisemeSkinnedMesh.sharedMesh != null;
    }

    /// <summary>
    /// Validates if the blink index is within valid range.
    /// </summary>
    private static bool IsValidBlinkIndex(int blinkIndex, Mesh mesh)
    {
      if (blinkIndex < 0)
      {
        Debug.Log("No blink blend shape is set in the VRC Avatar Descriptor. Skipping eye blink setup.");
        return false;
      }

      if (blinkIndex >= mesh.blendShapeCount)
      {
        Debug.LogWarning(
          $"Blend shape index {blinkIndex} is out of range. Mesh has {mesh.blendShapeCount} blend shapes. Skipping eye blink setup."
        );
        return false;
      }

      return true;
    }

    /// <summary>
    /// Creates a blink array with the specified blend shape name.
    /// </summary>
    private static Array CreateBlinkArray(string blinkBlendshapeName)
    {
      var blinkArray = Array.CreateInstance(typeof(string), 4);
      blinkArray.SetValue(blinkBlendshapeName, 0);
      blinkArray.SetValue("", 1);
      blinkArray.SetValue("", 2);
      blinkArray.SetValue("", 3);
      return blinkArray;
    }

    /// <summary>
    /// Copies viseme lipsync settings from VRC to CVR component.
    /// </summary>
    private static void CopyVisemeLipsyncSettings(
      VRCAvatarDescriptor vrcAvatar,
      object cvrComponent,
      Type cvrAvatarType
    )
    {
      if (vrcAvatar.VisemeBlendShapes == null || vrcAvatar.VisemeBlendShapes.Length == 0)
        return;

      SetFieldOrProperty(cvrAvatarType, cvrComponent, "useVisemeLipsync", true);

      if (vrcAvatar.VisemeBlendShapes.Length == 15)
      {
        var visemeArrayField = cvrAvatarType.GetField("visemeBlendshapes");
        if (visemeArrayField != null)
        {
          var visemeArray = CreateVisemeArray(vrcAvatar.VisemeBlendShapes);
          visemeArrayField.SetValue(cvrComponent, visemeArray);
        }
      }
    }

    /// <summary>
    /// Creates a viseme array from the VRC viseme blend shapes.
    /// </summary>
    private static Array CreateVisemeArray(string[] vrcVisemes)
    {
      var visemeArray = Array.CreateInstance(typeof(string), 15);
      for (int i = 0; i < 15; i++)
      {
        visemeArray.SetValue(vrcVisemes[i], i);
      }
      return visemeArray;
    }

    /// <summary>
    /// Copies collider configurations from VRC to CVR component.
    /// </summary>
    private static void CopyColliderConfigs(
      VRCAvatarDescriptor vrcAvatar,
      object colliderComponent,
      Type colliderInfoType
    )
    {
      var sourceFields = typeof(VRCAvatarDescriptor)
        .GetFields()
        .Where(f => f.FieldType == typeof(VRCAvatarDescriptor.ColliderConfig));

      foreach (var sourceField in sourceFields)
      {
        CopyColliderConfig(vrcAvatar, colliderComponent, colliderInfoType, sourceField);
      }
    }

    /// <summary>
    /// Copies a single collider configuration field.
    /// </summary>
    private static void CopyColliderConfig(
      VRCAvatarDescriptor vrcAvatar,
      object colliderComponent,
      Type colliderInfoType,
      System.Reflection.FieldInfo sourceField
    )
    {
      var targetField = colliderInfoType.GetField(sourceField.Name);
      if (targetField == null)
        return;

      var sourceValue = (VRCAvatarDescriptor.ColliderConfig)sourceField.GetValue(vrcAvatar);
      var targetValue = CreateTargetColliderConfig(sourceValue, targetField.FieldType);
      targetField.SetValue(colliderComponent, targetValue);
    }

    /// <summary>
    /// Creates a target collider config by copying properties from source.
    /// </summary>
    private static object CreateTargetColliderConfig(
      VRCAvatarDescriptor.ColliderConfig sourceValue,
      Type targetConfigType
    )
    {
      var targetValue = Activator.CreateInstance(targetConfigType);

      foreach (var prop in typeof(VRCAvatarDescriptor.ColliderConfig).GetFields())
      {
        var targetProp = targetConfigType.GetField(prop.Name);
        if (targetProp == null)
          continue;

        var value = prop.GetValue(sourceValue);

        if (prop.FieldType.IsEnum && targetProp.FieldType.IsEnum)
        {
          int enumValue = (int)value;
          value = Enum.ToObject(targetProp.FieldType, enumValue);
        }

        targetProp.SetValue(targetValue, value);
      }

      return targetValue;
    }

    /// <summary>
    /// Shows the conversion complete dialog to the user.
    /// </summary>
    private static void ShowConversionCompleteDialog()
    {
      EditorUtility.DisplayDialog(
        "Avatar Component Conversion Notice",
        "The conversion process is complete, but please note that the voice position must be set manually as this data is not available in the VRChat avatar component.",
        "OK"
      );
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      var stubVersionLabel = new Label($"Stub Version: {((VRCAvatarDescriptor)target).StubVersion}");
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

      var convertButton = new Button(ConvertVRCAvatarToCVR) { text = "Convert to Chillout VR Avatar Descriptor" };
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
