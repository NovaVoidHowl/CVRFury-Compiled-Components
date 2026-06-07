using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.Contact.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.compiled.vrccontacts;
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
      var result = VRCContactConversionActions.ConvertReceiver(
        (VRCContactReceiver)target,
        VRCContactConversionOptions.ForInspector()
      );
      ShowConversionResultDialog(result);
    }

    private static void ShowConversionResultDialog(VRCContactConversionResult result)
    {
      var message = result.SummaryMessage;
      if (result.Messages.Count > 0)
      {
        message += "\n\nMessages:";
        foreach (var conversionMessage in result.Messages)
        {
          message += "\n- " + conversionMessage.Text;
        }
      }

      EditorUtility.DisplayDialog(result.SummaryTitle, message, "OK");
    }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      var stubVersionLabel = new Label($"Stub Version: {((VRCContactReceiver)target).StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      root.Add(stubVersionLabel);

      var uiVersionLabel = new Label(
        $"UI Version: {UIVersion.CurrentVersion} | API Version: {VRCContactConversionActions.ApiVersion}"
      );
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
