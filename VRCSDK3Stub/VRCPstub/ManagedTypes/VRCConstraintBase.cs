using System;
using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [ExecuteInEditMode]
  public abstract class VRCConstraintBase : MonoBehaviour, IDisposable
  {
    // Core enum types needed for conversion
    public enum WorldUpType
    {
      SceneUp,
      ObjectUp,
      ObjectRotationUp,
      Vector,
      None
    }

    [Flags]
    public enum Axis
    {
      None = 0,
      X = 1,
      Y = 2,
      Z = 4,
      All = -1
    }

    // Essential constraint properties used during conversion
    public bool IsActive;
    public float GlobalWeight = 1f;
    public Transform TargetTransform;
    public bool SolveInLocalSpace;
    public bool FreezeToWorld;
    public VRCConstraintSourceKeyableList Sources;

    // Basic constraint type definitions
    protected enum VRCConstraintPositionMode
    {
      None,
      MatchPosition,
      ChildPosition
    }

    protected enum VRCConstraintRotationMode
    {
      None,
      MatchRotation,
      LookAtPosition,
      AimTowardsPosition
    }

    protected enum VRCConstraintScaleMode
    {
      None,
      MatchScale
    }

    // Essential properties for determining constraint behavior
    protected abstract VRCConstraintPositionMode PositionMode { get; }
    protected abstract VRCConstraintRotationMode RotationMode { get; }
    protected abstract VRCConstraintScaleMode ScaleMode { get; }

    // Basic property access needed for UI and conversion
    public bool AffectsPosition => PositionMode != VRCConstraintPositionMode.None;
    public bool AffectsRotation => RotationMode != VRCConstraintRotationMode.None;
    public bool AffectsScale => ScaleMode != VRCConstraintScaleMode.None;

    // Abstract method needed to determine if constraint affects any axis
    public abstract bool AffectsAnyAxis();

    // Minimal implementation of required methods
    public void Dispose() { }

    // Helper method used by derived classes
    protected Axis CreateAxisBitfield(bool x, bool y, bool z)
    {
      Axis axis = Axis.None;
      if (x)
        axis |= Axis.X;
      if (y)
        axis |= Axis.Y;
      if (z)
        axis |= Axis.Z;
      return axis;
    }

    // Basic helper for conversion to get the target transform
    internal Transform GetEffectiveTargetTransform() => TargetTransform != null ? TargetTransform : transform;
  }
}
