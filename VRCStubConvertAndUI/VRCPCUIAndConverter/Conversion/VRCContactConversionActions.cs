using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrccontacts
{
  public static class VRCContactConversionActions
  {
    private const string UPDATE_METHOD_OVERRIDE = "Override";

    public static string ApiVersion
    {
      get { return APIVersion.CurrentVersion; }
    }

    public static Version ApiVersionAsVersion
    {
      get { return APIVersion.AsVersion; }
    }

    public static bool IsApiVersionAtLeast(Version otherVersion)
    {
      return APIVersion.IsVersionAtLeast(otherVersion);
    }

    public static bool IsApiVersionAtLeast(string otherVersion)
    {
      return APIVersion.IsVersionAtLeast(otherVersion);
    }

    public static VRCContactConversionAvailability GetSenderAvailability()
    {
      var availability = new VRCContactConversionAvailability();
      availability.IsAvailable = FindRequiredCVRPointerType() != null;
      if (!availability.IsAvailable)
      {
        availability.Messages.Add(
          new ConversionMessage(
            ConversionMessageSeverity.Error,
            "missing_cvr_pointer",
            "Could not find CVRPointer component type. Make sure CVR CCK is imported."
          )
        );
      }
      return availability;
    }

    public static VRCContactConversionAvailability GetReceiverAvailability()
    {
      var availability = new VRCContactConversionAvailability();
      var types = FindRequiredReceiverCVRTypes();
      availability.IsAvailable =
        types.triggerType != null && types.taskType != null && types.taskStayType != null;
      if (!availability.IsAvailable)
      {
        availability.Messages.Add(
          new ConversionMessage(
            ConversionMessageSeverity.Error,
            "missing_cvr_trigger_types",
            "Could not find required CVR component types. Make sure CVR CCK is imported."
          )
        );
      }
      return availability;
    }

    public static VRCContactConversionGuidance GetSenderGuidance()
    {
      var guidance = new VRCContactConversionGuidance();
      guidance.Messages.Add(
        new ConversionMessage(
          ConversionMessageSeverity.Info,
          "sender_destructive_conversion",
          "Contact Sender conversion creates one CVR Pointer per collision tag and removes the source VRC component after success."
        )
      );
      return guidance;
    }

    public static VRCContactConversionGuidance GetReceiverGuidance()
    {
      var guidance = new VRCContactConversionGuidance();
      guidance.Messages.Add(
        new ConversionMessage(
          ConversionMessageSeverity.Info,
          "receiver_destructive_conversion",
          "Contact Receiver conversion creates one CVR Advanced Avatar Settings Trigger and removes the source VRC component after success."
        )
      );
      return guidance;
    }

    public static VRCContactConversionResult ConvertSender(
      VRCContactSender source,
      VRCContactConversionOptions options
    )
    {
      if (source == null)
        return VRCContactConversionResult.Failed("Contact Sender Conversion Failed", "Source sender is missing.", "missing_source");

      if (options == null)
        options = VRCContactConversionOptions.ForInspector();

      var senderGameObject = source.gameObject;
      var cvrPointerType = FindRequiredCVRPointerType();
      if (cvrPointerType == null)
      {
        const string msg = "Could not find CVRPointer component type. Make sure CVR CCK is imported.";
        Debug.LogError(msg);
        return VRCContactConversionResult.Failed("Contact Sender Conversion Failed", msg, "missing_cvr_pointer");
      }

      var result = new VRCContactConversionResult
      {
        Success = true,
        SummaryTitle = "Contact Sender Conversion Complete",
        SummaryMessage =
          "The VRC Contact Sender has been successfully converted to CVR Pointer(s). "
          + "The original component has been removed and new child GameObject(s) with CVR pointers have been created "
          + "(one for each collision tag). "
          + "The pointers are placed under the rootTransform if set, otherwise under the original component's GameObject."
      };

      var parentTransform = source.rootTransform != null ? source.rootTransform : senderGameObject.transform;

      for (int i = 0; i < source.collisionTags.Count; i++)
      {
        var collisionTag = source.collisionTags[i];
        var pointerGameObject = CreateChildGameObject(
          "CVR_Pointer" + (i + 1) + "_From_" + senderGameObject.name,
          parentTransform,
          options
        );

        AddColliderBasedOnShape(source, pointerGameObject, options, result);

        var cvrPointer = AddComponent(pointerGameObject, cvrPointerType, options);
        SetFieldOrProperty(cvrPointerType, cvrPointer, "type", collisionTag);
        result.CreatedObjects.Add(pointerGameObject);
      }

      EditorUtility.SetDirty(senderGameObject);
      if (source.rootTransform != null)
        EditorUtility.SetDirty(source.rootTransform.gameObject);

      if (options.RemoveSourceComponent)
      {
        DestroyComponent(source, options);
        result.SourceRemoved = true;
      }

      return result;
    }

    public static VRCContactConversionResult ConvertReceiver(
      VRCContactReceiver source,
      VRCContactConversionOptions options
    )
    {
      if (source == null)
        return VRCContactConversionResult.Failed("Contact Receiver Conversion Failed", "Source receiver is missing.", "missing_source");

      if (options == null)
        options = VRCContactConversionOptions.ForInspector();

      var receiverGameObject = source.gameObject;
      var types = FindRequiredReceiverCVRTypes();

      if (types.triggerType == null || types.taskType == null || types.taskStayType == null)
      {
        const string msg = "Could not find required CVR component types. Make sure CVR CCK is imported.";
        Debug.LogError(msg);
        return VRCContactConversionResult.Failed("Contact Receiver Conversion Failed", msg, "missing_cvr_trigger_types");
      }

      var result = new VRCContactConversionResult
      {
        Success = true,
        SummaryTitle = "Contact Receiver Conversion Complete",
        SummaryMessage =
          "The VRC Contact Receiver has been successfully converted to a CVR Advanced Avatar Settings Trigger. "
          + "The original component has been removed and a new child GameObject with the CVR trigger has been created. "
          + "The trigger is placed under the rootTransform if set, otherwise under the original component's GameObject."
      };

      var parentTransform = source.rootTransform != null ? source.rootTransform : receiverGameObject.transform;
      var triggerGameObject = CreateChildGameObject("CVR_Trigger_From_" + receiverGameObject.name, parentTransform, options);

      AddColliderBasedOnShape(source, triggerGameObject, options, result);

      var cvrTrigger = AddComponent(triggerGameObject, types.triggerType, options);
      ConfigureTriggerProperties(source, cvrTrigger, types.triggerType);
      ConfigureTriggerTasks(source, cvrTrigger, types.triggerType, types.taskType, types.taskStayType);

      result.CreatedObjects.Add(triggerGameObject);

      EditorUtility.SetDirty(triggerGameObject);
      EditorUtility.SetDirty(receiverGameObject);
      if (source.rootTransform != null)
        EditorUtility.SetDirty(source.rootTransform.gameObject);

      if (options.RemoveSourceComponent)
      {
        DestroyComponent(source, options);
        result.SourceRemoved = true;
      }

      return result;
    }

    private static Type FindRequiredCVRPointerType()
    {
      return AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRPointer");
    }

    private static ReceiverCVRTypes FindRequiredReceiverCVRTypes()
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

      return new ReceiverCVRTypes(triggerType, taskType, taskStayType);
    }

    private static GameObject CreateChildGameObject(
      string name,
      Transform parentTransform,
      VRCContactConversionOptions options
    )
    {
      var gameObject = new GameObject(name);
      if (options.RegisterUndo)
        Undo.RegisterCreatedObjectUndo(gameObject, "Convert VRC Contact Component");

      gameObject.transform.SetParent(parentTransform, false);
      return gameObject;
    }

    private static Component AddComponent(GameObject gameObject, Type componentType, VRCContactConversionOptions options)
    {
      if (options.RegisterUndo)
        return Undo.AddComponent(gameObject, componentType);

      return gameObject.AddComponent(componentType);
    }

    private static T AddComponent<T>(GameObject gameObject, VRCContactConversionOptions options)
      where T : Component
    {
      if (options.RegisterUndo)
        return Undo.AddComponent<T>(gameObject);

      return gameObject.AddComponent<T>();
    }

    private static void DestroyComponent(Component component, VRCContactConversionOptions options)
    {
      if (options.RegisterUndo)
        Undo.DestroyObjectImmediate(component);
      else
        UnityEngine.Object.DestroyImmediate(component);
    }

    private static void AddColliderBasedOnShape(
      VRCContactSender source,
      GameObject pointerGameObject,
      VRCContactConversionOptions options,
      VRCContactConversionResult result
    )
    {
      switch (source.shapeType)
      {
        case VRC.Dynamics.ContactBase.ShapeType.Sphere:
          var sphereCollider = AddComponent<SphereCollider>(pointerGameObject, options);
          sphereCollider.radius = source.radius;
          sphereCollider.isTrigger = true;
          pointerGameObject.transform.localPosition = source.position;
          break;

        case VRC.Dynamics.ContactBase.ShapeType.Capsule:
          var capsuleCollider = AddComponent<CapsuleCollider>(pointerGameObject, options);
          capsuleCollider.radius = source.radius;
          capsuleCollider.height = source.height;
          capsuleCollider.direction = 1;
          capsuleCollider.isTrigger = true;
          pointerGameObject.transform.localPosition = source.position;
          pointerGameObject.transform.localRotation = source.rotation;
          break;

        default:
          AddUnknownShapeWarning(result, source.shapeType.ToString());
          break;
      }
    }

    private static void AddColliderBasedOnShape(
      VRCContactReceiver source,
      GameObject triggerGameObject,
      VRCContactConversionOptions options,
      VRCContactConversionResult result
    )
    {
      switch (source.shapeType)
      {
        case VRC.Dynamics.ContactBase.ShapeType.Sphere:
          var sphereCollider = AddComponent<SphereCollider>(triggerGameObject, options);
          sphereCollider.radius = source.radius;
          sphereCollider.isTrigger = true;
          triggerGameObject.transform.localPosition = source.position;
          break;

        case VRC.Dynamics.ContactBase.ShapeType.Capsule:
          var capsuleCollider = AddComponent<CapsuleCollider>(triggerGameObject, options);
          capsuleCollider.radius = source.radius;
          capsuleCollider.height = source.height;
          capsuleCollider.direction = 1;
          capsuleCollider.isTrigger = true;
          triggerGameObject.transform.localPosition = source.position;
          triggerGameObject.transform.localRotation = source.rotation;
          break;

        default:
          AddUnknownShapeWarning(result, source.shapeType.ToString());
          break;
      }
    }

    private static void AddUnknownShapeWarning(VRCContactConversionResult result, string shapeType)
    {
      var message = "Unknown shape type " + shapeType + ". The converted object was created without a mapped collider.";
      Debug.LogError(message);
      result.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Warning, "unknown_shape", message));
    }

    private static void ConfigureTriggerProperties(
      VRCContactReceiver source,
      object cvrTrigger,
      Type cvrTriggerType
    )
    {
      var areaSizeValue = new Vector3(source.radius / 100, source.radius / 100, source.radius / 100);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "areaSize", areaSizeValue);

      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "useAdvancedTrigger", true);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "isLocalInteractable", source.allowSelf);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "isNetworkInteractable", source.allowOthers);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "allowParticleInteraction", false);
      SetFieldOrProperty(cvrTriggerType, cvrTrigger, "allowedTypes", source.collisionTags.ToArray());
    }

    private static void ConfigureTriggerTasks(
      VRCContactReceiver source,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskType,
      Type cvrTriggerTaskStayType
    )
    {
      switch (source.receiverType)
      {
        case VRC.Dynamics.ContactReceiver.ReceiverType.Constant:
          ConfigureConstantTasks(source, cvrTrigger, cvrTriggerType, cvrTriggerTaskType);
          break;

        case VRC.Dynamics.ContactReceiver.ReceiverType.OnEnter:
          ConfigureOnEnterTasks(source, cvrTrigger, cvrTriggerType, cvrTriggerTaskType);
          break;

        case VRC.Dynamics.ContactReceiver.ReceiverType.Proximity:
          ConfigureProximityTasks(source, cvrTrigger, cvrTriggerType, cvrTriggerTaskStayType);
          break;

        default:
          Debug.LogError("Unknown receiver type " + source.receiverType);
          break;
      }
    }

    private static void ConfigureConstantTasks(
      VRCContactReceiver source,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskType
    )
    {
      var onEnterTask = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onEnterTask, cvrTriggerTaskType, source.parameter, 1f, 0f, 0f, UPDATE_METHOD_OVERRIDE);

      var onExitTask = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onExitTask, cvrTriggerTaskType, source.parameter, 0f, 0f, 0f, UPDATE_METHOD_OVERRIDE);

      AddTaskToList(cvrTrigger, cvrTriggerType, "enterTasks", onEnterTask);
      AddTaskToList(cvrTrigger, cvrTriggerType, "exitTasks", onExitTask);
    }

    private static void ConfigureOnEnterTasks(
      VRCContactReceiver source,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskType
    )
    {
      var onEnterTask = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onEnterTask, cvrTriggerTaskType, source.parameter, 1f, 0f, 0f, UPDATE_METHOD_OVERRIDE);

      var onEnterDelayed = Activator.CreateInstance(cvrTriggerTaskType);
      ConfigureTriggerTask(onEnterDelayed, cvrTriggerTaskType, source.parameter, 0f, 0.5f, 0f, UPDATE_METHOD_OVERRIDE);

      AddTaskToList(cvrTrigger, cvrTriggerType, "enterTasks", onEnterTask);
      AddTaskToList(cvrTrigger, cvrTriggerType, "enterTasks", onEnterDelayed);
    }

    private static void ConfigureProximityTasks(
      VRCContactReceiver source,
      object cvrTrigger,
      Type cvrTriggerType,
      Type cvrTriggerTaskStayType
    )
    {
      var onStayTask = Activator.CreateInstance(cvrTriggerTaskStayType);

      SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "settingName", source.parameter);
      SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "minValue", 1f);
      SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "maxValue", 0f);

      var updateMethodType = cvrTriggerTaskStayType.GetNestedType("UpdateMethod");
      if (updateMethodType != null && updateMethodType.IsEnum)
      {
        var setFromDistanceValue = Enum.Parse(updateMethodType, "SetFromDistance");
        SetFieldOrProperty(cvrTriggerTaskStayType, onStayTask, "updateMethod", setFromDistanceValue);
      }

      AddTaskToList(cvrTrigger, cvrTriggerType, "stayTasks", onStayTask);
    }

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

      var updateMethodType = taskType.GetNestedType("UpdateMethod");
      if (updateMethodType != null && updateMethodType.IsEnum)
      {
        var updateMethodValue = Enum.Parse(updateMethodType, updateMethod);
        SetFieldOrProperty(taskType, task, "updateMethod", updateMethodValue);
      }
    }

    private static void AddTaskToList(object cvrTrigger, Type cvrTriggerType, string listPropertyName, object task)
    {
      var listProperty = cvrTriggerType.GetField(listPropertyName);
      if (listProperty == null)
        return;

      var list = listProperty.GetValue(cvrTrigger) as IList;
      if (list != null)
        list.Add(task);
    }

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
        property.SetValue(instance, value, null);
    }

    private struct ReceiverCVRTypes
    {
      public readonly Type triggerType;
      public readonly Type taskType;
      public readonly Type taskStayType;

      public ReceiverCVRTypes(Type triggerType, Type taskType, Type taskStayType)
      {
        this.triggerType = triggerType;
        this.taskType = taskType;
        this.taskStayType = taskStayType;
      }
    }
  }
}
