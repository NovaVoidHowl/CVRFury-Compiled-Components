using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.Contact.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles

namespace VRC.SDK3.Dynamics.Contact.Editors
{
  /// <summary>
  /// Custom editor for VRCContactReceiver that provides UI and CVRFury conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCContactReceiver))]
  public class VRCContactReceiverEditor : Editor
  {
    // CSS class name constants
    private const string CSS_CVR_FURY_BUTTONS_CONTAINER = "cvr-fury-buttons-container";
    private const string CSS_CVR_FURY_BUTTON = "cvr-fury-button";
    private const string CSS_INFO_BOX = "info-box";
    private const string CSS_SPACER = "spacer";
    private const string STUB_VERSION = "stub-version";
    private const string UI_VERSION = "ui-version";
    private const string UPDATE_METHOD_OVERRIDE = "Override";

    private static void ApplyCVRFuryStyles(VisualElement root)
    {
      SharedUIStyles.ApplySharedStyles(root);
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);
      var convertButtons = root.Query<Button>().Where(b => b.text.Contains("Convert")).ToList();
      foreach (var button in convertButtons)
        button.AddToClassList(CSS_CVR_FURY_BUTTON);
    }

    /// <summary>
    /// Converts a VRC Contact Receiver to CVR Advanced Avatar Settings Trigger.
    /// </summary>
    private void ConvertVRCContactReceiverToCVR()
    {
      var vrcReceiver = (VRCContactReceiver)target;
      var receiverGameObject = vrcReceiver.gameObject;

      // Find required CVR component types using reflection
      var (cvrTriggerType, cvrTriggerTaskType, cvrTriggerTaskStayType) = FindRequiredCVRTypes();

      if (cvrTriggerType == null || cvrTriggerTaskType == null || cvrTriggerTaskStayType == null)
      {
        Debug.LogError("Could not find required CVR component types. Make sure CVR CCK is imported.");
        return;
      }

      // Determine the parent transform for converted trigger
      // If rootTransform is set, use that; otherwise use the receiver's transform
      var parentTransform = vrcReceiver.rootTransform != null ? vrcReceiver.rootTransform : receiverGameObject.transform;

      // Create a new child GameObject for the trigger
      var triggerGameObject = new GameObject($"CVR_Trigger_From_{receiverGameObject.name}");
      triggerGameObject.transform.SetParent(parentTransform, false);

      // Add appropriate collider based on shape type
      AddColliderBasedOnShape(vrcReceiver, triggerGameObject);

      // Add CVR trigger component
      var cvrTrigger = triggerGameObject.AddComponent(cvrTriggerType);

      // Configure trigger properties
      ConfigureTriggerProperties(vrcReceiver, cvrTrigger, cvrTriggerType);

      // Configure trigger tasks based on receiver type
      ConfigureTriggerTasks(vrcReceiver, cvrTrigger, cvrTriggerType, cvrTriggerTaskType, cvrTriggerTaskStayType);

      ShowConversionCompleteDialog();

      // Mark objects as dirty for saving
      EditorUtility.SetDirty(triggerGameObject);
      EditorUtility.SetDirty(receiverGameObject);
      if (vrcReceiver.rootTransform != null)
      {
        EditorUtility.SetDirty(vrcReceiver.rootTransform.gameObject);
      }

      // Remove the original VRC component
      DestroyImmediate(vrcReceiver);
    }

    /// <summary>
    /// Finds the required CVR component types using reflection.
    /// </summary>
    private static (Type triggerType, Type taskType, Type taskStayType) FindRequiredCVRTypes()
    {
      var triggerType = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRAdvancedAvatarSettingsTrigger");

      var taskType = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRAdvancedAvatarSettingsTriggerTask");

      var taskStayType = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRAdvancedAvatarSettingsTriggerTaskStay");

      return (triggerType, taskType, taskStayType);
    }

