using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles

namespace VRC.SDK3.Avatars.Editors
{
  /// <summary>
  /// Custom editor for VRCExpressionParameters that provides UI and CVRFury conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCExpressionParameters))]
  public class VRCExpressionParametersEditorStub : Editor
  {
    private const string ErrorDialogTitle = "Error";

    // CSS class name constants
    private const string CSS_STORES_CONTAINER = "stores-container";
    private const string CSS_STORES_HEADER = "stores-header";
    private const string CSS_CVR_FURY_BUTTON = "cvr-fury-button";
    private const string CSS_CVR_FURY_BUTTONS_CONTAINER = "cvr-fury-buttons-container";
    private const string CSS_PARAMETER_FIELD = "parameter-field";
    private const string PARAMETER_ITEM = "parameter-item";
    private const string CSS_EMPTY_SLOT = "empty-slot";
    private const string CSS_SECTION_HEADER = "section-header";
    private const string CSS_SPACER = "spacer";
    private const string CSS_INFO_BOX = "info-box";
    private const string STUB_VERSION = "stub-version";
    private const string UI_VERSION = "ui-version";

    private readonly List<RuntimeAnimatorController> selectedAnimators = new List<RuntimeAnimatorController>();

    private static void ApplyCVRFuryStyles(VisualElement root)
    {
      // Use shared stylesheet and class wiring
      SharedUIStyles.ApplySharedStyles(root);

      // Root container class
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      // Buttons
      var convertButtons = root.Query<Button>().Where(b => b.text.Contains("Convert")).ToList();
      foreach (var button in convertButtons)
      {
        button.AddToClassList(CSS_CVR_FURY_BUTTON);
      }

      // Stores/sections
      var storesContainers = root.Query<VisualElement>(className: CSS_STORES_CONTAINER).ToList();
      foreach (var container in storesContainers)
      {
        container.AddToClassList(CSS_STORES_CONTAINER);
      }

      var sectionHeaders = root.Query<Label>(className: CSS_SECTION_HEADER).ToList();
      foreach (var header in sectionHeaders)
      {
        header.AddToClassList(CSS_STORES_HEADER);
      }

      // Parameter fields
      var parameterFields = root.Query<ObjectField>().ToList();
      foreach (var field in parameterFields)
      {
        field.AddToClassList(CSS_PARAMETER_FIELD);
        if (field.value == null)
        {
          field.AddToClassList(CSS_EMPTY_SLOT);
        }
      }
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      var versionLabel = new Label($"Stub Version: {((VRCExpressionParameters)target).StubVersion}");
      versionLabel.AddToClassList(STUB_VERSION);
      root.Add(versionLabel);

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      // Show parameter count
      var vrcParameters = (VRCExpressionParameters)target;
      var paramCountLabel = new Label($"Parameters: {(vrcParameters.parameters?.Length ?? 0)}");
      paramCountLabel.style.marginBottom = new StyleLength(10);
      root.Add(paramCountLabel);

      // Show parameters list for preview
      if (vrcParameters.parameters != null && vrcParameters.parameters.Length > 0)
      {
        var parametersBox = new VisualElement();
        parametersBox.AddToClassList("parameters-list");

        var parametersLabel = new Label("Parameters List:");
        parametersLabel.AddToClassList(CSS_SECTION_HEADER);
        parametersBox.Add(parametersLabel);

        var scrollView = new ScrollView();
        scrollView.style.maxHeight = new StyleLength(200);

        for (int i = 0; i < vrcParameters.parameters.Length; i++)
        {
          var param = vrcParameters.parameters[i];
          var paramLabel = new Label($"• {param.name} ({param.valueType}) = {param.defaultValue}");
          paramLabel.AddToClassList(PARAMETER_ITEM);
          paramLabel.style.fontSize = new StyleLength(11);
          scrollView.Add(paramLabel);
        }

        parametersBox.Add(scrollView);
        root.Add(parametersBox);
      }

      // Animator selection section
      var animatorSection = new VisualElement();
      animatorSection.AddToClassList(CSS_STORES_CONTAINER);

      var animatorLabel = new Label("Animator Controllers to Link:");
      animatorLabel.AddToClassList(CSS_STORES_HEADER);
      animatorSection.Add(animatorLabel);

      var animatorHelpLabel = new Label(
        "Select RuntimeAnimatorController assets that should be linked to the converted CVRFury Parameters Store:"
      );
      animatorHelpLabel.style.fontSize = new StyleLength(11);
      animatorHelpLabel.style.whiteSpace = WhiteSpace.Normal;
      animatorHelpLabel.style.marginBottom = new StyleLength(10);
      animatorSection.Add(animatorHelpLabel);

      // Container for animator list
      var animatorContainer = new VisualElement();
      animatorSection.Add(animatorContainer);

      // Method to refresh the animator list display
      RefreshAnimatorList(animatorContainer);
      root.Add(animatorSection);

      // Add spacer
      var spacer = new VisualElement();
      spacer.AddToClassList(CSS_SPACER);
      root.Add(spacer);

      var infoBox = new Box();
      infoBox.AddToClassList(CSS_INFO_BOX);

      var infoLabel = new Label(
        "This VRCExpressionParameters file can be converted to CVRFury format. \n\n"
          + "Select any animator controllers above that should be linked to the converted asset, then click the button below to convert it directly."
      );
      infoLabel.style.whiteSpace = WhiteSpace.Normal;
      infoBox.Add(infoLabel);

      var convertButton = new Button(() =>
      {
        ConvertToCVRFury();
      })
      {
        text = "Convert to CVRFury Parameters Store"
      };
      convertButton.AddToClassList(CSS_CVR_FURY_BUTTON);

      root.Add(infoBox);
      root.Add(convertButton);
      root.Add(spacer);

      // Apply styling after all UI elements are created
      ApplyCVRFuryStyles(root);

      return root;
    }

