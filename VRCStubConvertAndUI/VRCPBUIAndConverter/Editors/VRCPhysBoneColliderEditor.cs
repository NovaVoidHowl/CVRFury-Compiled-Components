using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.PhysBone.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles

namespace VRC.SDK3.Dynamics.PhysBone.Editors
{
  /// <summary>
  /// Custom editor for VRCPhysBoneCollider separated from the stub assembly.
  /// </summary>
  [CustomEditor(typeof(VRCPhysBoneCollider))]
  public class VRCPhysBoneColliderEditor : Editor
  {
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

      var stubVersionLabel = new Label($"Stub Version: {((VRCPhysBoneCollider)target).StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      root.Add(stubVersionLabel);

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      var infoBox = new Box();
      infoBox.AddToClassList(CSS_INFO_BOX);
      var infoLabel = new Label(
        "This component needs to be converted for use in CVR. Automatic conversion is not available yet; you can remove the VRC component and set up the equivalent in CVR manually."
      );
      infoLabel.style.whiteSpace = WhiteSpace.Normal;
      infoBox.Add(infoLabel);

      var removeButton = new Button(() =>
      {
        DestroyImmediate(target);
      })
      {
        text = "Remove VRC PhysBone Collider Component"
      };
      removeButton.AddToClassList(CSS_CVR_FURY_BUTTON);

      var spacer = new VisualElement();
      spacer.AddToClassList(CSS_SPACER);

      root.Add(infoBox);
      root.Add(removeButton);
      root.Add(spacer);

      SharedUIStyles.ApplySharedStyles(root);
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);
      var removeButtons = root.Query<Button>().Where(b => b.text.Contains("Remove")).ToList();
      foreach (var button in removeButtons)
        button.AddToClassList(CSS_CVR_FURY_BUTTON);
      return root;
    }
  }
}