    /// <summary>
    /// Adds the appropriate collider component based on the VRC receiver's shape type.
    /// </summary>
    private static void AddColliderBasedOnShape(VRCContactReceiver vrcReceiver, GameObject triggerGameObject)
    {
      switch (vrcReceiver.shapeType)
      {
        case VRC.Dynamics.ContactBase.ShapeType.Sphere:
          var sphereCollider = triggerGameObject.AddComponent<SphereCollider>();
          sphereCollider.radius = vrcReceiver.radius;
          sphereCollider.isTrigger = true;
          triggerGameObject.transform.localPosition = vrcReceiver.position;
          break;

        case VRC.Dynamics.ContactBase.ShapeType.Capsule:
          var capsuleCollider = triggerGameObject.AddComponent<CapsuleCollider>();
          capsuleCollider.radius = vrcReceiver.radius;
          capsuleCollider.height = vrcReceiver.height;
          capsuleCollider.direction = 1;
          capsuleCollider.isTrigger = true;
          triggerGameObject.transform.localPosition = vrcReceiver.position;
          triggerGameObject.transform.localRotation = vrcReceiver.rotation;
          break;

        default:
          Debug.LogError($"Unknown shape type {vrcReceiver.shapeType}");
          break;
      }
    }

    /// <summary>
    /// Configures the basic properties of the CVR trigger component.
    /// </summary>
    private static void ConfigureTriggerProperties(
      VRCContactReceiver vrcReceiver,
      object cvrTrigger,
      Type cvrTriggerType
    )
    {
      // Set area size to a small fraction of the collider radius for distance-based triggers
      var areaSizeValue = new Vector3(vrcReceiver.radius / 100, vrcReceiver.radius / 100, vrcReceiver.radius / 100);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "areaSize", areaSizeValue);