    private void RefreshAnimatorList(VisualElement animatorContainer)
    {
      animatorContainer.Clear();

      for (int i = 0; i < selectedAnimators.Count; i++)
      {
        var index = i; // Capture for closure
        var animatorRow = new VisualElement();
        animatorRow.style.flexDirection = FlexDirection.Row;
        animatorRow.style.marginBottom = new StyleLength(2);

        var objectField = new ObjectField()
        {
          objectType = typeof(RuntimeAnimatorController),
          value = selectedAnimators[index]
        };
        objectField.AddToClassList(CSS_PARAMETER_FIELD);
        objectField.style.flexGrow = 1;

        objectField.RegisterValueChangedCallback(evt =>
        {
          selectedAnimators[index] = evt.newValue as RuntimeAnimatorController;
        });

        var removeButton = new Button(() =>
        {
          selectedAnimators.RemoveAt(index);
          RefreshAnimatorList(animatorContainer);
        })
        {
          text = "Remove"
        };
        removeButton.style.width = new StyleLength(60);
        removeButton.style.marginLeft = new StyleLength(5);

        animatorRow.Add(objectField);
        animatorRow.Add(removeButton);
        animatorContainer.Add(animatorRow);
      }

      // Add new animator button
      var addButton = new Button(() =>
      {
        selectedAnimators.Add(null);
        RefreshAnimatorList(animatorContainer);
      })
      {
        text = "Add Animator Controller"
      };
      addButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      animatorContainer.Add(addButton);

      // Show count
      var countLabel = new Label($"Animators selected: {selectedAnimators.Count}");
      countLabel.style.fontSize = new StyleLength(11);
      countLabel.style.marginTop = new StyleLength(5);
      animatorContainer.Add(countLabel);
    }

    private void ConvertToCVRFury()
    {
      var vrcParameters = (VRCExpressionParameters)target;
      var assetPath = AssetDatabase.GetAssetPath(vrcParameters);

      if (!ValidateAssetPath(assetPath) || !CheckParametersAndWarn())
      {
        return;
      }

      var outputPath = GenerateOutputPath(assetPath);
      if (!HandleExistingFile(outputPath))
      {
        return;
      }

      try
      {
        var cvrParametersStore = CreateCVRParametersStore(vrcParameters);
        if (cvrParametersStore == null)
        {
          return;
        }

        SaveAssetAndLinkAnimators(cvrParametersStore, outputPath);
        ShowSuccessDialog(vrcParameters, outputPath);
      }
      catch (System.Exception ex)
      {
        EditorUtility.DisplayDialog("Conversion Error", $"An error occurred during conversion:\n\n{ex.Message}", "OK");
        UnityEngine.Debug.LogError($"VRCExpressionParameters conversion error: {ex}");
      }
    }

    private static bool ValidateAssetPath(string assetPath)
    {
      if (string.IsNullOrEmpty(assetPath))
      {
        EditorUtility.DisplayDialog(
          ErrorDialogTitle,
          "Cannot convert unsaved asset. Please save the VRCExpressionParameters asset first.",
          "OK"
        );
        return false;
      }
      return true;
    }

    private bool ConfirmAnimatorSelection()
    {
      var validAnimatorsCheck = selectedAnimators.Where(a => a != null).ToList();
      if (
        validAnimatorsCheck.Count == 0
        && !EditorUtility.DisplayDialog(
          "No Animation Controllers",
          "No animation controllers have been selected to link with the converted CVRFury Parameters Store.\n\n"
            + "Animation controllers are typically needed to use the parameters in your avatar's animations.\n\n"
            + "Are you sure you want to continue without linking any animation controllers?",
          "Yes, Continue",
          "Cancel"
        )
      )
      {
        return false;
      }
      return true;
    }

