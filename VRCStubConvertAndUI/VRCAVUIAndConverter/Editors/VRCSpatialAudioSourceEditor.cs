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
  /// Custom editor for VRCSpatialAudioSource that provides UI and removal functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCSpatialAudioSource))]
  public class VRCSpatialAudioSourceEditor : Editor
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
          UnityEngine.Debug.Log("CVRFury embedded stylesheet loaded successfully for VRCSpatialAudioSource editor");
        }
        else
        {
          // Fallback: Apply basic styling directly via code
          ApplyFallbackStyling(root);
          UnityEngine.Debug.LogWarning("CVRFury embedded stylesheet not found, using fallback styling");
        }

        // Apply CSS classes to elements
        root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

        var removeButtons = root.Query<Button>().Where(b => b.text.Contains("Remove")).ToList();
        foreach (var button in removeButtons)
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

      var stubVersionLabel = new Label($"Stub Version: {((VRCSpatialAudioSource)target).StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      root.Add(stubVersionLabel);

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      var warningBox = new Box();
      warningBox.AddToClassList(CSS_INFO_BOX);

      var warningLabel = new Label(
        "This component is not compatible for data import to CVRFury and should be removed, please click the below button to do so"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var removeButton = new Button(() =>
      {
        DestroyImmediate(target);
      })
      {
        text = "Remove VRCSpatialAudioSource Component"
      };
      removeButton.AddToClassList(CSS_CVR_FURY_BUTTON);

      var spacer = new VisualElement();
      spacer.AddToClassList(CSS_SPACER);

      root.Add(warningBox);
      root.Add(removeButton);
      root.Add(spacer);

      // Apply styling after all UI elements are created
      ApplyCVRFuryStyles(root);

      return root;
    }
  }
}