      // Configure advanced trigger settings
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "useAdvancedTrigger", true);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "isLocalInteractable", vrcReceiver.allowSelf);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "isNetworkInteractable", vrcReceiver.allowOthers);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "allowParticleInteraction", false);

      // Set allowed types from collision tags
      var allowedTypes = vrcReceiver.collisionTags.ToArray();
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "allowedTypes", allowedTypes);
    }

    /// <summary>
    /// Configures trigger tasks based on the VRC receiver type.
    /// </summary>
    private static void ConfigureTriggerTasks(
      VRCContactReceiver vrcReceiver,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskType,
      Type cvrTriggerTaskStayType
    )
    {
      switch (vrcReceiver.receiverType)
      {
        case VRC.Dynamics.ContactReceiver.ReceiverType.Constant:
          ConfigureConstantTasks(vrcReceiver, cvrTrigger, cvrTriggerType, cvrTriggerTaskType);
          break;

        case VRC.Dynamics.ContactReceiver.ReceiverType.OnEnter:
          ConfigureOnEnterTasks(vrcReceiver, cvrTrigger, cvrTriggerType, cvrTriggerTaskType);
          break;

        case VRC.Dynamics.ContactReceiver.ReceiverType.Proximity:
          ConfigureProximityTasks(vrcReceiver, cvrTrigger, cvrTriggerType, cvrTriggerTaskStayType);
          break;

        default:
          Debug.LogError($"Unknown receiver type {vrcReceiver.receiverType}");
          break;
      }
    }

    /// <summary>
    /// Configures tasks for Constant receiver type.
    /// </summary>
    private static void ConfigureConstantTasks(
      VRCContactReceiver vrcReceiver,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskType
    )
    {
      // Create enter task (set to 1)
      var onEnterTask = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onEnterTask, cvrTriggerTaskType, vrcReceiver.parameter, 1f, 0f, 0f, UPDATE_METHOD_OVERRIDE);

      // Create exit task (set to 0)
      var onExitTask = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onExitTask, cvrTriggerTaskType, vrcReceiver.parameter, 0f, 0f, 0f, UPDATE_METHOD_OVERRIDE);

      // Add tasks to trigger
      AddTaskToList(cvrTrigger, cvrTriggerType, "enterTasks", onEnterTask);
      AddTaskToList(cvrTrigger, cvrTriggerType, "exitTasks", onExitTask);
    }

    /// <summary>
    /// Configures tasks for OnEnter receiver type.
    /// </summary>
    private static void ConfigureOnEnterTasks(
      VRCContactReceiver vrcReceiver,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskType
    )
    {
      // Create immediate enter task (set to 1)
      var onEnterTask = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onEnterTask, cvrTriggerTaskType, vrcReceiver.parameter, 1f, 0f, 0f, UPDATE_METHOD_OVERRIDE);

      // Create delayed enter task (set to 0 after delay to create pulse effect)
      var onEnterDelayed = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(
        onEnterDelayed,
        cvrTriggerTaskType,
        vrcReceiver.parameter,
        0f,
        0.5f,
        0f,
        UPDATE_METHOD_OVERRIDE
      );

      // Add tasks to trigger
      AddTaskToList(cvrTrigger, cvrTriggerType, "enterTasks", onEnterTask);
      AddTaskToList(cvrTrigger, cvrTriggerType, "enterTasks", onEnterDelayed);
    }

    /// <summary>
    /// Configures tasks for Proximity receiver type.
    /// </summary>
    private static void ConfigureProximityTasks(
      VRCContactReceiver vrcReceiver,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskStayType
    )
    {
      // Create proximity stay task
      var onStayTask = Activator.CreateInstance(cvrTriggerTaskStayType);

      // Configure the stay task for distance-based updates
      SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "settingName", vrcReceiver.parameter);
      SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "minValue", 1f);
      SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "maxValue", 0f);

      // Set update method to SetFromDistance using enum conversion
      var updateMethodType = cvrTriggerTaskStayType.GetNestedType("UpdateMethod");
      if (updateMethodType != null && updateMethodType.IsEnum)
      {
        var setFromDistanceValue = Enum.Parse(updateMethodType, "SetFromDistance");
        SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "updateMethod", setFromDistanceValue);
      }

      // Add task to trigger
      AddTaskToList(cvrTrigger, cvrTriggerType, "stayTasks", onStayTask);
    }

    /// <summary>
    /// Configures a basic trigger task with common properties.
    /// </summary>
    private static void ConfigureTriggerTask(
      object task,
      Type taskType,
      string parameterName,
      float value,
      float delay,
      float holdTime,
      string updateMethod
    )
    {
      SetFieldOrProperty(taskType, task, "settingName", parameterName);
      SetFieldOrProperty(taskType, task, "settingValue", value);
      SetFieldOrProperty(taskType, task, "delay", delay);
      SetFieldOrProperty(taskType, task, "holdTime", holdTime);

      // Set update method using enum conversion
      var updateMethodType = taskType.GetNestedType("UpdateMethod");
      if (updateMethodType != null && updateMethodType.IsEnum)
      {
        var updateMethodValue = Enum.Parse(updateMethodType, updateMethod);
        SetFieldOrProperty(taskType, task, "updateMethod", updateMethodValue);
      }
    }

    /// <summary>
    /// Adds a task to a list property on the trigger component.
    /// </summary>
    private static void AddTaskToList(object cvrTrigger, Type cvrTriggerType, string listPropertyName, object task)
    {
      var listProperty = cvrTriggerType.GetField(listPropertyName);
      if (listProperty != null)
      {
        var list = listProperty.GetValue(cvrTrigger) as System.Collections.IList;
        if (list != null)
        {
          list.Add(task);
        }
      }
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
    /// Shows the conversion complete dialog to the user.
    /// </summary>
    private static void ShowConversionCompleteDialog()
    {
      EditorUtility.DisplayDialog(
        "Contact Receiver Conversion Complete",
        "The VRC Contact Receiver has been successfully converted to a CVR Advanced Avatar Settings Trigger. "
          + "The original component has been removed and a new child GameObject with the CVR trigger has been created. "
          + "The trigger is placed under the rootTransform if set, otherwise under the original component's GameObject.",
        "OK"
      );
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      var stubVersionLabel = new Label($"Stub Version: {((VRCContactReceiver)target).StubVersion}");
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

      var convertButton = new Button(ConvertVRCContactReceiverToCVR)
      {
        text = "Convert to Chillout VR Advanced Avatar Settings Trigger"
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
