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
  /// Custom editor for VRCContactSender that provides UI and CVRFury conversion functionality.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(VRCContactSender))]
  public class VRCContactSenderEditor : Editor
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
    /// Converts a VRC Contact Sender to CVR Pointer.
    /// </summary>
    private void ConvertVRCContactSenderToCVR()
    {
      var vrcSender = (VRCContactSender)target;
      var senderGameObject = vrcSender.gameObject;

      // Find required CVR component type using reflection
      var cvrPointerType = FindRequiredCVRPointerType();

      if (cvrPointerType == null)
      {
        Debug.LogError("Could not find CVRPointer component type. Make sure CVR CCK is imported.");
        return;
      }

      // Determine the parent transform for converted pointers
      // If rootTransform is set, use that; otherwise use the sender's transform
      var parentTransform = vrcSender.rootTransform != null ? vrcSender.rootTransform : senderGameObject.transform;

      // Create a child GameObject for each collision tag
      for (int i = 0; i < vrcSender.collisionTags.Count; i++)
      {
        var collisionTag = vrcSender.collisionTags[i];
        var pointerGameObject = new GameObject($"CVR_Pointer{i + 1}_From_{senderGameObject.name}");
        pointerGameObject.transform.SetParent(parentTransform, false);

        // Add appropriate collider based on shape type
        AddColliderBasedOnShape(vrcSender, pointerGameObject);

        // Add CVR pointer component for this collision tag
        var cvrPointer = pointerGameObject.AddComponent(cvrPointerType);
        SetFieldOrProperty(cvrPointerType, cvrPointer, "type", collisionTag);
      }

      ShowConversionCompleteDialog();

      // Mark objects as dirty for saving
      EditorUtility.SetDirty(senderGameObject);
      if (vrcSender.rootTransform != null)
      {
        EditorUtility.SetDirty(vrcSender.rootTransform.gameObject);
      }

      // Remove the original VRC component
      DestroyImmediate(vrcSender);
    }

    /// <summary>
    /// Finds the CVR Pointer component type using reflection.
    /// </summary>
    private static Type FindRequiredCVRPointerType()
    {
      return AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => t.Name == "CVRPointer");
    }

    /// <summary>
    /// Adds the appropriate collider component based on the VRC sender's shape type.
    /// </summary>
    private static void AddColliderBasedOnShape(VRCContactSender vrcSender, GameObject pointerGameObject)
    {
      switch (vrcSender.shapeType)
      {
        case VRC.Dynamics.ContactBase.ShapeType.Sphere:
          var sphereCollider = pointerGameObject.AddComponent<SphereCollider>();
          sphereCollider.radius = vrcSender.radius;
          sphereCollider.isTrigger = true;
          pointerGameObject.transform.localPosition = vrcSender.position;
          break;

        case VRC.Dynamics.ContactBase.ShapeType.Capsule:
          var capsuleCollider = pointerGameObject.AddComponent<CapsuleCollider>();
          capsuleCollider.radius = vrcSender.radius;
          capsuleCollider.height = vrcSender.height;
          capsuleCollider.direction = 1;
          capsuleCollider.isTrigger = true;
          pointerGameObject.transform.localPosition = vrcSender.position;
          pointerGameObject.transform.localRotation = vrcSender.rotation;
          break;

        default:
          Debug.LogError($"Unknown shape type {vrcSender.shapeType}");
          break;
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
        "Contact Sender Conversion Complete",
        "The VRC Contact Sender has been successfully converted to CVR Pointer(s). "
          + "The original component has been removed and new child GameObject(s) with CVR pointers have been created "
          + "(one for each collision tag). "
          + "The pointers are placed under the rootTransform if set, otherwise under the original component's GameObject.",
        "OK"
      );
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      var stubVersionLabel = new Label($"Stub Version: {((VRCContactSender)target).StubVersion}");
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

      var convertButton = new Button(ConvertVRCContactSenderToCVR) { text = "Convert to Chillout VR Pointer" };
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
