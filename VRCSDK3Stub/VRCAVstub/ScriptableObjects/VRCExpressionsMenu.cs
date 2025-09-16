using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace VRC.SDK3.Avatars.ScriptableObjects
{
  public class VRCExpressionsMenu : ScriptableObject
  {
    public const int MAX_CONTROLS = 8;
    public List<Control> controls;

    [Serializable]
    public class Control
    {
      public string name;
      public Texture2D icon;
      public ControlType type;
      public Parameter parameter;
      public float value;
      public Style style;
      public VRCExpressionsMenu subMenu;
      public Parameter[] subParameters;
      public Label[] labels;

      [Serializable]
      public enum ControlType
      {
        Button = 101,
        Toggle = 102,
        SubMenu = 103,
        TwoAxisPuppet = 201,
        FourAxisPuppet = 202,
        RadialPuppet = 203
      }

      [Serializable]
      public enum Style
      {
        Style1 = 0,
        Style2 = 1,
        Style3 = 2,
        Style4 = 3
      }

      [Serializable]
      public struct Label
      {
        public string name;
        public Texture2D icon;
      }

      [Serializable]
      public class Parameter
      {
        public string name;
        public int hash { get; }
      }
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

  [CustomEditor(typeof(VRCExpressionsMenu))]
  public class VRCExpressionsMenuEditorStub : Editor
  {
    private readonly List<ScriptableObject> selectedParameterStores = new List<ScriptableObject>();

    // Constants for field names used in reflection
    private static readonly string MACHINE_NAME_FIELD = "MachineName";
    private static readonly string FORCE_MACHINE_NAME_FIELD = "forceMachineName";

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var versionLabel = new Label($"Stub Version: {((VRCExpressionsMenu)target).StubVersion}");
      versionLabel.style.marginBottom = new StyleLength(10);
      root.Add(versionLabel);

      // Show controls count
      var vrcMenu = (VRCExpressionsMenu)target;
      var controlsCountLabel = new Label($"Controls: {(vrcMenu.controls?.Count ?? 0)}");
      controlsCountLabel.style.marginBottom = new StyleLength(10);
      root.Add(controlsCountLabel);

      // Container for controls display - we'll need to refresh this when parameter stores change
      var controlsDisplayContainer = new VisualElement();
      root.Add(controlsDisplayContainer);

      // Parameter stores selection section
      var parameterStoresSection = new Box();
      parameterStoresSection.style.marginTop = new StyleLength(10);
      parameterStoresSection.style.marginBottom = new StyleLength(10);
      parameterStoresSection.style.paddingTop = new StyleLength(6);
      parameterStoresSection.style.paddingBottom = new StyleLength(6);
      parameterStoresSection.style.paddingLeft = new StyleLength(6);
      parameterStoresSection.style.paddingRight = new StyleLength(6);
      parameterStoresSection.style.backgroundColor = new StyleColor(new Color(0.8f, 1f, 0.8f, 0.2f));

      var parameterStoresLabel = new Label("CVRFury Parameter Stores to Link:");
      parameterStoresLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      parameterStoresLabel.style.marginBottom = new StyleLength(5);
      parameterStoresSection.Add(parameterStoresLabel);

      var parameterStoresHelpLabel = new Label(
        "Select CVRFuryParametersStore assets that contain the parameters used by this menu:"
      );
      parameterStoresHelpLabel.style.fontSize = new StyleLength(11);
      parameterStoresHelpLabel.style.whiteSpace = WhiteSpace.Normal;
      parameterStoresHelpLabel.style.marginBottom = new StyleLength(10);
      parameterStoresSection.Add(parameterStoresHelpLabel);

      // Container for parameter stores list
      var parameterStoresContainer = new VisualElement();
      parameterStoresSection.Add(parameterStoresContainer);

      // Method to refresh both parameter stores list and controls display
      System.Action refreshAll = () =>
      {
        RefreshParameterStoresList(
          parameterStoresContainer,
          () => RefreshControlsDisplay(controlsDisplayContainer, vrcMenu)
        );
        RefreshControlsDisplay(controlsDisplayContainer, vrcMenu);
      };

      refreshAll();
      root.Add(parameterStoresSection);

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This VRCExpressionsMenu file can be converted to CVRFury format. "
          + "Select any parameter stores above that should be linked to the converted asset, then click the button below to convert it directly."
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        ConvertToCVRFury();
      })
      {
        text = "Convert to CVRFury Menu Store"
      };
      convertButton.style.marginTop = new StyleLength(10);
      convertButton.style.height = new StyleLength(30);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }

    private void RefreshControlsDisplay(VisualElement controlsDisplayContainer, VRCExpressionsMenu vrcMenu)
    {
      controlsDisplayContainer.Clear();

      // Show controls list if we have controls
      if (vrcMenu.controls != null && vrcMenu.controls.Count > 0)
      {
        var controlsBox = new Box();
        controlsBox.style.marginBottom = new StyleLength(10);
        controlsBox.style.paddingTop = new StyleLength(6);
        controlsBox.style.paddingBottom = new StyleLength(6);
        controlsBox.style.paddingLeft = new StyleLength(6);
        controlsBox.style.paddingRight = new StyleLength(6);
        controlsBox.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 1f, 0.2f));

        var controlsLabel = new Label("Controls List:");
        controlsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        controlsBox.Add(controlsLabel);

        var scrollView = new ScrollView();
        scrollView.style.maxHeight = new StyleLength(150);

        // Get parameter info from selected stores to check availability
        var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
        var parameterInfo =
          validParameterStores.Count > 0
            ? ExtractParameterInfo(validParameterStores)
            : new Dictionary<string, System.Tuple<System.Type, object>>();

        foreach (var control in vrcMenu.controls)
        {
          var machineName = GetMachineName(control);
          var hasParameter = !string.IsNullOrEmpty(machineName) && parameterInfo.ContainsKey(machineName);
          var checkMark = hasParameter ? "✓ " : "✗ ";
          var controlLabel = new Label($"{checkMark}{control.name} ({control.type}) - {control.parameter?.name}");
          controlLabel.style.fontSize = new StyleLength(11);

          // Color coding: green for found parameters, red for missing
          if (hasParameter)
          {
            controlLabel.style.color = new StyleColor(new Color(0.0f, 0.7f, 0.0f, 1.0f)); // Dark green
          }
          else
          {
            controlLabel.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f, 1.0f)); // Dark red
          }

          scrollView.Add(controlLabel);
        }

        controlsBox.Add(scrollView);
        controlsDisplayContainer.Add(controlsBox);
      }
    }

    private void RefreshParameterStoresList(VisualElement parameterStoresContainer, System.Action onChanged = null)
    {
      parameterStoresContainer.Clear();

      for (int i = 0; i < selectedParameterStores.Count; i++)
      {
        var index = i; // Capture for closure
        var storeRow = new VisualElement();
        storeRow.style.flexDirection = FlexDirection.Row;
        storeRow.style.marginBottom = new StyleLength(2);

        var objectField = new ObjectField()
        {
          objectType = typeof(ScriptableObject),
          value = selectedParameterStores[index]
        };
        objectField.style.flexGrow = 1;

        objectField.RegisterValueChangedCallback(evt =>
        {
          selectedParameterStores[index] = evt.newValue as ScriptableObject;
          onChanged?.Invoke(); // Refresh controls display when parameter store changes
        });

        var removeButton = new Button(() =>
        {
          selectedParameterStores.RemoveAt(index);
          RefreshParameterStoresList(parameterStoresContainer, onChanged);
          onChanged?.Invoke(); // Refresh controls display when parameter store is removed
        })
        {
          text = "Remove"
        };
        removeButton.style.width = new StyleLength(60);
        removeButton.style.marginLeft = new StyleLength(5);

        storeRow.Add(objectField);
        storeRow.Add(removeButton);
        parameterStoresContainer.Add(storeRow);
      }

      // Add new parameter store button
      var addButton = new Button(() =>
      {
        selectedParameterStores.Add(null);
        RefreshParameterStoresList(parameterStoresContainer, onChanged);
        onChanged?.Invoke(); // Refresh controls display when parameter store is added
      })
      {
        text = "Add Parameter Store"
      };
      addButton.style.marginTop = new StyleLength(5);
      parameterStoresContainer.Add(addButton);

      // Show count
      var countLabel = new Label($"Parameter stores selected: {selectedParameterStores.Count}");
      countLabel.style.fontSize = new StyleLength(11);
      countLabel.style.marginTop = new StyleLength(5);
      parameterStoresContainer.Add(countLabel);
    }

    private void ConvertToCVRFury()
    {
      var vrcMenu = (VRCExpressionsMenu)target;
      var assetPath = AssetDatabase.GetAssetPath(vrcMenu);

      if (!ValidateAssetPath(assetPath))
        return;

      var outputPath = GenerateOutputPath(assetPath);
      if (!HandleExistingFile(outputPath))
        return;

      try
      {
        var cvrMenuStoreType = FindCVRFuryMenuStoreType();
        if (cvrMenuStoreType == null)
          return;

        var cvrMenuStore = CreateAndConfigureMenuStore(cvrMenuStoreType, vrcMenu);
        SaveAssetAndShowResult(cvrMenuStore, outputPath, vrcMenu);
      }
      catch (System.Exception ex)
      {
        HandleConversionError(ex);
      }
    }

    private static bool ValidateAssetPath(string assetPath)
    {
      if (string.IsNullOrEmpty(assetPath))
      {
        EditorUtility.DisplayDialog(
          "Error",
          "Cannot convert unsaved asset. Please save the VRCExpressionsMenu asset first.",
          "OK"
        );
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
      if (!System.IO.File.Exists(outputPath))
        return true;

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
      return true;
    }

    private static System.Type FindCVRFuryMenuStoreType()
    {
      var cvrMenuStoreType = System.AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRFuryMenuStore");

      if (cvrMenuStoreType == null)
      {
        EditorUtility.DisplayDialog(
          "Error",
          "CVRFuryMenuStore type not found. Make sure CVRFury is properly installed.",
          "OK"
        );
      }

      return cvrMenuStoreType;
    }

    private ScriptableObject CreateAndConfigureMenuStore(System.Type cvrMenuStoreType, VRCExpressionsMenu vrcMenu)
    {
      var cvrMenuStore = ScriptableObject.CreateInstance(cvrMenuStoreType);

      LinkParameterStores(cvrMenuStoreType, cvrMenuStore);
      ConvertMenuControlsIfNeeded(vrcMenu, cvrMenuStore, cvrMenuStoreType);

      return cvrMenuStore;
    }

    private void LinkParameterStores(System.Type cvrMenuStoreType, ScriptableObject cvrMenuStore)
    {
      var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
      if (validParameterStores.Count == 0)
        return;

      var relatedParametersStoresField = cvrMenuStoreType.GetField("relatedParametersStores");
      if (relatedParametersStoresField == null)
        return;

      var listType = relatedParametersStoresField.FieldType;
      var parameterStoresList = System.Activator.CreateInstance(listType);
      var addMethod = listType.GetMethod("Add");

      foreach (var parameterStore in validParameterStores)
      {
        addMethod.Invoke(parameterStoresList, new object[] { parameterStore });
      }

      relatedParametersStoresField.SetValue(cvrMenuStore, parameterStoresList);
    }

    private void ConvertMenuControlsIfNeeded(
      VRCExpressionsMenu vrcMenu,
      ScriptableObject cvrMenuStore,
      System.Type cvrMenuStoreType
    )
    {
      var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
      if (vrcMenu.controls != null && vrcMenu.controls.Count > 0 && validParameterStores.Count > 0)
      {
        ConvertMenuControls(vrcMenu, cvrMenuStore, cvrMenuStoreType, validParameterStores);
      }
    }

    private void SaveAssetAndShowResult(ScriptableObject cvrMenuStore, string outputPath, VRCExpressionsMenu vrcMenu)
    {
      AssetDatabase.CreateAsset(cvrMenuStore, outputPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      var message = BuildSuccessMessage(vrcMenu);
      EditorUtility.DisplayDialog("Conversion Complete", $"{message}\n\nOutput file: {outputPath}", "OK");

      SelectNewAsset(outputPath);
    }

    private string BuildSuccessMessage(VRCExpressionsMenu vrcMenu)
    {
      var controlCount = vrcMenu.controls?.Count ?? 0;
      var validStoresCount = selectedParameterStores.Count(s => s != null);

      if (controlCount == 0 && validStoresCount == 0)
      {
        return "Conversion completed successfully!\n\nAn empty CVRFury Menu Store has been created.";
      }

      var parts = new List<string>();
      if (controlCount > 0)
      {
        parts.Add($"{controlCount} menu controls have been converted");
      }
      if (validStoresCount > 0)
      {
        parts.Add($"{validStoresCount} parameter stores have been linked");
      }

      return $"Conversion completed successfully!\n\n{string.Join(" and ", parts)}.";
    }

    private static void SelectNewAsset(string outputPath)
    {
      var newAsset = AssetDatabase.LoadAssetAtPath(outputPath, typeof(ScriptableObject));
      if (newAsset != null)
      {
        EditorGUIUtility.PingObject(newAsset);
        Selection.activeObject = newAsset;
      }
    }

    private static void HandleConversionError(System.Exception ex)
    {
      EditorUtility.DisplayDialog("Conversion Error", $"An error occurred during conversion:\n\n{ex.Message}", "OK");
      UnityEngine.Debug.LogError($"VRCExpressionsMenu conversion error: {ex}");
    }

    // Helper class for collecting dropdown parameters
    private sealed class DropdownCollector
    {
      public string machineName;
      public List<DropdownPair> pairs = new List<DropdownPair>();
    }

    private sealed class DropdownPair
    {
      public string name;
      public float value;
    }

    private void ConvertMenuControls(
      VRCExpressionsMenu vrcMenu,
      ScriptableObject cvrMenuStore,
      System.Type cvrMenuStoreType,
      List<ScriptableObject> parameterStores
    )
    {
      try
      {
        // Get parameter information from the parameter stores
        var parameterInfo = ExtractParameterInfo(parameterStores);

        // Get the menuItems field from CVRFuryMenuStore
        var menuItemsField = cvrMenuStoreType.GetField("menuItems");
        if (menuItemsField == null)
          return;

        // Create the menu items list
        var menuItemsListType = menuItemsField.FieldType;
        var menuItemsList = System.Activator.CreateInstance(menuItemsListType);
        var addMethod = menuItemsListType.GetMethod("Add");

        // Collect dropdown items during conversion
        var dropdownCollectors = new List<DropdownCollector>();

        // Process each control
        foreach (var control in vrcMenu.controls)
        {
          var menuItem = ConvertControl(control, parameterInfo, dropdownCollectors);
          if (menuItem != null)
          {
            addMethod.Invoke(menuItemsList, new object[] { menuItem });
          }
        }

        // Convert collected dropdown items
        foreach (var dropdownCollector in dropdownCollectors)
        {
          var dropdownItem = CreateDropdownParameter(dropdownCollector);
          if (dropdownItem != null)
          {
            addMethod.Invoke(menuItemsList, new object[] { dropdownItem });
          }
        }

        // Set the menuItems field
        menuItemsField.SetValue(cvrMenuStore, menuItemsList);
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"ConvertMenuControls error: {ex}");
      }
    }

    private static Dictionary<string, System.Tuple<System.Type, object>> ExtractParameterInfo(
      List<ScriptableObject> parameterStores
    )
    {
      var parameterInfo = new Dictionary<string, System.Tuple<System.Type, object>>();

      foreach (var store in parameterStores)
      {
        if (store == null)
          continue;

        ProcessParameterStore(store, parameterInfo);
      }

      return parameterInfo;
    }

    private static void ProcessParameterStore(
      ScriptableObject store,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo
    )
    {
      var parameters = GetParametersFromStore(store);
      if (parameters == null)
        return;

      foreach (var param in parameters)
      {
        ProcessParameter(param, parameterInfo);
      }
    }

    private static System.Array GetParametersFromStore(ScriptableObject store)
    {
      var storeType = store.GetType();
      var parametersField = storeType.GetField("parameters");

      return parametersField?.GetValue(store) as System.Array;
    }

    private static void ProcessParameter(
      object param,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo
    )
    {
      if (param == null)
        return;

      var parameterData = ExtractParameterData(param);
      if (parameterData == null)
        return;

      var (paramName, valueType) = parameterData.Value;
      if (ShouldAddParameter(paramName, parameterInfo))
      {
        parameterInfo[paramName] = new System.Tuple<System.Type, object>(valueType.GetType(), valueType);
      }
    }

    private static (string paramName, object valueType)? ExtractParameterData(object param)
    {
      var paramType = param.GetType();
      var nameField = paramType.GetField("name");
      var valueTypeField = paramType.GetField("valueType");

      if (nameField == null || valueTypeField == null)
        return null;

      var paramName = nameField.GetValue(param) as string;
      var valueType = valueTypeField.GetValue(param);

      return (paramName, valueType);
    }

    private static bool ShouldAddParameter(
      string paramName,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo
    )
    {
      return !string.IsNullOrEmpty(paramName) && !parameterInfo.ContainsKey(paramName);
    }

    private object ConvertControl(
      VRCExpressionsMenu.Control control,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo,
      List<DropdownCollector> dropdownCollectors
    )
    {
      if (control == null)
        return null;

      var machineName = GetMachineName(control);
      if (string.IsNullOrEmpty(machineName))
        return null;

      switch (control.type)
      {
        case VRCExpressionsMenu.Control.ControlType.Button:
        case VRCExpressionsMenu.Control.ControlType.Toggle:
          return ConvertToggleControl(control, machineName, parameterInfo, dropdownCollectors);

        case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
          return ConvertTwoAxisPuppet(control);

        case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
          return ConvertRadialPuppet(control, machineName);

        case VRCExpressionsMenu.Control.ControlType.SubMenu:
        case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
          // Not supported yet
          UnityEngine.Debug.LogWarning($"Control type {control.type} is not supported for conversion");
          return null;

        default:
          UnityEngine.Debug.LogWarning($"Unknown control type: {control.type}");
          return null;
      }
    }

    private object ConvertToggleControl(
      VRCExpressionsMenu.Control control,
      string machineName,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo,
      List<DropdownCollector> dropdownCollectors
    )
    {
      // Check if we have parameter info for this control
      if (!parameterInfo.ContainsKey(machineName))
      {
        UnityEngine.Debug.LogWarning($"Parameter {machineName} not found in parameter stores, skipping control");
        return null;
      }

      var parameterTypeInfo = parameterInfo[machineName];
      var valueTypeName = parameterTypeInfo.Item2.ToString();

      // If it's a Bool parameter, create a toggle
      if (valueTypeName.Contains("Bool"))
      {
        return CreateToggleParameter(control, machineName);
      }
      else
      {
        // If it's not a Bool, add to dropdown collection
        AddToDropdownCollection(control, machineName, dropdownCollectors);
        return null; // Will be processed later as dropdown
      }
    }

    private static object ConvertTwoAxisPuppet(VRCExpressionsMenu.Control control)
    {
      if (control.subParameters == null || control.subParameters.Length != 2)
      {
        UnityEngine.Debug.LogWarning($"TwoAxisPuppet control {control.name} doesn't have exactly 2 subParameters");
        return null;
      }

      // Get common prefix from subparameters
      var commonPrefix = GetCommonPrefix(control.subParameters[0].name, control.subParameters[1].name);

      return CreateTwoDJoystickParameter(control, commonPrefix);
    }

    private static object ConvertRadialPuppet(VRCExpressionsMenu.Control control, string machineName)
    {
      return CreateSliderParameter(control, machineName);
    }

    private void AddToDropdownCollection(
      VRCExpressionsMenu.Control control,
      string machineName,
      List<DropdownCollector> dropdownCollectors
    )
    {
      var existingCollector = dropdownCollectors.FirstOrDefault(d => d.machineName == machineName);

      if (existingCollector == null)
      {
        existingCollector = new DropdownCollector { machineName = machineName };
        dropdownCollectors.Add(existingCollector);
      }

      existingCollector.pairs.Add(new DropdownPair { name = control.name.Trim(), value = control.value });
    }

    private static object CreateToggleParameter(VRCExpressionsMenu.Control control, string machineName)
    {
      try
      {
        // Find toggleParameter type
        var toggleParameterType = System.AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "toggleParameter");

        if (toggleParameterType == null)
          return null;

        var toggleParameter = System.Activator.CreateInstance(toggleParameterType);

        // Set properties
        SetField(toggleParameter, "name", control.name.Trim());
        SetField(toggleParameter, MACHINE_NAME_FIELD, machineName);
        // Use Math.Abs for floating point comparison instead of exact equality (==)
        // to avoid precision issues where 1.0f might be stored as 0.9999999f or 1.0000001f
        SetField(toggleParameter, "defaultState", Math.Abs(control.value - 1f) < 0.0001f ? 1f : 0f);
        SetField(toggleParameter, FORCE_MACHINE_NAME_FIELD, true);

        // Set generateType to Bool
        var generateTypeEnum = toggleParameterType.GetNestedType("GenerateType");
        if (generateTypeEnum != null)
        {
          var boolValue = System.Enum.Parse(generateTypeEnum, "Bool");
          SetField(toggleParameter, "generateType", boolValue);
        }

        return toggleParameter;
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"Error creating toggle parameter: {ex}");
        return null;
      }
    }

    private static object CreateTwoDJoystickParameter(VRCExpressionsMenu.Control control, string machineName)
    {
      try
      {
        var joystickType = System.AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "twoDJoystickParameter");

        if (joystickType == null)
          return null;

        var joystickParameter = System.Activator.CreateInstance(joystickType);

        SetField(joystickParameter, "name", control.name.Trim());
        SetField(joystickParameter, MACHINE_NAME_FIELD, machineName);
        SetField(joystickParameter, FORCE_MACHINE_NAME_FIELD, true);

        return joystickParameter;
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"Error creating 2D joystick parameter: {ex}");
        return null;
      }
    }

    private static object CreateSliderParameter(VRCExpressionsMenu.Control control, string machineName)
    {
      try
      {
        var sliderType = System.AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "sliderParameter");

        if (sliderType == null)
          return null;

        var sliderParameter = System.Activator.CreateInstance(sliderType);

        SetField(sliderParameter, "name", control.name.Trim());
        SetField(sliderParameter, MACHINE_NAME_FIELD, machineName);
        SetField(sliderParameter, FORCE_MACHINE_NAME_FIELD, true);

        return sliderParameter;
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"Error creating slider parameter: {ex}");
        return null;
      }
    }

    private static object CreateDropdownParameter(DropdownCollector collector)
    {
      try
      {
        var dropdownType = System.AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "dropdownParameter");

        if (dropdownType == null)
          return null;

        var dropdownParameter = System.Activator.CreateInstance(dropdownType);

        SetField(dropdownParameter, "name", collector.machineName);
        SetField(dropdownParameter, MACHINE_NAME_FIELD, collector.machineName);
        SetField(dropdownParameter, FORCE_MACHINE_NAME_FIELD, true);

        // Set generateType to Int (default for dropdowns)
        var generateTypeEnum = dropdownType.GetNestedType("GenerateType");
        if (generateTypeEnum != null)
        {
          var intValue = System.Enum.Parse(generateTypeEnum, "Int");
          SetField(dropdownParameter, "generateType", intValue);
        }

        // Create dropdown list - we need to create DropdownParameterPair objects
        var dropdownListField = dropdownType.GetField("dropdownList");
        if (dropdownListField != null)
        {
          var dropdownListType = dropdownListField.FieldType;
          var dropdownList = System.Activator.CreateInstance(dropdownListType);
          var addMethod = dropdownListType.GetMethod("Add");

          // Find DropdownParameterPair type
          var pairType = System.AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "DropdownParameterPair");

          if (pairType != null && addMethod != null)
          {
            foreach (var pair in collector.pairs)
            {
              var pairObject = System.Activator.CreateInstance(pairType);
              SetField(pairObject, "name", pair.name);
              SetField(pairObject, "value", pair.value);
              addMethod.Invoke(dropdownList, new object[] { pairObject });
            }
          }

          dropdownListField.SetValue(dropdownParameter, dropdownList);
        }

        return dropdownParameter;
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"Error creating dropdown parameter: {ex}");
        return null;
      }
    }

    private static string GetMachineName(VRCExpressionsMenu.Control control)
    {
      // Priority 1: Use parameter name if it exists (this is the actual parameter name used in animators)
      if (control.parameter != null && !string.IsNullOrEmpty(control.parameter.name))
      {
        return control.parameter.name;
      }
      // Priority 2: If there's exactly one subParameter, use that
      else if (control.subParameters != null && control.subParameters.Length == 1)
      {
        return control.subParameters[0].name;
      }
      // Priority 3: If multiple subParameters, use control name and warn
      else if (control.subParameters != null && control.subParameters.Length > 1)
      {
        UnityEngine.Debug.LogWarning($"Control {control.name} has multiple subParameters, using control name");
        return control.name;
      }
      // Priority 4: Fall back to control name
      else
      {
        return control.name;
      }
    }

    private static string GetCommonPrefix(string str1, string str2)
    {
      if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
        return string.Empty;

      int minLength = System.Math.Min(str1.Length, str2.Length);
      for (int i = 0; i < minLength; i++)
      {
        if (str1[i] != str2[i])
        {
          return str1.Substring(0, i);
        }
      }
      return str1.Substring(0, minLength);
    }

    private static void SetField(object target, string fieldName, object value)
    {
      if (target == null)
        return;

      var field = target.GetType().GetField(fieldName);
      if (field != null)
      {
        field.SetValue(target, value);
      }
    }
  }
}
