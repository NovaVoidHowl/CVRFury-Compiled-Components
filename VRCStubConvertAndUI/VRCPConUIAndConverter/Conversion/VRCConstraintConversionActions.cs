using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics.ManagedTypes;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrcconstraints
{
  public static class VRCConstraintConversionActions
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

    public static VRCConstraintConversionAvailability GetAvailability(Component source)
    {
      VRCConstraintKind kind;
      var availability = new VRCConstraintConversionAvailability
      {
        IsAvailable = TryGetKind(source, out kind),
        Kind = kind
      };

      if (!availability.IsAvailable)
      {
        availability.Messages.Add(
          new ConversionMessage(
            ConversionMessageSeverity.Error,
            "unsupported_constraint",
            "This component is not a supported VRC constraint conversion source."
          )
        );
      }

      return availability;
    }

    public static VRCConstraintConversionGuidance GetGuidance(Component source)
    {
      VRCConstraintKind kind;
      TryGetKind(source, out kind);

      var guidance = new VRCConstraintConversionGuidance { Kind = kind };
      guidance.Messages.Add(
        new ConversionMessage(
          ConversionMessageSeverity.Info,
          "constraint_destructive_conversion",
          "VRC constraint conversion adds the matching Unity constraint component to the same GameObject and removes the source VRC constraint after success."
        )
      );
      guidance.Messages.Add(
        new ConversionMessage(
          ConversionMessageSeverity.Info,
          "constraint_one_to_one_mapping",
          "Supported VRC constraints map one-to-one to Aim, LookAt, Parent, Position, Rotation, or Scale Unity constraints."
        )
      );
      return guidance;
    }

    public static VRCConstraintConversionResult Convert(
      Component source,
      VRCConstraintConversionOptions options
    )
    {
      if (source == null)
        return VRCConstraintConversionResult.Failed("Constraint Conversion Failed", "Source constraint is missing.", "missing_source");

      if (options == null)
        options = VRCConstraintConversionOptions.ForInspector();

      if (source is VRCAimConstraint)
        return ConvertAim((VRCAimConstraint)source, options);
      if (source is VRCLookAtConstraint)
        return ConvertLookAt((VRCLookAtConstraint)source, options);
      if (source is VRCParentConstraint)
        return ConvertParent((VRCParentConstraint)source, options);
      if (source is VRCPositionConstraint)
        return ConvertPosition((VRCPositionConstraint)source, options);
      if (source is VRCRotationConstraint)
        return ConvertRotation((VRCRotationConstraint)source, options);
      if (source is VRCScaleConstraint)
        return ConvertScale((VRCScaleConstraint)source, options);

      return VRCConstraintConversionResult.Failed(
        "Constraint Conversion Failed",
        "This component is not a supported VRC constraint conversion source.",
        "unsupported_constraint"
      );
    }

    private static VRCConstraintConversionResult ConvertAim(
      VRCAimConstraint source,
      VRCConstraintConversionOptions options
    )
    {
      var unityConstraint = AddComponent<AimConstraint>(source.gameObject, options);

      unityConstraint.weight = source.GlobalWeight;
      unityConstraint.constraintActive = source.IsActive;
      unityConstraint.aimVector = source.AimAxis;
      unityConstraint.upVector = source.UpAxis;

      switch (source.WorldUp)
      {
        case VRC.Dynamics.ManagedTypes.VRCConstraintBase.WorldUpType.SceneUp:
          unityConstraint.worldUpType = AimConstraint.WorldUpType.SceneUp;
          break;
        case VRC.Dynamics.ManagedTypes.VRCConstraintBase.WorldUpType.ObjectUp:
          unityConstraint.worldUpType = AimConstraint.WorldUpType.ObjectUp;
          unityConstraint.worldUpObject = source.WorldUpTransform;
          break;
        case VRC.Dynamics.ManagedTypes.VRCConstraintBase.WorldUpType.ObjectRotationUp:
          unityConstraint.worldUpType = AimConstraint.WorldUpType.ObjectRotationUp;
          unityConstraint.worldUpObject = source.WorldUpTransform;
          break;
        case VRC.Dynamics.ManagedTypes.VRCConstraintBase.WorldUpType.Vector:
          unityConstraint.worldUpType = AimConstraint.WorldUpType.Vector;
          unityConstraint.worldUpVector = source.WorldUpVector;
          break;
        case VRC.Dynamics.ManagedTypes.VRCConstraintBase.WorldUpType.None:
          unityConstraint.worldUpType = AimConstraint.WorldUpType.None;
          break;
      }

      AddSources(source.Sources, unityConstraint);

      unityConstraint.rotationAxis = 0;
      if (source.AffectsRotationX)
        unityConstraint.rotationAxis |= Axis.X;
      if (source.AffectsRotationY)
        unityConstraint.rotationAxis |= Axis.Y;
      if (source.AffectsRotationZ)
        unityConstraint.rotationAxis |= Axis.Z;

      return BuildSuccess(source, unityConstraint, VRCConstraintKind.Aim, "Unity AimConstraint", options);
    }

    private static VRCConstraintConversionResult ConvertLookAt(
      VRCLookAtConstraint source,
      VRCConstraintConversionOptions options
    )
    {
      var unityConstraint = AddComponent<LookAtConstraint>(source.gameObject, options);

      unityConstraint.weight = source.GlobalWeight;
      unityConstraint.constraintActive = source.IsActive;
      unityConstraint.roll = source.Roll;
      unityConstraint.useUpObject = source.UseUpTransform;
      if (source.UseUpTransform && source.WorldUpTransform != null)
        unityConstraint.worldUpObject = source.WorldUpTransform;

      AddSources(source.Sources, unityConstraint);

      return BuildSuccess(source, unityConstraint, VRCConstraintKind.LookAt, "Unity LookAtConstraint", options);
    }

    private static VRCConstraintConversionResult ConvertParent(
      VRCParentConstraint source,
      VRCConstraintConversionOptions options
    )
    {
      var unityConstraint = AddComponent<ParentConstraint>(source.gameObject, options);

      unityConstraint.weight = source.GlobalWeight;
      unityConstraint.constraintActive = source.IsActive;
      unityConstraint.translationAtRest = source.PositionAtRest;
      unityConstraint.rotationAtRest = source.RotationAtRest;

      for (int i = 0; i < source.Sources.Count; i++)
      {
        var vrcSource = source.Sources[i];
        if (vrcSource.SourceTransform == null)
          continue;

        var unitySource = new ConstraintSource
        {
          sourceTransform = vrcSource.SourceTransform,
          weight = vrcSource.Weight
        };
        int sourceIndex = unityConstraint.AddSource(unitySource);
        unityConstraint.SetTranslationOffset(sourceIndex, vrcSource.ParentPositionOffset);
        unityConstraint.SetRotationOffset(sourceIndex, vrcSource.ParentRotationOffset);
      }

      unityConstraint.translationAxis = 0;
      if (source.AffectsPositionX)
        unityConstraint.translationAxis |= Axis.X;
      if (source.AffectsPositionY)
        unityConstraint.translationAxis |= Axis.Y;
      if (source.AffectsPositionZ)
        unityConstraint.translationAxis |= Axis.Z;

      unityConstraint.rotationAxis = 0;
      if (source.AffectsRotationX)
        unityConstraint.rotationAxis |= Axis.X;
      if (source.AffectsRotationY)
        unityConstraint.rotationAxis |= Axis.Y;
      if (source.AffectsRotationZ)
        unityConstraint.rotationAxis |= Axis.Z;

      return BuildSuccess(source, unityConstraint, VRCConstraintKind.Parent, "Unity ParentConstraint", options);
    }

    private static VRCConstraintConversionResult ConvertPosition(
      VRCPositionConstraint source,
      VRCConstraintConversionOptions options
    )
    {
      var unityConstraint = AddComponent<PositionConstraint>(source.gameObject, options);

      unityConstraint.weight = source.GlobalWeight;
      unityConstraint.constraintActive = source.IsActive;
      unityConstraint.translationAtRest = source.PositionAtRest;
      unityConstraint.translationOffset = source.PositionOffset;

      AddSources(source.Sources, unityConstraint);

      unityConstraint.translationAxis = 0;
      if (source.AffectsPositionX)
        unityConstraint.translationAxis |= Axis.X;
      if (source.AffectsPositionY)
        unityConstraint.translationAxis |= Axis.Y;
      if (source.AffectsPositionZ)
        unityConstraint.translationAxis |= Axis.Z;

      return BuildSuccess(source, unityConstraint, VRCConstraintKind.Position, "Unity PositionConstraint", options);
    }

    private static VRCConstraintConversionResult ConvertRotation(
      VRCRotationConstraint source,
      VRCConstraintConversionOptions options
    )
    {
      var unityConstraint = AddComponent<RotationConstraint>(source.gameObject, options);

      unityConstraint.weight = source.GlobalWeight;
      unityConstraint.constraintActive = source.IsActive;
      unityConstraint.rotationAtRest = source.RotationAtRest;
      unityConstraint.rotationOffset = source.RotationOffset;

      AddSources(source.Sources, unityConstraint);

      unityConstraint.rotationAxis = 0;
      if (source.AffectsRotationX)
        unityConstraint.rotationAxis |= Axis.X;
      if (source.AffectsRotationY)
        unityConstraint.rotationAxis |= Axis.Y;
      if (source.AffectsRotationZ)
        unityConstraint.rotationAxis |= Axis.Z;

      return BuildSuccess(source, unityConstraint, VRCConstraintKind.Rotation, "Unity RotationConstraint", options);
    }

    private static VRCConstraintConversionResult ConvertScale(
      VRCScaleConstraint source,
      VRCConstraintConversionOptions options
    )
    {
      var unityConstraint = AddComponent<ScaleConstraint>(source.gameObject, options);

      unityConstraint.weight = source.GlobalWeight;
      unityConstraint.constraintActive = source.IsActive;
      unityConstraint.scaleAtRest = source.ScaleAtRest;
      unityConstraint.scaleOffset = source.ScaleOffset;

      AddSources(source.Sources, unityConstraint);

      unityConstraint.scalingAxis = 0;
      if (source.AffectsScaleX)
        unityConstraint.scalingAxis |= Axis.X;
      if (source.AffectsScaleY)
        unityConstraint.scalingAxis |= Axis.Y;
      if (source.AffectsScaleZ)
        unityConstraint.scalingAxis |= Axis.Z;

      return BuildSuccess(source, unityConstraint, VRCConstraintKind.Scale, "Unity ScaleConstraint", options);
    }

    private static T AddComponent<T>(GameObject gameObject, VRCConstraintConversionOptions options)
      where T : Component
    {
      if (options.RegisterUndo)
        return Undo.AddComponent<T>(gameObject);

      return gameObject.AddComponent<T>();
    }

    private static void AddSources<T>(VRCConstraintSourceKeyableList sources, T unityConstraint)
      where T : Behaviour, IConstraint
    {
      for (int i = 0; i < sources.Count; i++)
      {
        var vrcSource = sources[i];
        if (vrcSource.SourceTransform == null)
          continue;

        var unitySource = new ConstraintSource
        {
          sourceTransform = vrcSource.SourceTransform,
          weight = vrcSource.Weight
        };
        unityConstraint.AddSource(unitySource);
      }
    }

    private static VRCConstraintConversionResult BuildSuccess(
      Component source,
      Component unityConstraint,
      VRCConstraintKind kind,
      string targetName,
      VRCConstraintConversionOptions options
    )
    {
      var gameObject = source.gameObject;
      var result = new VRCConstraintConversionResult
      {
        Success = true,
        Kind = kind,
        SourceGameObject = gameObject,
        CreatedConstraint = unityConstraint,
        SummaryTitle = "Constraint Conversion Complete",
        SummaryMessage = "Successfully converted to " + targetName + "."
      };

      foreach (var message in GetGuidance(source).Messages)
        result.Messages.Add(message);

      EditorUtility.SetDirty(gameObject);
      EditorUtility.SetDirty(unityConstraint);

      if (options.RemoveSourceComponent)
      {
        DestroyComponent(source, options);
        result.SourceRemoved = true;
      }

      return result;
    }

    private static void DestroyComponent(Component component, VRCConstraintConversionOptions options)
    {
      if (options.RegisterUndo)
        Undo.DestroyObjectImmediate(component);
      else
        UnityEngine.Object.DestroyImmediate(component);
    }

    private static bool TryGetKind(Component source, out VRCConstraintKind kind)
    {
      if (source is VRCAimConstraint)
      {
        kind = VRCConstraintKind.Aim;
        return true;
      }
      if (source is VRCLookAtConstraint)
      {
        kind = VRCConstraintKind.LookAt;
        return true;
      }
      if (source is VRCParentConstraint)
      {
        kind = VRCConstraintKind.Parent;
        return true;
      }
      if (source is VRCPositionConstraint)
      {
        kind = VRCConstraintKind.Position;
        return true;
      }
      if (source is VRCRotationConstraint)
      {
        kind = VRCConstraintKind.Rotation;
        return true;
      }
      if (source is VRCScaleConstraint)
      {
        kind = VRCConstraintKind.Scale;
        return true;
      }

      kind = default(VRCConstraintKind);
      return false;
    }
  }
}
