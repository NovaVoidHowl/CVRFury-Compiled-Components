using System;
using System.Collections.Generic;
using System.Linq;
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
  /// Custom editor for VRCExpressionsMenu that provides UI and CVRFury conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCExpressionsMenu))]
  public class VRCExpressionsMenuEditorStub : Editor
  {
    private readonly List<ScriptableObject> selectedParameterStores = new List<ScriptableObject>();

    // Constants for field names used in reflection
    private static readonly string MACHINE_NAME_FIELD = "MachineName";
    private static readonly string FORCE_MACHINE_NAME_FIELD = "forceMachineName";

    // CSS class name constants
    private const string CSS_STORES_CONTAINER = "stores-container";
    private const string CSS_STORES_HEADER = "stores-header";
    private const string CSS_CVR_FURY_BUTTON = "cvr-fury-button";
    private const string CSS_CVR_FURY_BUTTONS_CONTAINER = "cvr-fury-buttons-container";
    private const string CSS_PARAMETER_FIELD = "parameter-field";
    private const string CSS_EMPTY_SLOT = "empty-slot";
    private const string CSS_SECTION_HEADER = "section-header";
    private const string CSS_SPACER = "spacer";
    private const string CSS_INFO_BOX = "info-box";
    private const string CSS_CONTROLS_CONTAINER = "controls-container";
    private const string CSS_CONTROLS_CONTAINER_INNER = "controls-container-inner";
    private const string CONTROL_ITEM = "control-item";
    private const string CSS_PARAMETER_FOUND = "parameter-found";
    private const string CSS_PARAMETER_MISSING = "parameter-missing";
    private const string CSS_PARAMETER_WARNING = "parameter-warning";
    private const string STUB_VERSION = "stub-version";
    private const string UI_VERSION = "ui-version";

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

      var versionLabel = new Label($"Stub Version: {((VRCExpressionsMenu)target).StubVersion}");
      versionLabel.AddToClassList(STUB_VERSION);
      root.Add(versionLabel);

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      // Show controls count
      var vrcMenu = (VRCExpressionsMenu)target;
      var controlsCountLabel = new Label($"Controls: {(vrcMenu.controls?.Count ?? 0)}");
      controlsCountLabel.style.marginBottom = new StyleLength(10);
      root.Add(controlsCountLabel);

      // Container for controls display - we'll need to refresh this when parameter stores change
      var controlsDisplayContainer = new VisualElement();
      controlsDisplayContainer.AddToClassList(CSS_CONTROLS_CONTAINER);
      root.Add(controlsDisplayContainer);

      // Parameter stores selection section
      var parameterStoresSection = new VisualElement();
      parameterStoresSection.AddToClassList(CSS_STORES_CONTAINER);

      var parameterStoresLabel = new Label("CVRFury Parameter Stores to Link:");
      parameterStoresLabel.AddToClassList(CSS_STORES_HEADER);
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

      // Add spacer
      var spacer = new VisualElement();
      spacer.AddToClassList(CSS_SPACER);
      root.Add(spacer);

      var infoBox = new Box();
      infoBox.AddToClassList(CSS_INFO_BOX);

      var infoLabel = new Label(
        "This VRCExpressionsMenu file can be converted to CVRFury format. \n\n"
          + "Select any parameter stores above that should be linked to the converted asset, then click the button below to convert it directly."
      );
      infoLabel.style.whiteSpace = WhiteSpace.Normal;
      infoBox.Add(infoLabel);

      var convertButton = new Button(() =>
      {
        ConvertToCVRFury();
      })
      {
        text = "Convert to CVRFury Menu Store"
      };
      convertButton.AddToClassList(CSS_CVR_FURY_BUTTON);

      root.Add(infoBox);
      root.Add(convertButton);
      root.Add(spacer);

      // Apply styling after all UI elements are created
      ApplyCVRFuryStyles(root);

      return root;
    }

    private void RefreshControlsDisplay(VisualElement controlsDisplayContainer, VRCExpressionsMenu vrcMenu)
    {
      controlsDisplayContainer.Clear();

      if (vrcMenu.controls == null || vrcMenu.controls.Count == 0)
        return;

      var controlsBox = new Box();
      controlsBox.AddToClassList(CSS_CONTROLS_CONTAINER_INNER);
      var controlsLabel = new Label("Controls List:");
      controlsLabel.AddToClassList(CSS_SECTION_HEADER);
      controlsBox.Add(controlsLabel);

      var scrollView = new ScrollView();
      scrollView.style.maxHeight = new StyleLength(250);

      // Get parameter info from selected stores to check availability
      var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
      var parameterInfo =
        validParameterStores.Count > 0
          ? ExtractParameterInfo(validParameterStores)
          : new Dictionary<string, System.Tuple<System.Type, object>>();

      var visited = new HashSet<VRCExpressionsMenu>();
      RenderSubMenuControlsSection(scrollView, vrcMenu, parameterInfo, 0, visited);

      controlsBox.Add(scrollView);
      controlsDisplayContainer.Add(controlsBox);
    }

    /// <summary>
    /// Recursively renders controls from a VRCExpressionsMenu into the given container.
    /// Sub-menu controls are rendered as indented nested sections.
    /// </summary>
    private static void RenderSubMenuControlsSection(
      VisualElement container,
      VRCExpressionsMenu menu,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo,
      int depth,
      HashSet<VRCExpressionsMenu> visited
    )
    {
      if (menu == null || menu.controls == null)
        return;

      if (visited.Contains(menu))
      {
        var cycleLabel = new Label($"{new string('─', depth * 2)}  ⚠ Circular sub-menu reference detected — skipping");
        cycleLabel.style.fontSize = new StyleLength(11);
        cycleLabel.AddToClassList(CSS_PARAMETER_WARNING);
        container.Add(cycleLabel);
        return;
      }
      visited.Add(menu);

      float indentPx = depth * 12f;

      foreach (var control in menu.controls)
      {
        if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
        {
          // Render sub-menu header
          var subMenuHeaderLabel = new Label($"▶ {control.name} (SubMenu)");
          subMenuHeaderLabel.style.fontSize = new StyleLength(11);
          subMenuHeaderLabel.style.marginLeft = new StyleLength(indentPx);
          subMenuHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
          container.Add(subMenuHeaderLabel);

          if (control.subMenu != null)
          {
            RenderSubMenuControlsSection(container, control.subMenu, parameterInfo, depth + 1, visited);
          }
          else
          {
            var emptyLabel = new Label($"  (no linked sub-menu asset)");
            emptyLabel.style.fontSize = new StyleLength(11);
            emptyLabel.style.marginLeft = new StyleLength(indentPx + 12f);
            emptyLabel.AddToClassList(CSS_PARAMETER_WARNING);
            container.Add(emptyLabel);
          }
        }
        else
        {
          var machineName = GetMachineName(control);
          var hasParameter = !string.IsNullOrEmpty(machineName) && parameterInfo.ContainsKey(machineName);
          var checkMark = hasParameter ? "✓ " : "✗ ";
          var controlLabel = new Label($"{checkMark}{control.name} ({control.type}) - {control.parameter?.name}");
          controlLabel.style.fontSize = new StyleLength(11);
          controlLabel.style.marginLeft = new StyleLength(indentPx);
          controlLabel.AddToClassList(CONTROL_ITEM);

          if (hasParameter)
          {
            controlLabel.AddToClassList(CSS_PARAMETER_FOUND);
          }
          else
          {
            controlLabel.AddToClassList(CSS_PARAMETER_MISSING);
          }

          container.Add(controlLabel);
        }
      }

      visited.Remove(menu); // allow the same menu to appear on different branches
    }

    /// <summary>
    /// Refreshes the parameter stores list UI with object fields that only accept CVRFuryParametersStore types.
    /// Includes validation to prevent VRC parameter stores or other incompatible types from being assigned.
    /// </summary>
    private void RefreshParameterStoresList(VisualElement parameterStoresContainer, System.Action onChanged = null)
    {
      parameterStoresContainer.Clear();

      for (int i = 0; i < selectedParameterStores.Count; i++)
      {
        var index = i; // Capture for closure
        var storeRow = new VisualElement();
        storeRow.style.flexDirection = FlexDirection.Row;
        storeRow.style.marginBottom = new StyleLength(2);

        // Get the CVRFury parameter store type for object field restriction
        var cvrParameterStoreType = FindCVRFuryParameterStoreType();
        var objectFieldType = cvrParameterStoreType ?? typeof(ScriptableObject); // Fallback to ScriptableObject if not found

        var objectField = new ObjectField() { objectType = objectFieldType, value = selectedParameterStores[index] };
        objectField.AddToClassList(CSS_PARAMETER_FIELD);
        objectField.style.flexGrow = 1;

        objectField.RegisterValueChangedCallback(evt =>
        {
          // Additional validation to ensure only CVRFury parameter stores are accepted
          var newValue = evt.newValue as ScriptableObject;
          if (newValue != null && cvrParameterStoreType != null && !cvrParameterStoreType.IsInstanceOfType(newValue))
          {
            // Reject non-CVRFury parameter store types
            objectField.SetValueWithoutNotify(selectedParameterStores[index]); // Revert to previous value
            EditorUtility.DisplayDialog(
              "Invalid Type",
              $"Only CVRFuryParametersStore assets are allowed. The selected asset is of type: {newValue.GetType().Name}",
              "OK"
            );
            return;
          }
          selectedParameterStores[index] = newValue;
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
      addButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      parameterStoresContainer.Add(addButton);

      // Show count
      var countLabel = new Label($"Parameter stores selected: {selectedParameterStores.Count}");
      countLabel.style.fontSize = new StyleLength(11);
      countLabel.style.marginTop = new StyleLength(5);
      parameterStoresContainer.Add(countLabel);
    }

    private bool CheckMissingParametersAndWarn(VRCExpressionsMenu vrcMenu)
    {
      if (vrcMenu.controls == null || vrcMenu.controls.Count == 0)
        return true;

      var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
      var parameterInfo =
        validParameterStores.Count > 0
          ? ExtractParameterInfo(validParameterStores)
          : new Dictionary<string, System.Tuple<System.Type, object>>();

      var missingParameters = new List<string>();
      var visited = new HashSet<VRCExpressionsMenu>();
      CollectMissingParametersRecursive(vrcMenu, parameterInfo, missingParameters, visited, "");

      if (missingParameters.Count > 0)
      {
        var message =
          $"Warning: {missingParameters.Count} control(s) have missing parameters that are not found in the selected parameter stores:\n\n"
          + string.Join("\n", missingParameters)
          + "\n\nThese controls may not function correctly in the converted menu. "
          + "Consider adding the missing parameter stores before converting.\n\n"
          + "Do you want to continue with the conversion anyway?";

        return EditorUtility.DisplayDialog("Missing Parameters Warning", message, "Continue Anyway", "Cancel");
      }

      return true;
    }

    /// <summary>
    /// Recursively collects controls whose parameters are not found in the selected stores.
    /// </summary>
    private static void CollectMissingParametersRecursive(
      VRCExpressionsMenu menu,
      Dictionary<string, System.Tuple<System.Type, object>> parameterInfo,
      List<string> missingParameters,
      HashSet<VRCExpressionsMenu> visited,
      string pathPrefix
    )
    {
      if (menu == null || menu.controls == null)
        return;

      if (visited.Contains(menu))
        return;
      visited.Add(menu);

      foreach (var control in menu.controls)
      {
        var controlPath = string.IsNullOrEmpty(pathPrefix) ? control.name : $"{pathPrefix}/{control.name}";

        if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
        {
          if (control.subMenu != null)
            CollectMissingParametersRecursive(control.subMenu, parameterInfo, missingParameters, visited, controlPath);
        }
        else
        {
          var machineName = GetMachineName(control);
          if (!string.IsNullOrEmpty(machineName) && !parameterInfo.ContainsKey(machineName))
          {
            missingParameters.Add($"• {controlPath} ({control.type}) - Parameter: {control.parameter?.name}");
          }
        }
      }

      visited.Remove(menu);
    }

    private void ConvertToCVRFury()
    {
      var vrcMenu = (VRCExpressionsMenu)target;
      var assetPath = AssetDatabase.GetAssetPath(vrcMenu);

      if (!ValidateAssetPath(assetPath))
        return;

      // Check for missing parameters and warn user (recursive across all sub-menus)
      if (!CheckMissingParametersAndWarn(vrcMenu))
        return;

      var outputPath = GenerateOutputPath(assetPath);
      if (!HandleExistingFile(outputPath))
        return;

      try
      {
        var cvrMenuStoreType = FindCVRFuryMenuStoreType();
        if (cvrMenuStoreType == null)
          return;

        var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
        var visited = new HashSet<VRCExpressionsMenu>();
        int subMenusConverted = 0;

        var cvrMenuStore = ConvertMenuAsset(
          vrcMenu,
          outputPath,
          validParameterStores,
          cvrMenuStoreType,
          visited,
          ref subMenusConverted
        );

        SaveAssetAndShowResult(cvrMenuStore, outputPath, vrcMenu, subMenusConverted);
      }
      catch (System.Exception ex)
      {
        HandleConversionError(ex);
      }
    }

    /// <summary>
    /// Core recursive conversion method. Creates a CVRFuryMenuStore asset for the given
    /// VRCExpressionsMenu and saves it to outputPath. Sub-menu controls are converted
    /// recursively (bottom-up) before the parent is saved.
    /// </summary>
    private ScriptableObject ConvertMenuAsset(
      VRCExpressionsMenu vrcMenu,
      string outputPath,
      List<ScriptableObject> parameterStores,
      System.Type cvrMenuStoreType,
      HashSet<VRCExpressionsMenu> visited,
      ref int subMenusConverted
    )
    {
      visited.Add(vrcMenu);

      var cvrMenuStore = ScriptableObject.CreateInstance(cvrMenuStoreType);

      // Link parameter stores
      LinkParameterStoresToAsset(cvrMenuStoreType, cvrMenuStore, parameterStores);

      // Convert controls
      if (vrcMenu.controls != null && vrcMenu.controls.Count > 0 && parameterStores.Count > 0)
      {
        ConvertMenuControls(vrcMenu, cvrMenuStore, cvrMenuStoreType, parameterStores, visited, ref subMenusConverted);
      }

      AssetDatabase.CreateAsset(cvrMenuStore, outputPath);
      AssetDatabase.SaveAssets();

      visited.Remove(vrcMenu);
      return cvrMenuStore;
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

    /// <summary>
    /// Finds the CVRFuryParametersStore type dynamically using reflection.
    /// This is used to restrict object fields to only accept CVRFury parameter stores.
    /// </summary>
    /// <returns>The CVRFuryParametersStore type if found, null otherwise</returns>
    private static System.Type FindCVRFuryParameterStoreType()
    {
      var cvrParameterStoreType = System.AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRFuryParametersStore");

      if (cvrParameterStoreType == null)
      {
        EditorUtility.DisplayDialog(
          "Error",
          "CVRFuryParametersStore type not found. Make sure CVRFury is properly installed.",
          "OK"
        );
      }

      return cvrParameterStoreType;
    }

    private static void LinkParameterStoresToAsset(
      System.Type cvrMenuStoreType,
      ScriptableObject cvrMenuStore,
      List<ScriptableObject> validParameterStores
    )
    {
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

    // Legacy overload kept for backward compatibility — not used by recursive path
    private ScriptableObject CreateAndConfigureMenuStore(System.Type cvrMenuStoreType, VRCExpressionsMenu vrcMenu)
    {
      var cvrMenuStore = ScriptableObject.CreateInstance(cvrMenuStoreType);
      LinkParameterStores(cvrMenuStoreType, cvrMenuStore);
      var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
      if (vrcMenu.controls != null && vrcMenu.controls.Count > 0 && validParameterStores.Count > 0)
      {
        var visited = new HashSet<VRCExpressionsMenu>();
        int ignored = 0;
        ConvertMenuControls(vrcMenu, cvrMenuStore, cvrMenuStoreType, validParameterStores, visited, ref ignored);
      }
      return cvrMenuStore;
    }

    private void LinkParameterStores(System.Type cvrMenuStoreType, ScriptableObject cvrMenuStore)
    {
      var validParameterStores = selectedParameterStores.Where(s => s != null).ToList();
      LinkParameterStoresToAsset(cvrMenuStoreType, cvrMenuStore, validParameterStores);
    }

    private void SaveAssetAndShowResult(
      ScriptableObject cvrMenuStore,
      string outputPath,
      VRCExpressionsMenu vrcMenu,
      int subMenusConverted = 0
    )
    {
      AssetDatabase.Refresh();

      var message = BuildSuccessMessage(vrcMenu, subMenusConverted);
      EditorUtility.DisplayDialog("Conversion Complete", $"{message}\n\nOutput file: {outputPath}", "OK");

      SelectNewAsset(outputPath);
    }

    private string BuildSuccessMessage(VRCExpressionsMenu vrcMenu, int subMenusConverted = 0)
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
      if (subMenusConverted > 0)
      {
        parts.Add($"{subMenusConverted} sub-menu(s) were also recursively converted and linked");
      }

      return $"Conversion completed successfully!\n\n{string.Join(", and ", parts)}.";
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
      List<ScriptableObject> parameterStores,
      HashSet<VRCExpressionsMenu> visited,
      ref int subMenusConverted
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
          object menuItem;
          if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
          {
            menuItem = ConvertSubMenuControl(
              control,
              parameterStores,
              cvrMenuStoreType,
              visited,
              ref subMenusConverted
            );
          }
          else
          {
            menuItem = ConvertControl(control, parameterInfo, dropdownCollectors);
          }

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

        // Log how many items were collected before setting the field
        var countProp = menuItemsListType.GetProperty("Count");
        int itemCount = countProp != null ? (int)countProp.GetValue(menuItemsList) : -1;
        UnityEngine.Debug.Log(
          $"[CVRFury Converter] ConvertMenuControls: setting {itemCount} menu item(s) on '{cvrMenuStore.name}'"
        );

        // Set the menuItems field
        menuItemsField.SetValue(cvrMenuStore, menuItemsList);
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"[CVRFury Converter] ConvertMenuControls error on '{cvrMenuStore?.name}': {ex}");
      }
    }

    /// <summary>
    /// Converts a SubMenu control by recursively converting its linked VRCExpressionsMenu,
    /// saving it as its own .CVRFury.asset, then creating a subMenuParameter that references it.
    /// </summary>
    private object ConvertSubMenuControl(
      VRCExpressionsMenu.Control control,
      List<ScriptableObject> parameterStores,
      System.Type cvrMenuStoreType,
      HashSet<VRCExpressionsMenu> visited,
      ref int subMenusConverted
    )
    {
      if (control.subMenu == null)
      {
        UnityEngine.Debug.LogWarning($"SubMenu control '{control.name}' has no linked sub-menu asset — skipping");
        return null;
      }

      if (visited.Contains(control.subMenu))
      {
        UnityEngine.Debug.LogWarning(
          $"Circular sub-menu reference detected for '{control.name}' — skipping to prevent infinite loop"
        );
        return null;
      }

      var subAssetPath = AssetDatabase.GetAssetPath(control.subMenu);
      if (string.IsNullOrEmpty(subAssetPath))
      {
        UnityEngine.Debug.LogWarning($"Sub-menu asset for '{control.name}' is not saved to disk — skipping");
        return null;
      }

      var subOutputPath = GenerateOutputPath(subAssetPath);

      // Handle existing file for sub-menu (auto-overwrite during recursive conversion)
      if (System.IO.File.Exists(subOutputPath))
      {
        AssetDatabase.DeleteAsset(subOutputPath);
        AssetDatabase.Refresh();
      }

      // Recursively convert the sub-menu asset
      var childStore = ConvertMenuAsset(
        control.subMenu,
        subOutputPath,
        parameterStores,
        cvrMenuStoreType,
        visited,
        ref subMenusConverted
      );

      subMenusConverted++;

      // Find the subMenuParameter type via reflection
      var subMenuParamType = System.AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "subMenuParameter");

      if (subMenuParamType == null)
      {
        UnityEngine.Debug.LogError("subMenuParameter type not found — make sure CVRFury runtime is up to date");
        return null;
      }

      var subMenuParam = System.Activator.CreateInstance(subMenuParamType);
      SetField(subMenuParam, "name", control.name.Trim());
      SetField(subMenuParam, "subMenuStore", childStore);

      UnityEngine.Debug.Log(
        $"[CVRFury Converter] Created subMenuParameter '{control.name.Trim()}' → '{childStore?.name}'"
      );

      return subMenuParam;
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
          // SubMenu controls are handled before ConvertControl is called (in ConvertMenuControls).
          // If we somehow reach here, log and skip.
          UnityEngine.Debug.LogWarning(
            $"SubMenu control '{control.name}' reached ConvertControl unexpectedly — skipping"
          );
          return null;

        case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
          // Not supported
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
