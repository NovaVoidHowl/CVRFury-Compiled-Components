using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.PhysBone.Editors;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrccolliders
{
  public static class VRCPhysBoneColliderConversionActions
  {
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

    public static VRCPhysBoneColliderConversionAvailability GetAvailability(VRCPhysBoneCollider source)
    {
      var types = DetectTargetTypes();
      bool isPlane = source != null && source.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane;

      var availability = new VRCPhysBoneColliderConversionAvailability
      {
        CanConvertDynamicBone = types.dbType != null && !isPlane,
        CanConvertMagicaCloth1 = types.mc1SphereType != null && types.mc1CapsuleType != null && types.mc1PlaneType != null,
        CanConvertMagicaCloth2 = types.mc2SphereType != null && types.mc2CapsuleType != null && types.mc2PlaneType != null
      };

      if (types.dbType == null)
        availability.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Info, "dynamic_bone_missing", "DynamicBone package not detected."));
      else if (isPlane)
        availability.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Info, "dynamic_bone_plane_unsupported", "DynamicBone does not support Plane colliders."));

      if (!availability.CanConvertMagicaCloth1)
        availability.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Info, "magica_cloth_1_missing", "MagicaCloth 1 package not detected."));

      if (!availability.CanConvertMagicaCloth2)
        availability.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Info, "magica_cloth_2_missing", "MagicaCloth 2 package not detected."));

      return availability;
    }

    public static VRCPhysBoneColliderConversionGuidance GetGuidance(
      VRCPhysBoneCollider source,
      PhysBoneColliderTarget targets
    )
    {
      var guidance = new VRCPhysBoneColliderConversionGuidance();

      guidance.Messages.Add(
        new ConversionMessage(
          ConversionMessageSeverity.Info,
          "collider_reconvert_updates_owned",
          "PhysBone collider conversion updates existing owned converted colliders in place when marker ownership matches the source collider."
        )
      );

      if (source != null && source.insideBounds && HasTarget(targets, PhysBoneColliderTarget.MagicaCloth1 | PhysBoneColliderTarget.MagicaCloth2))
      {
        guidance.Messages.Add(
          new ConversionMessage(
            ConversionMessageSeverity.Warning,
            "inside_bounds_magica_approximation",
            "Inside-bounds is not supported by MagicaCloth. Outside mode is applied for MC1/MC2."
          )
        );
      }

      if (source != null && source.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane && HasTarget(targets, PhysBoneColliderTarget.MagicaCloth1 | PhysBoneColliderTarget.MagicaCloth2))
      {
        guidance.Messages.Add(
          new ConversionMessage(
            ConversionMessageSeverity.Warning,
            "plane_orientation_check",
            "Plane conversion applies the VRC rotation to the child GameObject. Verify the orientation in the Scene view."
          )
        );
      }

      return guidance;
    }

    public static VRCPhysBoneColliderConversionResult Convert(
      VRCPhysBoneCollider source,
      PhysBoneColliderTarget targets,
      VRCPhysBoneColliderConversionOptions options
    )
    {
      if (source == null)
        return VRCPhysBoneColliderConversionResult.Failed("PhysBone Collider Conversion Failed", "Source collider is missing.", "missing_source");

      if (options == null)
        options = VRCPhysBoneColliderConversionOptions.ForInspector();

      var types = DetectTargetTypes();
      var effectiveRoot = source.rootTransform != null ? source.rootTransform : source.transform;
      var result = new VRCPhysBoneColliderConversionResult
      {
        SummaryTitle = "PhysBone Collider Converted",
        EffectiveRoot = effectiveRoot
      };

      foreach (var message in GetGuidance(source, targets).Messages)
        result.Messages.Add(message);

      if (targets == PhysBoneColliderTarget.None)
      {
        const string message = "No collider conversion targets were selected.";
        result.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Error, "no_targets_selected", message));
        result.SummaryMessage = message;
        return result;
      }

      if (HasTarget(targets, PhysBoneColliderTarget.DynamicBone))
      {
        if (types.dbType == null)
          AddError(result, "dynamic_bone_missing", "DynamicBoneCollider type was not found. Dynamic Bone conversion was skipped.");
        else if (source.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane)
          AddError(result, "dynamic_bone_plane_unsupported", "DynamicBone does not support Plane colliders. Dynamic Bone conversion was skipped.");
        else
        {
          var convertedObject = ConvertToDynamicBone(source, types.dbType, effectiveRoot);
          result.ConvertedTargets.Add("DynamicBoneCollider");
          result.CreatedOrUpdatedObjects.Add(convertedObject);
        }
      }

      if (HasTarget(targets, PhysBoneColliderTarget.MagicaCloth1))
      {
        if (types.mc1SphereType == null || types.mc1CapsuleType == null || types.mc1PlaneType == null)
          AddError(result, "magica_cloth_1_missing", "MagicaCloth 1 collider types were not found. MC1 conversion was skipped.");
        else
        {
          string warning;
          GameObject convertedObject;
          string mc1Name = ConvertToMagicaCloth1(
            source,
            types.mc1SphereType,
            types.mc1CapsuleType,
            types.mc1PlaneType,
            effectiveRoot,
            out warning,
            out convertedObject
          );
          result.ConvertedTargets.Add(mc1Name);
          result.CreatedOrUpdatedObjects.Add(convertedObject);
          if (warning != null)
            result.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Warning, "magica_cloth_1_capsule_clamped", warning));
        }
      }

      if (HasTarget(targets, PhysBoneColliderTarget.MagicaCloth2))
      {
        if (types.mc2SphereType == null || types.mc2CapsuleType == null || types.mc2PlaneType == null)
          AddError(result, "magica_cloth_2_missing", "MagicaCloth 2 collider types were not found. MC2 conversion was skipped.");
        else
        {
          GameObject convertedObject;
          string mc2Name = ConvertToMagicaCloth2(
            source,
            types.mc2SphereType,
            types.mc2CapsuleType,
            types.mc2PlaneType,
            effectiveRoot,
            out convertedObject
          );
          result.ConvertedTargets.Add(mc2Name);
          result.CreatedOrUpdatedObjects.Add(convertedObject);
        }
      }

      result.Success = result.ConvertedTargets.Count > 0;
      result.SummaryMessage = BuildSummaryMessage(result);
      return result;
    }

    private static bool HasTarget(PhysBoneColliderTarget targets, PhysBoneColliderTarget target)
    {
      return (targets & target) != 0;
    }

    private static void AddError(VRCPhysBoneColliderConversionResult result, string code, string message)
    {
      Debug.LogError("[CVRFury] " + message);
      result.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Error, code, message));
    }

    private static string BuildSummaryMessage(VRCPhysBoneColliderConversionResult result)
    {
      if (!result.Success)
        return "No PhysBone collider conversions were completed.";

      return "Created or updated child GameObjects under \""
        + result.EffectiveRoot.name
        + "\":\n"
        + string.Join(", ", result.ConvertedTargets.ToArray());
    }

    private static TargetTypes DetectTargetTypes()
    {
      return new TargetTypes(
        FindType("DynamicBoneCollider"),
        FindType("MagicaCloth.MagicaSphereCollider"),
        FindType("MagicaCloth.MagicaCapsuleCollider"),
        FindType("MagicaCloth.MagicaPlaneCollider"),
        FindType("MagicaCloth2.MagicaSphereCollider"),
        FindType("MagicaCloth2.MagicaCapsuleCollider"),
        FindType("MagicaCloth2.MagicaPlaneCollider")
      );
    }

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

    private static void SetProp(object obj, string name, object value)
    {
      var type = obj.GetType();
      while (type != null)
      {
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (prop != null && prop.CanWrite)
        {
          prop.SetValue(obj, value);
          return;
        }
        type = type.BaseType;
      }
      Debug.LogWarning("[CVRFury] Property '" + name + "' not found on " + obj.GetType().FullName);
    }

    private static void SetField(object obj, string name, object value)
    {
      var type = obj.GetType();
      while (type != null)
      {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (field != null)
        {
          field.SetValue(obj, value);
          return;
        }
        type = type.BaseType;
      }
      Debug.LogWarning("[CVRFury] Field '" + name + "' not found on " + obj.GetType().FullName);
    }

    private static Type GetNestedTypeDeep(Type type, string nestedName)
    {
      while (type != null)
      {
        var nested = type.GetNestedType(nestedName, BindingFlags.Public);
        if (nested != null)
          return nested;
        type = type.BaseType;
      }
      return null;
    }

    private static GameObject CreateChildGameObject(string name, Transform parent)
    {
      var go = new GameObject(name);
      Undo.RegisterCreatedObjectUndo(go, "Create converted collider");
      go.transform.SetParent(parent, false);
      go.transform.localPosition = Vector3.zero;
      go.transform.localRotation = Quaternion.identity;
      go.transform.localScale = Vector3.one;
      return go;
    }

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

    private static GameObject ConvertToDynamicBone(VRCPhysBoneCollider src, Type dbType, Transform effectiveRoot)
    {
      var existingDbGO = FindExistingConvertedGO(effectiveRoot, src, dbType);
      var newGO = existingDbGO ?? CreateChildGameObject(src.gameObject.name + "_DBCollider", effectiveRoot);

      if (src.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule)
        newGO.transform.localRotation = src.rotation;

      var dbc = newGO.GetComponent(dbType) ?? ObjectFactory.AddComponent(newGO, dbType);
      var so = new SerializedObject(dbc);

      var radProp = so.FindProperty("m_Radius");
      var hProp = so.FindProperty("m_Height");
      var cProp = so.FindProperty("m_Center");
      var dirProp = so.FindProperty("m_Direction");
      var bndProp = so.FindProperty("m_Bound");

      if (radProp != null)
        radProp.floatValue = src.radius;
      if (hProp != null)
        hProp.floatValue = src.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule ? src.height : 0f;
      if (cProp != null)
        cProp.vector3Value = src.position;
      if (dirProp != null)
        dirProp.enumValueIndex = 1;
      if (bndProp != null)
        bndProp.enumValueIndex = src.insideBounds ? 1 : 0;

      so.ApplyModifiedProperties();
      if (existingDbGO == null)
      {
        var dbMarker = newGO.AddComponent<CVRFuryConvertedColliderMarker>();
        dbMarker.sourceCollider = src;
      }
      EditorUtility.SetDirty(newGO);
      return newGO;
    }

    private static string ConvertToMagicaCloth1(
      VRCPhysBoneCollider src,
      Type mc1SphereType,
      Type mc1CapsuleType,
      Type mc1PlaneType,
      Transform effectiveRoot,
      out string warning,
      out GameObject convertedObject
    )
    {
      warning = null;

      switch (src.shapeType)
      {
        case VRCPhysBoneColliderBase.ShapeType.Sphere:
        {
          var existingMc1SphereGO = FindExistingConvertedGO(effectiveRoot, src, mc1SphereType);
          var newGO = existingMc1SphereGO ?? CreateChildGameObject(src.gameObject.name + "_MC1SphereCollider", effectiveRoot);
          var c = newGO.GetComponent(mc1SphereType) ?? ObjectFactory.AddComponent(newGO, mc1SphereType);
          SetProp(c, "Radius", src.radius);
          SetProp(c, "Center", src.position);
          AddMarkerIfNeeded(newGO, existingMc1SphereGO, src);
          EditorUtility.SetDirty(newGO);
          convertedObject = newGO;
          return "MagicaSphereCollider (MC1)";
        }
        case VRCPhysBoneColliderBase.ShapeType.Capsule:
        {
          var existingMc1CapsGO = FindExistingConvertedGO(effectiveRoot, src, mc1CapsuleType);
          var newGO = existingMc1CapsGO ?? CreateChildGameObject(src.gameObject.name + "_MC1CapsuleCollider", effectiveRoot);
          newGO.transform.localRotation = src.rotation;
          var c = newGO.GetComponent(mc1CapsuleType) ?? ObjectFactory.AddComponent(newGO, mc1CapsuleType);

          var axisEnumType = GetNestedTypeDeep(mc1CapsuleType, "Axis");
          if (axisEnumType != null)
            SetProp(c, "AxisMode", Enum.ToObject(axisEnumType, 1));

          float mc1HalfLen = Mathf.Max(src.height / 2f - src.radius, 0f);
          float clampedLength = Mathf.Clamp(mc1HalfLen, 0f, 1f);
          if (mc1HalfLen > 1f)
            warning =
              "MC1 capsule half-length ("
              + mc1HalfLen.ToString("F3")
              + " m) exceeds MC1 max (1.0 m) and was clamped. Original VRC height: "
              + src.height.ToString("F3")
              + " m.";

          SetProp(c, "Length", clampedLength);
          SetProp(c, "StartRadius", src.radius);
          SetProp(c, "EndRadius", src.radius);
          SetProp(c, "Center", src.position);
          AddMarkerIfNeeded(newGO, existingMc1CapsGO, src);
          EditorUtility.SetDirty(newGO);
          convertedObject = newGO;
          return "MagicaCapsuleCollider (MC1)";
        }
        case VRCPhysBoneColliderBase.ShapeType.Plane:
        {
          var existingMc1PlaneGO = FindExistingConvertedGO(effectiveRoot, src, mc1PlaneType);
          var newGO = existingMc1PlaneGO ?? CreateChildGameObject(src.gameObject.name + "_MC1PlaneCollider", effectiveRoot);
          newGO.transform.localRotation = src.rotation;
          var c = newGO.GetComponent(mc1PlaneType) ?? ObjectFactory.AddComponent(newGO, mc1PlaneType);
          SetProp(c, "Center", src.position);
          AddMarkerIfNeeded(newGO, existingMc1PlaneGO, src);
          EditorUtility.SetDirty(newGO);
          convertedObject = newGO;
          return "MagicaPlaneCollider (MC1)";
        }
        default:
          convertedObject = null;
          return "MagicaCloth1 (unknown shape)";
      }
    }

    private static string ConvertToMagicaCloth2(
      VRCPhysBoneCollider src,
      Type mc2SphereType,
      Type mc2CapsuleType,
      Type mc2PlaneType,
      Transform effectiveRoot,
      out GameObject convertedObject
    )
    {
      switch (src.shapeType)
      {
        case VRCPhysBoneColliderBase.ShapeType.Sphere:
        {
          var existingMc2SphereGO = FindExistingConvertedGO(effectiveRoot, src, mc2SphereType);
          var newGO = existingMc2SphereGO ?? CreateChildGameObject(src.gameObject.name + "_MC2SphereCollider", effectiveRoot);
          var c = newGO.GetComponent(mc2SphereType) ?? ObjectFactory.AddComponent(newGO, mc2SphereType);
          SetField(c, "center", src.position);

          var setSizeFloat = mc2SphereType.GetMethod("SetSize", new[] { typeof(float) });
          if (setSizeFloat != null)
            setSizeFloat.Invoke(c, new object[] { src.radius });
          else
            SetField(c, "size", new Vector3(src.radius, 0f, 0f));

          AddMarkerIfNeeded(newGO, existingMc2SphereGO, src);
          EditorUtility.SetDirty(newGO);
          convertedObject = newGO;
          return "MagicaSphereCollider (MC2)";
        }
        case VRCPhysBoneColliderBase.ShapeType.Capsule:
        {
          var existingMc2CapsGO = FindExistingConvertedGO(effectiveRoot, src, mc2CapsuleType);
          var newGO = existingMc2CapsGO ?? CreateChildGameObject(src.gameObject.name + "_MC2CapsuleCollider", effectiveRoot);
          newGO.transform.localRotation = src.rotation;
          var c = newGO.GetComponent(mc2CapsuleType) ?? ObjectFactory.AddComponent(newGO, mc2CapsuleType);
          SetField(c, "center", src.position);

          var dirEnumType = GetNestedTypeDeep(mc2CapsuleType, "Direction");
          if (dirEnumType != null)
            SetField(c, "direction", Enum.ToObject(dirEnumType, 1));

          SetField(c, "radiusSeparation", false);
          SetField(c, "alignedOnCenter", true);

          var setSizeThree = mc2CapsuleType.GetMethod("SetSize", new[] { typeof(float), typeof(float), typeof(float) });
          if (setSizeThree != null)
            setSizeThree.Invoke(c, new object[] { src.radius, src.radius, src.height });
          else
            SetField(c, "size", new Vector3(src.radius, src.radius, src.height));

          AddMarkerIfNeeded(newGO, existingMc2CapsGO, src);
          EditorUtility.SetDirty(newGO);
          convertedObject = newGO;
          return "MagicaCapsuleCollider (MC2)";
        }
        case VRCPhysBoneColliderBase.ShapeType.Plane:
        {
          var existingMc2PlaneGO = FindExistingConvertedGO(effectiveRoot, src, mc2PlaneType);
          var newGO = existingMc2PlaneGO ?? CreateChildGameObject(src.gameObject.name + "_MC2PlaneCollider", effectiveRoot);
          newGO.transform.localRotation = src.rotation;
          var c = newGO.GetComponent(mc2PlaneType) ?? ObjectFactory.AddComponent(newGO, mc2PlaneType);
          SetField(c, "center", src.position);
          AddMarkerIfNeeded(newGO, existingMc2PlaneGO, src);
          EditorUtility.SetDirty(newGO);
          convertedObject = newGO;
          return "MagicaPlaneCollider (MC2)";
        }
        default:
          convertedObject = null;
          return "MagicaCloth2 (unknown shape)";
      }
    }

    private static void AddMarkerIfNeeded(GameObject newGO, GameObject existingGO, VRCPhysBoneCollider source)
    {
      if (existingGO != null)
        return;

      var marker = newGO.AddComponent<CVRFuryConvertedColliderMarker>();
      marker.sourceCollider = source;
    }

    private struct TargetTypes
    {
      public readonly Type dbType;
      public readonly Type mc1SphereType;
      public readonly Type mc1CapsuleType;
      public readonly Type mc1PlaneType;
      public readonly Type mc2SphereType;
      public readonly Type mc2CapsuleType;
      public readonly Type mc2PlaneType;

      public TargetTypes(
        Type dbType,
        Type mc1SphereType,
        Type mc1CapsuleType,
        Type mc1PlaneType,
        Type mc2SphereType,
        Type mc2CapsuleType,
        Type mc2PlaneType
      )
      {
        this.dbType = dbType;
        this.mc1SphereType = mc1SphereType;
        this.mc1CapsuleType = mc1CapsuleType;
        this.mc1PlaneType = mc1PlaneType;
        this.mc2SphereType = mc2SphereType;
        this.mc2CapsuleType = mc2CapsuleType;
        this.mc2PlaneType = mc2PlaneType;
      }
    }
  }
}
