using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [AddComponentMenu("")]
  public class VRCRotationConstraintBase : VRCConstraintBase
  {
    public Vector3 RotationAtRest = Vector3.zero;
    public Vector3 RotationOffset = Vector3.zero;
    public bool AffectsRotationX = true;
    public bool AffectsRotationY = true;
    public bool AffectsRotationZ = true;

    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.None;
    protected override VRCConstraintRotationMode RotationMode => VRCConstraintRotationMode.MatchRotation;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.None;

    public sealed override bool AffectsAnyAxis()
    {
      return AffectsRotationX || AffectsRotationY || AffectsRotationZ;
    }
  }
}
