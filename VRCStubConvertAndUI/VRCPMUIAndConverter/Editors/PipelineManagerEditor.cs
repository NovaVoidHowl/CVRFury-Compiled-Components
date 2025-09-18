using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.Core;
using VRC.SDK3.Editor.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles

namespace VRC.SDK3.Editor.Editors
{
  /// <summary>
  /// Custom editor for PipelineManager that provides UI for component removal.
  /// This editor is separated from the core stub to allow UI changes without recompiling stubs.
  /// </summary>
  [CustomEditor(typeof(PipelineManager))]
  public class PipelineManagerEditor : UnityEditor.Editor
  {
    // CSS class name constants
    private const string CSS_CVR_FURY_BUTTONS_CONTAINER = "cvr-fury-buttons-container";
    private const string CSS_CVR_FURY_BUTTON = "cvr-fury-button";
    private const string CSS_INFO_BOX = "info-box";
    private const string CSS_SPACER = "spacer";
    private const string STUB_VERSION = "stub-version";
    private const string UI_VERSION = "ui-version";

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      // Apply shared styles
      SharedUIStyles.ApplySharedStyles(root);

      var stubVersionLabel = new Label($"Stub Version: {((PipelineManager)target).StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      root.Add(stubVersionLabel);

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      var spacer = new VisualElement();
      spacer.AddToClassList(CSS_SPACER);
      root.Add(spacer);

      var infoBox = new Box();
      infoBox.AddToClassList(CSS_INFO_BOX);

      var infoLabel = new Label(
        "This component is not needed for data import to CVRFury and should be removed, please click the below button to do so"
      );
      infoLabel.style.whiteSpace = WhiteSpace.Normal;
      infoBox.Add(infoLabel);

      root.Add(infoBox);

      Label blueprintLabel;

      if (((PipelineManager)target).blueprintId == null || ((PipelineManager)target).blueprintId == "")
      {
        blueprintLabel = new Label($"Blueprint ID: Not Set");
      }
      else
      {
        blueprintLabel = new Label($"Blueprint ID: {((PipelineManager)target).blueprintId}");
      }

      blueprintLabel.style.marginTop = new StyleLength(10);
      root.Add(blueprintLabel);

      var removeButton = new Button(() =>
      {
        DestroyImmediate(target);
      })
      {
        text = "Remove PipelineManager Component"
      };
      removeButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      removeButton.style.marginTop = new StyleLength(10);

      root.Add(removeButton);

      return root;
    }
  }
}
