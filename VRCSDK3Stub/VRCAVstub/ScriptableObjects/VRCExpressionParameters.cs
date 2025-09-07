using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
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

        var parametersLabel = new Label("Parameters Preview:");
        parametersLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        parametersBox.Add(parametersLabel);

        var scrollView = new ScrollView();
        scrollView.style.maxHeight = new StyleLength(200);

        for (int i = 0; i < Mathf.Min(vrcParameters.parameters.Length, 10); i++)
        {
          var param = vrcParameters.parameters[i];
          var paramLabel = new Label($"• {param.name} ({param.valueType}) = {param.defaultValue}");
          paramLabel.style.fontSize = new StyleLength(11);
          scrollView.Add(paramLabel);
        }

        if (vrcParameters.parameters.Length > 10)
        {
          var moreLabel = new Label($"... and {vrcParameters.parameters.Length - 10} more parameters");
          moreLabel.style.fontSize = new StyleLength(11);
          // moreLabel.style.fontStyle = FontStyle.Italic; // Commented out for compatibility
          scrollView.Add(moreLabel);
        }

        parametersBox.Add(scrollView);
        root.Add(parametersBox);
      }

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This VRCExpressionParameters file can be converted to CVRFury format. Click the button below to convert it directly."
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

    private void ConvertToCVRFury()
    {
      var vrcParameters = (VRCExpressionParameters)target;
      var assetPath = AssetDatabase.GetAssetPath(vrcParameters);
      
      if (string.IsNullOrEmpty(assetPath))
      {
        EditorUtility.DisplayDialog(
          "Error",
          "Cannot convert unsaved asset. Please save the VRCExpressionParameters asset first.",
          "OK"
        );
        return;
      }

      // Generate output path
      var filePathWithoutExtension = System.IO.Path.ChangeExtension(assetPath, null);
      var outputPath = filePathWithoutExtension + ".CVRFury.asset";

      // Check if output file already exists
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
          return;
        }
        
        // Delete existing file
        AssetDatabase.DeleteAsset(outputPath);
        AssetDatabase.Refresh();
      }

      try
      {
        // Find CVRFuryParametersStore type
        var cvrParametersStoreType = System.AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "CVRFuryParametersStore");

        if (cvrParametersStoreType == null)
        {
          EditorUtility.DisplayDialog(
            "Error",
            "CVRFuryParametersStore type not found. Make sure CVRFury is properly installed.",
            "OK"
          );
          return;
        }

        // Create new CVRFury parameters store
        var cvrParametersStore = ScriptableObject.CreateInstance(cvrParametersStoreType);
        
        // Get the parameters field
        var parametersField = cvrParametersStoreType.GetField("parameters");
        if (parametersField == null)
        {
          EditorUtility.DisplayDialog("Error", "Could not find parameters field in CVRFuryParametersStore.", "OK");
          return;
        }

        // Find the Parameter nested type and ValueType enum
        var parameterType = cvrParametersStoreType.GetNestedType("Parameter");
        var valueTypeEnum = cvrParametersStoreType.GetNestedType("ValueType");
        
        if (parameterType == null || valueTypeEnum == null)
        {
          EditorUtility.DisplayDialog(
            "Error",
            "Could not find Parameter type or ValueType enum in CVRFuryParametersStore.",
            "OK"
          );
          return;
        }

        // Convert parameters
        if (vrcParameters.parameters != null && vrcParameters.parameters.Length > 0)
        {
          var parameterArray = System.Array.CreateInstance(parameterType, vrcParameters.parameters.Length);
          
          for (int i = 0; i < vrcParameters.parameters.Length; i++)
          {
            var vrcParam = vrcParameters.parameters[i];
            var cvrParam = System.Activator.CreateInstance(parameterType);
            
            // Set name
            var nameField = parameterType.GetField("name");
            nameField?.SetValue(cvrParam, vrcParam.name);
            
            // Convert and set value type
            var valueTypeField = parameterType.GetField("valueType");
            if (valueTypeField != null)
            {
              object cvrValueType;
              switch (vrcParam.valueType)
              {
                case VRCExpressionParameters.ValueType.Bool:
                  cvrValueType = System.Enum.Parse(valueTypeEnum, "Bool");
                  break;
                case VRCExpressionParameters.ValueType.Float:
                  cvrValueType = System.Enum.Parse(valueTypeEnum, "Float");
                  break;
                case VRCExpressionParameters.ValueType.Int:
                  cvrValueType = System.Enum.Parse(valueTypeEnum, "Int");
                  break;
                default:
                  cvrValueType = System.Enum.Parse(valueTypeEnum, "Float");
                  break;
              }
              valueTypeField.SetValue(cvrParam, cvrValueType);
            }
            
            // Set default value
            var defaultValueField = parameterType.GetField("defaultValue");
            defaultValueField?.SetValue(cvrParam, vrcParam.defaultValue);
            
            parameterArray.SetValue(cvrParam, i);
          }
          
          parametersField.SetValue(cvrParametersStore, parameterArray);
        }
        else
        {
          // Create empty array
          var emptyArray = System.Array.CreateInstance(parameterType, 0);
          parametersField.SetValue(cvrParametersStore, emptyArray);
        }

        // Save the new asset
        AssetDatabase.CreateAsset(cvrParametersStore, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Show success dialog
        var paramCount = vrcParameters.parameters?.Length ?? 0;
        string message;
        if (paramCount == 0)
        {
          message = "Conversion completed successfully!\n\nAn empty CVRFury Parameters Store has been created.";
        }
        else
        {
          message =
            $"Conversion completed successfully!\n\n{paramCount} parameters have been converted to CVRFury format.";
        }

        EditorUtility.DisplayDialog("Conversion Complete", $"{message}\n\nOutput file: {outputPath}", "OK");

        // Select the new asset
        var newAsset = AssetDatabase.LoadAssetAtPath(outputPath, typeof(ScriptableObject));
        if (newAsset != null)
        {
          EditorGUIUtility.PingObject(newAsset);
          Selection.activeObject = newAsset;
        }
      }
      catch (System.Exception ex)
      {
        EditorUtility.DisplayDialog("Conversion Error", $"An error occurred during conversion:\n\n{ex.Message}", "OK");
        UnityEngine.Debug.LogError($"VRCExpressionParameters conversion error: {ex}");
      }
    }
  }
}
