using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.PhysBone.Editors.Common;
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
        if (t != null) return t;
      }
      return null;
    }

    // -------------------------------------------------------------------------
    // Axis helper — returns 0=X, 1=Y, 2=Z for the dominant axis of a quaternion
    // -------------------------------------------------------------------------

    private static int GetDominantAxis(Quaternion rotation)
    {
      Vector3 fwd = rotation * Vector3.forward;
      float ax = Mathf.Abs(fwd.x);
      float ay = Mathf.Abs(fwd.y);
      float az = Mathf.Abs(fwd.z);
      if (ax >= ay && ax >= az) return 0; // X
      if (ay >= ax && ay >= az) return 1; // Y
      return 2;                           // Z
    }

    // -------------------------------------------------------------------------
    // Reflection helpers — walk the inheritance chain so base-class
    // properties/fields are found when called on a derived type instance.
    // -------------------------------------------------------------------------

    private static void SetProp(object obj, string name, object value)
    {
      var type = obj.GetType();
      while (type != null)
      {
        var prop = type.GetProperty(
          name,
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
        );
        if (prop != null && prop.CanWrite)
        {
          prop.SetValue(obj, value);
          return;
        }
        type = type.BaseType;
      }
      Debug.LogWarning($"[CVRFury] Property '{name}' not found on {obj.GetType().FullName}");
    }

    private static void SetField(object obj, string name, object value)
    {
      var type = obj.GetType();
      while (type != null)
      {
        var field = type.GetField(
          name,
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
        );
        if (field != null)
        {
          field.SetValue(obj, value);
          return;
        }
        type = type.BaseType;
      }
      Debug.LogWarning($"[CVRFury] Field '{name}' not found on {obj.GetType().FullName}");
    }

    private static Type GetNestedTypeDeep(Type type, string nestedName)
    {
      while (type != null)
      {
        var nested = type.GetNestedType(nestedName, BindingFlags.Public);
        if (nested != null) return nested;
        type = type.BaseType;
      }
      return null;
    }

    // -------------------------------------------------------------------------
    // Create a named child GameObject under a parent, registered with Undo.
    // The new GO is positioned at local identity so that the component's own
    // center/offset field carries the position data from the VRC collider.
    // -------------------------------------------------------------------------

    private static GameObject CreateChildGameObject(string name, Transform parent)
    {
      var go = new GameObject(name);
      Undo.RegisterCreatedObjectUndo(go, "Create converted collider");
      go.transform.SetParent(parent, false);
      go.transform.localPosition = Vector3.zero;
      go.transform.localRotation = Quaternion.identity;
      go.transform.localScale    = Vector3.one;
      return go;
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
      Transform effectiveRoot,
      Type dbType,
      Type[] mc1Types,
      Type[] mc2Types)
    {
      container.Clear();

      var found = new List<(string label, Component comp)>();

      // DynamicBone
      if (dbType != null)
        foreach (Component c in effectiveRoot.GetComponentsInChildren(dbType, true))
          found.Add(($"DynamicBoneCollider on \"{c.gameObject.name}\"", c));

      // MagicaCloth 1
      foreach (var t in mc1Types)
      {
        if (t == null) continue;
        foreach (Component c in effectiveRoot.GetComponentsInChildren(t, true))
          found.Add(($"{t.Name} (MC1) on \"{c.gameObject.name}\"", c));
      }

      // MagicaCloth 2
      foreach (var t in mc2Types)
      {
        if (t == null) continue;
        foreach (Component c in effectiveRoot.GetComponentsInChildren(t, true))
          found.Add(($"{t.Name} (MC2) on \"{c.gameObject.name}\"", c));
      }

      // Leave container empty (invisible) when nothing found
      if (found.Count == 0) return;

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

    // -------------------------------------------------------------------------
    // Inspector GUI
    // -------------------------------------------------------------------------

    public override VisualElement CreateInspectorGUI()
    {
      var src = (VRCPhysBoneCollider)target;

      // Detect physics packages at Inspector-open time via runtime type lookup.
      // No #if guards — this file is a precompiled DLL and project defines are
      // not available at its build time.
      Type dbType         = FindType("DynamicBoneCollider");
      Type mc1SphereType  = FindType("MagicaCloth.MagicaSphereCollider");
      Type mc1CapsuleType = FindType("MagicaCloth.MagicaCapsuleCollider");
      Type mc1PlaneType   = FindType("MagicaCloth.MagicaPlaneCollider");
      Type mc2SphereType  = FindType("MagicaCloth2.MagicaSphereCollider");
      Type mc2CapsuleType = FindType("MagicaCloth2.MagicaCapsuleCollider");
      Type mc2PlaneType   = FindType("MagicaCloth2.MagicaPlaneCollider");

      bool mc1Available = mc1SphereType != null && mc1CapsuleType != null && mc1PlaneType != null;
      bool mc2Available = mc2SphereType != null && mc2CapsuleType != null && mc2PlaneType != null;
      bool isPlane      = src.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane;

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

      var uiVersionLabel = new Label($"UI Version: {UIVersion.CurrentVersion}");
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
      string rootDesc = src.rootTransform != null
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
        dbToggle.tooltip = dbType == null
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
      var convertButton = new Button { text = "Convert" };
      convertButton.AddToClassList(CSS_CVR_FURY_BUTTON);
      convertButton.style.marginTop = new StyleLength(6);

      // Keep the Convert button enabled only while at least one enabled toggle is checked
      Action updateConvertButton = () =>
      {
        bool any = (dbToggle.enabledSelf  && dbToggle.value)
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
      Transform capturedEffectiveRoot  = effectiveRoot;
      Type capturedDbType              = dbType;
      Type capturedMc1SphereType       = mc1SphereType;
      Type capturedMc1CapsuleType      = mc1CapsuleType;
      Type capturedMc1PlaneType        = mc1PlaneType;
      Type capturedMc2SphereType       = mc2SphereType;
      Type capturedMc2CapsuleType      = mc2CapsuleType;
      Type capturedMc2PlaneType        = mc2PlaneType;

      convertButton.clicked += () =>
      {
        var converted = new List<string>();
        var warnings  = new List<string>();

        bool doDb  = dbToggle.enabledSelf  && dbToggle.value;
        bool doMc1 = mc1Toggle.enabledSelf && mc1Toggle.value;
        bool doMc2 = mc2Toggle.enabledSelf && mc2Toggle.value;

        // Cross-cutting warnings
        if (src.insideBounds && (doMc1 || doMc2))
          warnings.Add("Inside-bounds is not supported by MagicaCloth \u2014 Outside mode applied.");

        if (isPlane && (doMc1 || doMc2))
          warnings.Add(
            "Plane: VRC rotation has been applied to the new child GO's local rotation. Verify the orientation in the Scene view."
          );

        // Group all child-GO creation + component additions into one undoable step
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Convert PhysBone Collider");

        if (doDb)
        {
          ConvertToDynamicBone(src, capturedDbType, capturedEffectiveRoot);
          converted.Add("DynamicBoneCollider");
        }

        if (doMc1)
        {
          string mc1Warning;
          string mc1Name = ConvertToMagicaCloth1(
            src,
            capturedMc1SphereType, capturedMc1CapsuleType, capturedMc1PlaneType,
            capturedEffectiveRoot,
            out mc1Warning
          );
          converted.Add(mc1Name);
          if (mc1Warning != null) warnings.Add(mc1Warning);
        }

        if (doMc2)
        {
          string mc2Name = ConvertToMagicaCloth2(
            src,
            capturedMc2SphereType, capturedMc2CapsuleType, capturedMc2PlaneType,
            capturedEffectiveRoot
          );
          converted.Add(mc2Name);
        }

        // Summary dialog
        string msg = "Created child GameObjects under \"" + capturedEffectiveRoot.name + "\":\n"
                   + string.Join(", ", converted.ToArray());
        if (warnings.Count > 0)
        {
          msg += "\n\nWarnings:";
          foreach (var w in warnings)
            msg += "\n\u2022 " + w;
        }
        EditorUtility.DisplayDialog("PhysBone Collider Converted", msg, "OK");

        // Refresh the existing-conversions panel immediately so the new links
        // appear without the user needing to deselect and reselect the object.
        PopulateExistingConversions(
          capturedExistingContainer,
          capturedEffectiveRoot,
          capturedDbType,
          new[] { capturedMc1SphereType, capturedMc1CapsuleType, capturedMc1PlaneType },
          new[] { capturedMc2SphereType, capturedMc2CapsuleType, capturedMc2PlaneType }
        );
      };

      root.Add(convertButton);

      // Spacer before Remove
      var spacer2 = new VisualElement();
      spacer2.AddToClassList(CSS_SPACER);
      root.Add(spacer2);

      // Remove button
      var removeButton = new Button(() => DestroyImmediate(target))
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

    // -------------------------------------------------------------------------
    // Conversion: DynamicBone
    // Creates a new child GO under effectiveRoot, then uses SerializedObject to
    // set the (public) fields robustly through Unity's serialization layer.
    // -------------------------------------------------------------------------

    private static void ConvertToDynamicBone(
      VRCPhysBoneCollider src, Type dbType, Transform effectiveRoot)
    {
      if (src.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane)
      {
        Debug.LogError("[CVRFury] ConvertToDynamicBone called with Plane shape \u2014 aborting.");
        return;
      }

      var newGO = CreateChildGameObject(src.gameObject.name + "_DBCollider", effectiveRoot);

      // VRC capsule extends along its Y-axis; apply the VRC rotation to the child GO
      // so that direction Y on the DynamicBoneCollider aligns with the intended capsule axis.
      // (For spheres the rotation has no meaningful effect but applying it is harmless.)
      if (src.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule)
        newGO.transform.localRotation = src.rotation;

      var dbc   = ObjectFactory.AddComponent(newGO, dbType);
      var so    = new SerializedObject(dbc);

      var radProp = so.FindProperty("m_Radius");
      var hProp   = so.FindProperty("m_Height");
      var cProp   = so.FindProperty("m_Center");
      var dirProp = so.FindProperty("m_Direction");
      var bndProp = so.FindProperty("m_Bound");

      if (radProp != null) radProp.floatValue     = src.radius;
      if (hProp   != null) hProp.floatValue       =
        src.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule ? src.height : 0f;
      if (cProp   != null) cProp.vector3Value     = src.position;
      // Always use Y-axis (1) — VRC's natural capsule axis is Y; orientation is carried by localRotation.
      if (dirProp != null) dirProp.enumValueIndex = 1;
      if (bndProp != null) bndProp.enumValueIndex = src.insideBounds ? 1 : 0; // 0=Outside 1=Inside

      so.ApplyModifiedProperties();
      EditorUtility.SetDirty(newGO);
    }

    // -------------------------------------------------------------------------
    // Conversion: MagicaCloth 1
    // Creates a new child GO under effectiveRoot per shape type.
    // MC1 fields are [SerializeField] private; access via their public properties.
    // Returns the display name of the added component, and an optional warning.
    // -------------------------------------------------------------------------

    private static string ConvertToMagicaCloth1(
      VRCPhysBoneCollider src,
      Type mc1SphereType, Type mc1CapsuleType, Type mc1PlaneType,
      Transform effectiveRoot,
      out string warning)
    {
      warning = null;
      string addedName;

      switch (src.shapeType)
      {
        case VRCPhysBoneColliderBase.ShapeType.Sphere:
        {
          var newGO = CreateChildGameObject(src.gameObject.name + "_MC1SphereCollider", effectiveRoot);
          var c = ObjectFactory.AddComponent(newGO, mc1SphereType);
          SetProp(c, "Radius", src.radius);
          SetProp(c, "Center", src.position);
          EditorUtility.SetDirty(newGO);
          addedName = "MagicaSphereCollider (MC1)";
          break;
        }
        case VRCPhysBoneColliderBase.ShapeType.Capsule:
        {
          var newGO = CreateChildGameObject(src.gameObject.name + "_MC1CapsuleCollider", effectiveRoot);
          // VRC capsule extends along its Y-axis; apply VRC rotation to the child GO
          // so the GO's local Y aligns with the intended capsule axis.
          newGO.transform.localRotation = src.rotation;
          var c = ObjectFactory.AddComponent(newGO, mc1CapsuleType);

          // AxisMode: always Y (1) — VRC's natural capsule axis is Y; orientation carried by GO's rotation.
          var axisEnumType = GetNestedTypeDeep(mc1CapsuleType, "Axis");
          if (axisEnumType != null)
            SetProp(c, "AxisMode", Enum.ToObject(axisEnumType, 1));

          // MC1 Length is a HALF-length (distance from center to one end sphere).
          // Total visual span = 2*Length + 2*radius. MC2/VRC height = total tip-to-tip,
          // so: Length = src.height/2 - src.radius (clamped to MC1's [Range(0,1)] max).
          float mc1HalfLen = Mathf.Max(src.height / 2f - src.radius, 0f);
          float clampedLength = Mathf.Clamp(mc1HalfLen, 0f, 1f);
          if (mc1HalfLen > 1f)
            warning = $"MC1 capsule half-length ({mc1HalfLen:F3}\u202fm) exceeds MC1 max (1.0\u202fm) and was clamped. Original VRC height: {src.height:F3}\u202fm.";

          SetProp(c, "Length",      clampedLength);
          SetProp(c, "StartRadius", src.radius);
          SetProp(c, "EndRadius",   src.radius);
          SetProp(c, "Center",      src.position);
          EditorUtility.SetDirty(newGO);
          addedName = "MagicaCapsuleCollider (MC1)";
          break;
        }
        case VRCPhysBoneColliderBase.ShapeType.Plane:
        {
          var newGO = CreateChildGameObject(src.gameObject.name + "_MC1PlaneCollider", effectiveRoot);
          // MC1 uses the GO's Y+ axis as the plane normal — apply the VRC rotation so
          // the Y+ of this child GO matches the intended plane normal direction.
          newGO.transform.localRotation = src.rotation;
          var c = ObjectFactory.AddComponent(newGO, mc1PlaneType);
          SetProp(c, "Center", src.position);
          EditorUtility.SetDirty(newGO);
          addedName = "MagicaPlaneCollider (MC1)";
          break;
        }
        default:
          return "MagicaCloth1 (unknown shape)";
      }

      return addedName;
    }

    // -------------------------------------------------------------------------
    // Conversion: MagicaCloth 2
    // Creates a new child GO under effectiveRoot per shape type.
    // MC2 exposes public fields (center, direction, radiusSeparation,
    // alignedOnCenter) and public SetSize() methods on each collider type.
    // Returns the display name of the added component.
    // -------------------------------------------------------------------------

    private static string ConvertToMagicaCloth2(
      VRCPhysBoneCollider src,
      Type mc2SphereType, Type mc2CapsuleType, Type mc2PlaneType,
      Transform effectiveRoot)
    {
      string addedName;

      switch (src.shapeType)
      {
        case VRCPhysBoneColliderBase.ShapeType.Sphere:
        {
          var newGO = CreateChildGameObject(src.gameObject.name + "_MC2SphereCollider", effectiveRoot);
          var c = ObjectFactory.AddComponent(newGO, mc2SphereType);
          SetField(c, "center", src.position);

          // MagicaSphereCollider.SetSize(float radius) sets size = new Vector3(r,0,0)
          var setSizeFloat = mc2SphereType.GetMethod("SetSize", new[] { typeof(float) });
          if (setSizeFloat != null)
            setSizeFloat.Invoke(c, new object[] { src.radius });
          else
            SetField(c, "size", new Vector3(src.radius, 0f, 0f)); // fallback

          EditorUtility.SetDirty(newGO);
          addedName = "MagicaSphereCollider (MC2)";
          break;
        }
        case VRCPhysBoneColliderBase.ShapeType.Capsule:
        {
          var newGO = CreateChildGameObject(src.gameObject.name + "_MC2CapsuleCollider", effectiveRoot);
          // VRC capsule extends along its Y-axis; apply VRC rotation to the child GO
          // so the GO's local Y aligns with the intended capsule axis.
          newGO.transform.localRotation = src.rotation;
          var c = ObjectFactory.AddComponent(newGO, mc2CapsuleType);
          SetField(c, "center", src.position);

          // Direction: always Y (1) — VRC's natural capsule axis is Y; orientation carried by GO's rotation.
          var dirEnumType = GetNestedTypeDeep(mc2CapsuleType, "Direction");
          if (dirEnumType != null)
            SetField(c, "direction", Enum.ToObject(dirEnumType, 1)); // 1 = Y-Axis

          SetField(c, "radiusSeparation", false); // uniform radius
          SetField(c, "alignedOnCenter",  true);

          // MagicaCapsuleCollider.SetSize(float startR, float endR, float length)
          var setSizeThree = mc2CapsuleType.GetMethod(
            "SetSize", new[] { typeof(float), typeof(float), typeof(float) }
          );
          if (setSizeThree != null)
            setSizeThree.Invoke(c, new object[] { src.radius, src.radius, src.height });
          else
            SetField(c, "size", new Vector3(src.radius, src.radius, src.height)); // fallback

          EditorUtility.SetDirty(newGO);
          addedName = "MagicaCapsuleCollider (MC2)";
          break;
        }
        case VRCPhysBoneColliderBase.ShapeType.Plane:
        {
          var newGO = CreateChildGameObject(src.gameObject.name + "_MC2PlaneCollider", effectiveRoot);
          // MC2 uses the GO's Y+ axis as the plane normal — apply the VRC rotation so
          // the Y+ of this child GO matches the intended plane normal direction.
          newGO.transform.localRotation = src.rotation;
          var c = ObjectFactory.AddComponent(newGO, mc2PlaneType);
          SetField(c, "center", src.position);
          EditorUtility.SetDirty(newGO);
          addedName = "MagicaPlaneCollider (MC2)";
          break;
        }
        default:
          return "MagicaCloth2 (unknown shape)";
      }

      return addedName;
    }
  }
}