    private bool CheckParametersAndWarn()
    {
      var vrcParameters = (VRCExpressionParameters)target;
      var issues = new List<string>();

      // Check if parameters array is null or empty
      if (vrcParameters.parameters == null || vrcParameters.parameters.Length == 0)
      {
        issues.Add("No parameters defined in this VRCExpressionParameters asset");
      }
      else
      {
        // Check for parameters with missing or empty names
        for (int i = 0; i < vrcParameters.parameters.Length; i++)
        {
          var param = vrcParameters.parameters[i];
          if (param == null)
          {
            issues.Add($"Parameter at index {i + 1} is null");
          }
          else if (string.IsNullOrEmpty(param.name) || string.IsNullOrWhiteSpace(param.name))
          {
            issues.Add($"Parameter at index {i + 1} has no name");
          }
        }
      }

      // Check for missing animator controllers
      var validAnimatorsCheck = selectedAnimators.Where(a => a != null).ToList();
      if (validAnimatorsCheck.Count == 0)
      {
        issues.Add("No animation controllers selected (parameters may not be usable)");
      }

      // If there are issues, show a warning dialog
      if (issues.Count > 0)
      {
        var messageBuilder = new StringBuilder("The following issues were found with the parameters:\n\n");
        foreach (var issue in issues)
        {
          messageBuilder.AppendLine($"• {issue}");
        }
        messageBuilder.AppendLine(
          "\nDo you want to continue with the conversion anyway? These issues may cause problems with the converted parameters store."
        );

        return EditorUtility.DisplayDialog(
          "Parameter Issues Warning",
          messageBuilder.ToString(),
          "Continue Conversion",
          "Cancel"
        );
      }

      return true; // No issues found, proceed
    }

    private static string GenerateOutputPath(string assetPath)
    {
      var filePathWithoutExtension = System.IO.Path.ChangeExtension(assetPath, null);
      return filePathWithoutExtension + ".CVRFury.asset";
    }

    private static bool HandleExistingFile(string outputPath)
    {
      if (System.IO.File.Exists(outputPath))
      {
        if (
          !EditorUtility.DisplayDialog(
            "File Exists",
            $"A converted file already exists at:\n{outputPath}\n\nDo you want to overwrite it?",
            "Yes",
            "Cancel"
          )
        )
        {
          return false;
        }

        AssetDatabase.DeleteAsset(outputPath);
        AssetDatabase.Refresh();
      }
      return true;
    }

    private static ScriptableObject CreateCVRParametersStore(VRCExpressionParameters vrcParameters)
    {
      var cvrParametersStoreType = FindCVRParametersStoreType();
      if (cvrParametersStoreType == null)
      {
        return null;
      }

      var cvrParametersStore = ScriptableObject.CreateInstance(cvrParametersStoreType);
      if (!ConvertParameters(vrcParameters, cvrParametersStore, cvrParametersStoreType))
      {
        return null;
      }

      return cvrParametersStore;
    }

    private static System.Type FindCVRParametersStoreType()
    {
      var cvrParametersStoreType = System.AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRFuryParametersStore");

      if (cvrParametersStoreType == null)
      {
        EditorUtility.DisplayDialog(
          ErrorDialogTitle,
          "CVRFuryParametersStore type not found. Make sure CVRFury is properly installed.",
          "OK"
        );
      }

      return cvrParametersStoreType;
    }

    private static bool ConvertParameters(
      VRCExpressionParameters vrcParameters,
      ScriptableObject cvrParametersStore,
      System.Type cvrParametersStoreType
    )
    {
      var parametersField = cvrParametersStoreType.GetField("parameters");
      if (parametersField == null)
      {
        EditorUtility.DisplayDialog(
          ErrorDialogTitle,
          "Could not find parameters field in CVRFuryParametersStore.",
          "OK"
        );
        return false;
      }

      var parameterType = cvrParametersStoreType.GetNestedType("Parameter");
      var valueTypeEnum = cvrParametersStoreType.GetNestedType("ValueType");

      if (parameterType == null || valueTypeEnum == null)
      {
        EditorUtility.DisplayDialog(
          ErrorDialogTitle,
          "Could not find Parameter type or ValueType enum in CVRFuryParametersStore.",
          "OK"
        );
        return false;
      }

      if (vrcParameters.parameters != null && vrcParameters.parameters.Length > 0)
      {
        var parameterArray = CreateParameterArray(vrcParameters.parameters, parameterType, valueTypeEnum);
        parametersField.SetValue(cvrParametersStore, parameterArray);
      }
      else
      {
        var emptyArray = System.Array.CreateInstance(parameterType, 0);
        parametersField.SetValue(cvrParametersStore, emptyArray);
      }

      return true;
    }

