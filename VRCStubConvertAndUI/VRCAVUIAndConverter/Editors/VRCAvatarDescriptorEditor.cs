using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.Editors.Common;

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
      try
      {
        // First try to load the embedded USS file
        var styleSheet = LoadEmbeddedStyleSheet();
        if (styleSheet != null)
        {
          root.styleSheets.Add(styleSheet);
          UnityEngine.Debug.Log("CVRFury embedded stylesheet loaded successfully for VRCAvatarDescriptor editor");
        }
        else
        {
          // Fallback: Apply basic styling directly via code
          ApplyFallbackStyling(root);
          UnityEngine.Debug.LogWarning("CVRFury embedded stylesheet not found, using fallback styling");
        }

        // Apply CSS classes to elements
        root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

        var convertButtons = root.Query<Button>().Where(b => b.text.Contains("Convert")).ToList();
        foreach (var button in convertButtons)
        {
          button.AddToClassList(CSS_CVR_FURY_BUTTON);
        }
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"Error applying CVRFury styles: {ex.Message}");
        ApplyFallbackStyling(root);
      }
    }

    private static StyleSheet LoadEmbeddedStyleSheet()
    {
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "VRC.SDK3.Avatars.Editors.Resources.VRCAVUIAndConverter.uss";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
          if (stream == null)
          {
            UnityEngine.Debug.LogWarning($"Embedded resource '{resourceName}' not found in assembly");
            return null;
          }

          using (var reader = new StreamReader(stream))
          {
            var cssContent = reader.ReadToEnd();

            // Create a temporary USS file in the project for Unity to load
            var assetsPath = "Assets/Temp";
            var tempUssPath = "Assets/Temp/CVRFuryStubs_Runtime.uss";

            if (!Directory.Exists(assetsPath))
            {
              Directory.CreateDirectory(assetsPath);
            }

            File.WriteAllText(tempUssPath, cssContent);
            AssetDatabase.ImportAsset(tempUssPath);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(tempUssPath);
            return styleSheet;
          }
        }
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"Error loading embedded stylesheet: {ex.Message}");
        return null;
      }
    }

    private static void ApplyFallbackStyling(VisualElement root)
    {
      // Basic fallback styling when USS file is not available
      root.style.paddingTop = new StyleLength(10);
      root.style.paddingBottom = new StyleLength(10);
      root.style.paddingLeft = new StyleLength(10);
      root.style.paddingRight = new StyleLength(10);
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

      var convertButton = new Button(() =>
      {
        var vrcAvatar = (VRCAvatarDescriptor)target;
        var avatar = vrcAvatar.gameObject;

        // Find required component types
        var cvrAvatarType = AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "CVRAvatar");
        var colliderInfoType = AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "CVRFuryAvatarColliderInfoUnit");

        if (cvrAvatarType != null && colliderInfoType != null)
        {
          // Add components
          var cvrComponent = avatar.AddComponent(cvrAvatarType);
          var colliderComponent = avatar.AddComponent(colliderInfoType);

          // Copy view position and body mesh
          var viewPositionField = cvrAvatarType.GetField("viewPosition");
          if (viewPositionField != null)
          {
            viewPositionField.SetValue(cvrComponent, vrcAvatar.ViewPosition);
          }
          else
          {
            var viewPositionProperty = cvrAvatarType.GetProperty("viewPosition");
            if (viewPositionProperty != null)
            {
              viewPositionProperty.SetValue(cvrComponent, vrcAvatar.ViewPosition, null);
            }
          }

          // Copy body mesh
          var bodyMeshField = cvrAvatarType.GetField("bodyMesh");
          if (bodyMeshField != null)
          {
            bodyMeshField.SetValue(cvrComponent, vrcAvatar.VisemeSkinnedMesh);
          }
          else
          {
            var bodyMeshProperty = cvrAvatarType.GetProperty("bodyMesh");
            if (bodyMeshProperty != null)
            {
              bodyMeshProperty.SetValue(cvrComponent, vrcAvatar.VisemeSkinnedMesh, null);
            }
          }

          // Copy eye blink settings if available
          if (
            vrcAvatar.customEyeLookSettings.eyelidsBlendshapes != null
            && vrcAvatar.customEyeLookSettings.eyelidsBlendshapes.Length > 0
            && vrcAvatar.VisemeSkinnedMesh != null
            && vrcAvatar.VisemeSkinnedMesh.sharedMesh != null
          )
          {
            var blinkIndex = vrcAvatar.customEyeLookSettings.eyelidsBlendshapes[0];

            // Validate blend shape index is within bounds
            var mesh = vrcAvatar.VisemeSkinnedMesh.sharedMesh;
            if (blinkIndex < 0)
            {
              Debug.Log("No blink blend shape is set in the VRC Avatar Descriptor. Skipping eye blink setup.");
            }
            else if (blinkIndex >= mesh.blendShapeCount)
            {
              Debug.LogWarning(
                $"Blend shape index {blinkIndex} is out of range. Mesh has {mesh.blendShapeCount} blend shapes. Skipping eye blink setup."
              );
            }
            else
            {
              var blinkBlendshapeName = mesh.GetBlendShapeName(blinkIndex);

              var useBlinkField = cvrAvatarType.GetField("useBlinkBlendshapes");
              if (useBlinkField != null)
              {
                useBlinkField.SetValue(cvrComponent, true);
              }

              var blinkArrayField = cvrAvatarType.GetField("blinkBlendshape");
              if (blinkArrayField != null)
              {
                // Create array with single value if it's a string array
                var blinkArray = Array.CreateInstance(typeof(string), 4);
                blinkArray.SetValue(blinkBlendshapeName, 0);
                // add the other 3 empty slots
                blinkArray.SetValue("", 1);
                blinkArray.SetValue("", 2);
                blinkArray.SetValue("", 3);
                blinkArrayField.SetValue(cvrComponent, blinkArray);
              }
            }
          }

          // Set viseme lipsync if available
          if (vrcAvatar.VisemeBlendShapes != null && vrcAvatar.VisemeBlendShapes.Length > 0)
          {
            var useVisemeField = cvrAvatarType.GetField("useVisemeLipsync");
            if (useVisemeField != null)
            {
              useVisemeField.SetValue(cvrComponent, true);

              // If we have exactly 15 visemes, copy them over
              if (vrcAvatar.VisemeBlendShapes.Length == 15)
              {
                var visemeArrayField = cvrAvatarType.GetField("visemeBlendshapes");
                if (visemeArrayField != null)
                {
                  var visemeArray = Array.CreateInstance(typeof(string), 15);
                  for (int i = 0; i < 15; i++)
                  {
                    visemeArray.SetValue(vrcAvatar.VisemeBlendShapes[i], i);
                  }
                  visemeArrayField.SetValue(cvrComponent, visemeArray);
                }
              }
            }
          }

          // Copy collider configs
          var sourceFields = typeof(VRCAvatarDescriptor)
            .GetFields()
            .Where(f => f.FieldType == typeof(VRCAvatarDescriptor.ColliderConfig));

          foreach (var sourceField in sourceFields)
          {
            var targetField = colliderInfoType.GetField(sourceField.Name);
            if (targetField != null)
            {
              // Get source value
              var sourceValue = (VRCAvatarDescriptor.ColliderConfig)sourceField.GetValue(vrcAvatar);

              // Create target ColliderConfig type
              var targetConfigType = targetField.FieldType;
              var targetValue = Activator.CreateInstance(targetConfigType);

              // Copy properties
              foreach (var prop in typeof(VRCAvatarDescriptor.ColliderConfig).GetFields())
              {
                var targetProp = targetConfigType.GetField(prop.Name);
                if (targetProp != null)
                {
                  var value = prop.GetValue(sourceValue);

                  // Special handling for enum types
                  if (prop.FieldType.IsEnum && targetProp.FieldType.IsEnum)
                  {
                    // Convert enum to its underlying value then create new enum of target type
                    int enumValue = (int)value;
                    value = Enum.ToObject(targetProp.FieldType, enumValue);
                  }

                  targetProp.SetValue(targetValue, value);
                }
              }

              // Set the converted value
              targetField.SetValue(colliderComponent, targetValue);
            }
          }

          // Show warning about voice position
          EditorUtility.DisplayDialog(
            "Avatar Component Conversion Notice",
            "The conversion process is complete, but please note that the voice position must be set manually as this data is not available in the VRChat avatar component.",
            "OK"
          );

          // Remove this component after everything is copied
          DestroyImmediate(vrcAvatar);
        }
        else
        {
          Debug.LogError("Could not find required component types. Make sure CVR SDK is imported.");
        }
      })
      {
        text = "Convert to Chillout VR Avatar Descriptor"
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
