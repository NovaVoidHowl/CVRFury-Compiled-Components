using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  public abstract class VRCWorldUpConstraintBase : VRCConstraintBase
  {
    // Essential properties for worldup-based constraints
    public Vector3 RotationAtRest = Vector3.zero;
    public Vector3 RotationOffset = Vector3.zero;
    public Transform WorldUpTransform;

    // Override position/scale modes for worldup-based constraints
    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.None;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.None;

    // Flag to determine if this constraint uses a world up transform
    protected virtual bool UsesWorldUpTransform => false;
  }
}
