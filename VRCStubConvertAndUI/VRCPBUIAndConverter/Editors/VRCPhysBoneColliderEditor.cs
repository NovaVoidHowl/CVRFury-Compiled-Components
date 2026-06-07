using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.PhysBone.Editors.Common;
using uk.novavoidhowl.dev.cvrfury.compiled.vrccolliders;
using uk.novavoidhowl.dev.cvrfury.VRCstub.Common; // Shared UI styles

namespace VRC.SDK3.Dynamics.PhysBone.Editors
{
  /// <summary>
  /// Custom editor for VRCPhysBoneCollider: shows shape info and converts to
  /// DynamicBone, MagicaCloth 1 and MagicaCloth 2 colliders.
  ///
  /// Each converted collider is placed on a new child GameObject created under
  /// the collider's effective root transform (src.rootTransform if set, otherwise
  /// src.transform). This mirrors how VRC resolves the collider's origin at runtime.
  ///
  /// Package detection is done at runtime via type-lookup — no compile-time
  /// #if guards are needed because this file compiles to a precompiled DLL.
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

    // -------------------------------------------------------------------------
    // Runtime package detection
    // -------------------------------------------------------------------------

    private static Type FindType(string fullTypeName)
    {
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        var t = asm.GetType(fullTypeName);
        if (t != null)
          return t;
      }
      return null;
    }

    // -------------------------------------------------------------------------
    // Find an existing converted-collider GO owned by the given source collider
    // that already has a component of targetType. Returns null if none found.
    // Used by the conversion methods to update in place instead of duplicating.
    // -------------------------------------------------------------------------

    private static GameObject FindExistingConvertedGO(Transform effectiveRoot, VRCPhysBoneCollider src, Type targetType)
    {
      foreach (Component c in effectiveRoot.GetComponentsInChildren(targetType, true))
      {
        var marker = c.GetComponent<CVRFuryConvertedColliderMarker>();
        if (marker != null && marker.sourceCollider == src)
          return c.gameObject;
      }
      return null;
    }

    // -------------------------------------------------------------------------
    // Returns true if at least one converted-collider GO owned by src already
    // exists under effectiveRoot, used to set the initial Convert button label.
    // -------------------------------------------------------------------------

    private static bool HasAnyExistingConversions(
      Transform effectiveRoot,
      VRCPhysBoneCollider src,
      Type dbType,
      Type[] mc1Types,
      Type[] mc2Types
    )
    {
      if (dbType != null && FindExistingConvertedGO(effectiveRoot, src, dbType) != null)
        return true;
      foreach (var t in mc1Types)
        if (t != null && FindExistingConvertedGO(effectiveRoot, src, t) != null)
          return true;
      foreach (var t in mc2Types)
        if (t != null && FindExistingConvertedGO(effectiveRoot, src, t) != null)
          return true;
      return false;
    }

    // -------------------------------------------------------------------------
    // Existing-conversions UI section
    // Clears then repopulates a persistent container with clickable navigation
    // buttons for each already-converted collider found under effectiveRoot.
    // Called once at Inspector-build time and again after each conversion so the
    // list updates immediately without requiring a re-selection.
    // -------------------------------------------------------------------------

    private static void PopulateExistingConversions(
      VisualElement container,
      VRCPhysBoneCollider src,
      Transform effectiveRoot,
      Type dbType,
      Type[] mc1Types,
      Type[] mc2Types
    )
    {
      container.Clear();

      var found = new List<(string label, Component comp)>();

      // DynamicBone — only include GOs whose marker references this src collider
      if (dbType != null)
        foreach (Component c in effectiveRoot.GetComponentsInChildren(dbType, true))
        {
          var marker = c.GetComponent<CVRFuryConvertedColliderMarker>();
          if (marker != null && marker.sourceCollider == src)
            found.Add(($"DynamicBoneCollider on \"{c.gameObject.name}\"", c));
        }

      // MagicaCloth 1 — only include GOs whose marker references this src collider
      foreach (var t in mc1Types)
      {
        if (t == null)
          continue;
        foreach (Component c in effectiveRoot.GetComponentsInChildren(t, true))
        {
          var marker = c.GetComponent<CVRFuryConvertedColliderMarker>();
          if (marker != null && marker.sourceCollider == src)
            found.Add(($"{t.Name} (MC1) on \"{c.gameObject.name}\"", c));
        }
      }

      // MagicaCloth 2 — only include GOs whose marker references this src collider
      foreach (var t in mc2Types)
      {
        if (t == null)
          continue;
        foreach (Component c in effectiveRoot.GetComponentsInChildren(t, true))
        {
          var marker = c.GetComponent<CVRFuryConvertedColliderMarker>();
          if (marker != null && marker.sourceCollider == src)
            found.Add(($"{t.Name} (MC2) on \"{c.gameObject.name}\"", c));
        }
      }

      // Leave container empty (invisible) when nothing found
      if (found.Count == 0)
        return;

      var box = new Box();
      box.AddToClassList(CSS_INFO_BOX);

      var heading = new Label("Existing converted colliders:");
      heading.style.unityFontStyleAndWeight = FontStyle.Bold;
      box.Add(heading);

      foreach (var (label, comp) in found)
      {
        var capturedComp = comp;
        var btn = new Button(() =>
        {
          Selection.activeGameObject = capturedComp.gameObject;
          EditorGUIUtility.PingObject(capturedComp.gameObject);
        });
        btn.text = "\u2192 " + label;
        btn.style.unityTextAlign = TextAnchor.MiddleLeft;
        box.Add(btn);
      }

      container.Add(box);
    }

    private static void ShowConversionResultDialog(VRCPhysBoneColliderConversionResult result)
    {
      var msg = result.SummaryMessage;
      if (result.Messages.Count > 0)
      {
        msg += "\n\nMessages:";
        foreach (var message in result.Messages)
        {
          msg += "\n- " + message.Text;
        }
      }

      EditorUtility.DisplayDialog(result.SummaryTitle, msg, "OK");
    }

    // -------------------------------------------------------------------------
    // Inspector GUI
    // -------------------------------------------------------------------------

    public override VisualElement CreateInspectorGUI()
    {
      var src = (VRCPhysBoneCollider)target;

      // Detect physics packages at Inspector-open time via runtime type lookup.
      // No #if guards — this file is a precompiled DLL and project defines are
      // not available at its build time.
      Type dbType = FindType("DynamicBoneCollider");
      Type mc1SphereType = FindType("MagicaCloth.MagicaSphereCollider");
      Type mc1CapsuleType = FindType("MagicaCloth.MagicaCapsuleCollider");
      Type mc1PlaneType = FindType("MagicaCloth.MagicaPlaneCollider");
      Type mc2SphereType = FindType("MagicaCloth2.MagicaSphereCollider");
      Type mc2CapsuleType = FindType("MagicaCloth2.MagicaCapsuleCollider");
      Type mc2PlaneType = FindType("MagicaCloth2.MagicaPlaneCollider");

      bool mc1Available = mc1SphereType != null && mc1CapsuleType != null && mc1PlaneType != null;
      bool mc2Available = mc2SphereType != null && mc2CapsuleType != null && mc2PlaneType != null;
      bool isPlane = src.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane;

      // The effective root is where new collider GameObjects will be parented.
      // VRC resolves the collider's world origin from rootTransform when set,
      // so we match that behaviour for the converted colliders.
      Transform effectiveRoot = src.rootTransform != null ? src.rootTransform : src.transform;

      var root = new VisualElement();
      root.AddToClassList(CSS_CVR_FURY_BUTTONS_CONTAINER);

      // Version labels
      var stubVersionLabel = new Label($"Stub Version: {src.StubVersion}");
      stubVersionLabel.AddToClassList(STUB_VERSION);
      root.Add(stubVersionLabel);

      var uiVersionLabel = new Label(
        $"UI Version: {UIVersion.CurrentVersion} | API Version: {VRCPhysBoneColliderConversionActions.ApiVersion}"
      );
      uiVersionLabel.AddToClassList(UI_VERSION);
      root.Add(uiVersionLabel);

      // Info box — shape description + root transform info + any up-front warnings
      var infoBox = new Box();
      infoBox.AddToClassList(CSS_INFO_BOX);

      string shapeDesc;
      switch (src.shapeType)
      {
        case VRCPhysBoneColliderBase.ShapeType.Sphere:
          shapeDesc = "Sphere (radius only)";
          break;
        case VRCPhysBoneColliderBase.ShapeType.Capsule:
          shapeDesc = "Capsule (radius + height)";
          break;
        case VRCPhysBoneColliderBase.ShapeType.Plane:
          shapeDesc = "Plane (infinite flat plane)";
          break;
        default:
          shapeDesc = src.shapeType.ToString();
          break;
      }

      var shapeLabel = new Label($"Shape: {shapeDesc}");
      shapeLabel.style.whiteSpace = WhiteSpace.Normal;
      infoBox.Add(shapeLabel);

      // Root transform info — always shown so the user knows where converted GOs land
      string rootDesc =
        src.rootTransform != null
          ? $"Root transform: {src.rootTransform.name} \u2014 converted colliders will be created as children of this transform."
          : "No rootTransform set \u2014 converted colliders will be created as children of this GameObject.";
      var rootInfoLabel = new Label(rootDesc);
      rootInfoLabel.style.whiteSpace = WhiteSpace.Normal;
      infoBox.Add(rootInfoLabel);

      if (src.insideBounds)
      {
        var insideLabel = new Label(
          "\u26a0 insideBounds is set \u2014 Inside mode is not supported by MagicaCloth; Outside will be used for MC1/MC2."
        );
        insideLabel.style.whiteSpace = WhiteSpace.Normal;
        insideLabel.style.color = new StyleColor(new Color(1f, 0.75f, 0f));
        infoBox.Add(insideLabel);
      }

      root.Add(infoBox);

      // Existing conversions — persistent container; populated now and refreshed
      // immediately after each conversion without needing a re-select.
      var existingContainer = new VisualElement();
      root.Add(existingContainer);
      PopulateExistingConversions(
        existingContainer,
        src,
        effectiveRoot,
        dbType,
        new[] { mc1SphereType, mc1CapsuleType, mc1PlaneType },
        new[] { mc2SphereType, mc2CapsuleType, mc2PlaneType }
      );

      // Spacer
      var spacer1 = new VisualElement();
      spacer1.AddToClassList(CSS_SPACER);
      root.Add(spacer1);

      // "Convert to:" heading
      var convertLabel = new Label("Convert to:");
      convertLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      root.Add(convertLabel);

      // -- Dynamic Bone toggle --
      // Greyed out if: package not found OR shape is Plane (DB has no Plane collider)
      bool dbCanConvert = dbType != null && !isPlane;
      var dbToggle = new Toggle("Dynamic Bone Collider") { value = dbCanConvert };
      if (!dbCanConvert)
      {
        dbToggle.SetEnabled(false);
        dbToggle.tooltip =
          dbType == null
            ? "DynamicBone package not detected. Run NVH \u2192 CVRFury \u2192 Integrations \u2192 Check Dynamic Bone Support first."
            : "DynamicBone does not support Plane colliders.";
      }
      root.Add(dbToggle);

      // -- MagicaCloth 1 toggle --
      var mc1Toggle = new Toggle("MagicaCloth 1 Collider") { value = mc1Available };
      if (!mc1Available)
      {
        mc1Toggle.SetEnabled(false);
        mc1Toggle.tooltip = "MagicaCloth (v1) package not detected in this project.";
      }
      root.Add(mc1Toggle);

      // -- MagicaCloth 2 toggle --
      var mc2Toggle = new Toggle("MagicaCloth 2 Collider") { value = mc2Available };
      if (!mc2Available)
      {
        mc2Toggle.SetEnabled(false);
        mc2Toggle.tooltip =
          "MagicaCloth 2 package not detected. Ensure it is imported and the MAGICACLOTH2 scripting define is present.";
      }
      root.Add(mc2Toggle);

      // -- Convert button --
      // Label is "Re-Convert" if any conversions already exist for this source collider.
      bool hasExistingConversions = HasAnyExistingConversions(
        effectiveRoot,
        src,
        dbType,
        new[] { mc1SphereType, mc1CapsuleType, mc1PlaneType },
        new[] { mc2SphereType, mc2CapsuleType, mc2PlaneType }
      );
      var convertButton = new Button { text = hasExistingConversions ? "Re-Convert" : "Convert" };
      convertButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      convertButton.style.marginTop = new StyleLength(6);

      // Keep the Convert button enabled only while at least one enabled toggle is checked
      Action updateConvertButton = () =>
      {
        bool any =
          (dbToggle.enabledSelf && dbToggle.value)
          || (mc1Toggle.enabledSelf && mc1Toggle.value)
          || (mc2Toggle.enabledSelf && mc2Toggle.value);
        convertButton.SetEnabled(any);
      };

      dbToggle.RegisterValueChangedCallback(_ => updateConvertButton());
      mc1Toggle.RegisterValueChangedCallback(_ => updateConvertButton());
      mc2Toggle.RegisterValueChangedCallback(_ => updateConvertButton());
      updateConvertButton(); // set initial state

      // Capture for click handler closure
      VisualElement capturedExistingContainer = existingContainer;
      Transform capturedEffectiveRoot = effectiveRoot;
      Type capturedDbType = dbType;
      Type capturedMc1SphereType = mc1SphereType;
      Type capturedMc1CapsuleType = mc1CapsuleType;
      Type capturedMc1PlaneType = mc1PlaneType;
      Type capturedMc2SphereType = mc2SphereType;
      Type capturedMc2CapsuleType = mc2CapsuleType;
      Type capturedMc2PlaneType = mc2PlaneType;

      convertButton.clicked += () =>
      {
        var targets = PhysBoneColliderTarget.None;
        if (dbToggle.enabledSelf && dbToggle.value)
          targets |= PhysBoneColliderTarget.DynamicBone;
        if (mc1Toggle.enabledSelf && mc1Toggle.value)
          targets |= PhysBoneColliderTarget.MagicaCloth1;
        if (mc2Toggle.enabledSelf && mc2Toggle.value)
          targets |= PhysBoneColliderTarget.MagicaCloth2;

        // Group all child-GO creation + component additions into one undoable step
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Convert PhysBone Collider");

        var result = VRCPhysBoneColliderConversionActions.Convert(
          src,
          targets,
          VRCPhysBoneColliderConversionOptions.ForInspector()
        );

        ShowConversionResultDialog(result);

        // Refresh the existing-conversions panel immediately so the new links
        // appear without the user needing to deselect and reselect the object.
        PopulateExistingConversions(
          capturedExistingContainer,
          src,
          capturedEffectiveRoot,
          capturedDbType,
          new[] { capturedMc1SphereType, capturedMc1CapsuleType, capturedMc1PlaneType },
          new[] { capturedMc2SphereType, capturedMc2CapsuleType, capturedMc2PlaneType }
        );

        // Update button label now that at least one conversion exists
        if (result.Success)
          convertButton.text = "Re-Convert";
      };

      root.Add(convertButton);

      // Spacer before Remove
      var spacer2 = new VisualElement();
      spacer2.AddToClassList(CSS_SPACER);
      root.Add(spacer2);

      // Remove button
      // Also destroys any CVRFuryConvertedColliderMarker components that reference
      // this source collider — once the source is gone the markers serve no purpose.
      var removeButton = new Button(() =>
      {
        foreach (var marker in effectiveRoot.GetComponentsInChildren<CVRFuryConvertedColliderMarker>(true))
        {
          if (marker.sourceCollider == src)
            DestroyImmediate(marker);
        }
        DestroyImmediate(target);
      })
      {
        text = "Remove VRC PhysBone Collider Component"
      };
      removeButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      root.Add(removeButton);

      // Trailing spacer
      var spacer3 = new VisualElement();
      spacer3.AddToClassList(CSS_SPACER);
      root.Add(spacer3);

      SharedUIStyles.ApplySharedStyles(root);
      return root;
    }

  }

  // -------------------------------------------------------------------------
  // Marker component — tracks which VRCPhysBoneCollider owns each converted
  // collider child GO, so the Inspector UI can filter to show only the
  // conversions owned by the currently-inspected collider component.
  // -------------------------------------------------------------------------

  /// <summary>
  /// Lightweight marker attached to every converted-collider child GameObject.
  /// The sourceCollider field points back to the VRCPhysBoneCollider that was
  /// the source of the conversion, allowing the UI to filter the
  /// "Existing converted colliders" list to only show relevant entries.
  /// </summary>
  [AddComponentMenu("")] // hide from the Add Component menu
  public class CVRFuryConvertedColliderMarker : MonoBehaviour
  {
    public VRCPhysBoneCollider sourceCollider;
  }
}
