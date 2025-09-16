using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion;

namespace VRC.SDK3.Avatars.ScriptableObjects
{
  public class VRCExpressionParameters : ScriptableObject
  {
    public const int MAX_PARAMETER_COST = 256;
    public Parameter[] parameters;

    [System.Serializable]
    public enum ValueType
    {
      Int = 0,
      Float = 1,
      Bool = 2
    }

    [System.Serializable]
    public class Parameter
    {
      public string name;
      public ValueType valueType;
      public bool saved;
      public float defaultValue;
    }

    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      if (string.IsNullOrEmpty(_stubVersion))
      {
        _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion;
      }
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion; }
      private set { _stubVersion = value; }
    }

    public static Version GetStubVersion()
    {
      return uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.AsVersion;
    }
  }

  [CustomEditor(typeof(VRCExpressionParameters))]
  public class VRCExpressionParametersEditorStub : Editor
  {
    private const string ErrorDialogTitle = "Error";
    private readonly List<RuntimeAnimatorController> selectedAnimators = new List<RuntimeAnimatorController>();

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var versionLabel = new Label($"Stub Version: {((VRCExpressionParameters)target).StubVersion}");
      versionLabel.style.marginBottom = new StyleLength(10);
      root.Add(versionLabel);

      // Show parameter count
      var vrcParameters = (VRCExpressionParameters)target;
      var paramCountLabel = new Label($"Parameters: {(vrcParameters.parameters?.Length ?? 0)}");
      paramCountLabel.style.marginBottom = new StyleLength(10);
      root.Add(paramCountLabel);

      // Show parameters list for preview
      if (vrcParameters.parameters != null && vrcParameters.parameters.Length > 0)
      {
        var parametersBox = new Box();
        parametersBox.style.marginBottom = new StyleLength(10);
        parametersBox.style.paddingTop = new StyleLength(6);
        parametersBox.style.paddingBottom = new StyleLength(6);
        parametersBox.style.paddingLeft = new StyleLength(6);
        parametersBox.style.paddingRight = new StyleLength(6);
        parametersBox.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 1f, 0.2f));

        var parametersLabel = new Label("Parameters List:");
        parametersLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        parametersBox.Add(parametersLabel);

        var scrollView = new ScrollView();
        scrollView.style.maxHeight = new StyleLength(200);

        for (int i = 0; i < vrcParameters.parameters.Length; i++)
        {
          var param = vrcParameters.parameters[i];
          var paramLabel = new Label($"• {param.name} ({param.valueType}) = {param.defaultValue}");
          paramLabel.style.fontSize = new StyleLength(11);
          scrollView.Add(paramLabel);
        }

        parametersBox.Add(scrollView);
        root.Add(parametersBox);
      }

      // Animator selection section
      var animatorSection = new Box();
      animatorSection.style.marginTop = new StyleLength(10);
      animatorSection.style.marginBottom = new StyleLength(10);
      animatorSection.style.paddingTop = new StyleLength(6);
      animatorSection.style.paddingBottom = new StyleLength(6);
      animatorSection.style.paddingLeft = new StyleLength(6);
      animatorSection.style.paddingRight = new StyleLength(6);
      animatorSection.style.backgroundColor = new StyleColor(new Color(0.8f, 1f, 0.8f, 0.2f));

      var animatorLabel = new Label("Animator Controllers to Link:");
      animatorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      animatorLabel.style.marginBottom = new StyleLength(5);
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

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This VRCExpressionParameters file can be converted to CVRFury format. "
          + "Select any animator controllers above that should be linked to the converted asset, then click the button below to convert it directly."
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        ConvertToCVRFury();
      })
      {
        text = "Convert to CVRFury Parameters Store"
      };
      convertButton.style.marginTop = new StyleLength(10);
      convertButton.style.height = new StyleLength(30);

      root.Add(warningBox);
      root.Add(convertButton);

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
      addButton.style.marginTop = new StyleLength(5);
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

      if (!ValidateAssetPath(assetPath) || !ConfirmAnimatorSelection())
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