    private static System.Array CreateParameterArray(
      VRCExpressionParameters.Parameter[] vrcParameters,
      System.Type parameterType,
      System.Type valueTypeEnum
    )
    {
      var parameterArray = System.Array.CreateInstance(parameterType, vrcParameters.Length);

      for (int i = 0; i < vrcParameters.Length; i++)
      {
        var vrcParam = vrcParameters[i];
        var cvrParam = System.Activator.CreateInstance(parameterType);

        SetParameterFields(vrcParam, cvrParam, parameterType, valueTypeEnum);
        parameterArray.SetValue(cvrParam, i);
      }

      return parameterArray;
    }

    private static void SetParameterFields(
      VRCExpressionParameters.Parameter vrcParam,
      object cvrParam,
      System.Type parameterType,
      System.Type valueTypeEnum
    )
    {
      var nameField = parameterType.GetField("name");
      nameField?.SetValue(cvrParam, vrcParam.name);

      var valueTypeField = parameterType.GetField("valueType");
      if (valueTypeField != null)
      {
        var cvrValueType = ConvertValueType(vrcParam.valueType, valueTypeEnum);
        valueTypeField.SetValue(cvrParam, cvrValueType);
      }

      var defaultValueField = parameterType.GetField("defaultValue");
      defaultValueField?.SetValue(cvrParam, vrcParam.defaultValue);
    }

    private static object ConvertValueType(VRCExpressionParameters.ValueType vrcValueType, System.Type valueTypeEnum)
    {
      switch (vrcValueType)
      {
        case VRCExpressionParameters.ValueType.Bool:
          return System.Enum.Parse(valueTypeEnum, "Bool");
        case VRCExpressionParameters.ValueType.Float:
          return System.Enum.Parse(valueTypeEnum, "Float");
        case VRCExpressionParameters.ValueType.Int:
          return System.Enum.Parse(valueTypeEnum, "Int");
        default:
          return System.Enum.Parse(valueTypeEnum, "Float");
      }
    }

    private void SaveAssetAndLinkAnimators(ScriptableObject cvrParametersStore, string outputPath)
    {
      AssetDatabase.CreateAsset(cvrParametersStore, outputPath);
      LinkAnimatorsToStore(cvrParametersStore);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
    }

    private void LinkAnimatorsToStore(ScriptableObject cvrParametersStore)
    {
      if (selectedAnimators.Count <= 0)
      {
        return;
      }

      var validAnimators = selectedAnimators.Where(a => a != null).ToList();
      if (validAnimators.Count <= 0)
      {
        return;
      }

      var cvrParametersStoreType = cvrParametersStore.GetType();
      var relatedAnimationControllersField = cvrParametersStoreType.GetField("relatedAnimationControllers");
      if (relatedAnimationControllersField == null)
      {
        return;
      }

      var listType = typeof(List<>).MakeGenericType(typeof(RuntimeAnimatorController));
      var animatorList = System.Activator.CreateInstance(listType);
      var addMethod = listType.GetMethod("Add");

      foreach (var animator in validAnimators)
      {
        addMethod.Invoke(animatorList, new object[] { animator });
      }

      relatedAnimationControllersField.SetValue(cvrParametersStore, animatorList);
    }

    private void ShowSuccessDialog(VRCExpressionParameters vrcParameters, string outputPath)
    {
      var paramCount = vrcParameters.parameters?.Length ?? 0;
      var validAnimatorCount = selectedAnimators.Count(a => a != null);
      var message = BuildSuccessMessage(paramCount, validAnimatorCount);

      EditorUtility.DisplayDialog("Conversion Complete", $"{message}\n\nOutput file: {outputPath}", "OK");

      var newAsset = AssetDatabase.LoadAssetAtPath(outputPath, typeof(ScriptableObject));
      if (newAsset != null)
      {
        EditorGUIUtility.PingObject(newAsset);
        Selection.activeObject = newAsset;
      }
    }

    private static string BuildSuccessMessage(int paramCount, int validAnimatorCount)
    {
      if (paramCount == 0 && validAnimatorCount == 0)
      {
        return "Conversion completed successfully!\n\nAn empty CVRFury Parameters Store has been created.";
      }

      var parts = new List<string>();
      if (paramCount > 0)
      {
        parts.Add($"{paramCount} parameters have been converted");
      }
      if (validAnimatorCount > 0)
      {
        parts.Add($"{validAnimatorCount} animator controllers have been linked");
      }
      return $"Conversion completed successfully!\n\n{string.Join(" and ", parts)} to CVRFury format.";
    }
  }
}
