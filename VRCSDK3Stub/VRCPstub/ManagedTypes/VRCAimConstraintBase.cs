using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [AddComponentMenu("")]
  public class VRCAimConstraintBase : VRCWorldUpConstraintBase
  {
    public bool AffectsRotationX = true;
    public bool AffectsRotationY = true;
    public bool AffectsRotationZ = true;
    public Vector3 AimAxis = Vector3.forward;
    public Vector3 UpAxis = Vector3.up;
    public WorldUpType WorldUp;
    public Vector3 WorldUpVector = Vector3.up;

    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.None;
    protected override VRCConstraintRotationMode RotationMode => VRCConstraintRotationMode.AimTowardsPosition;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.None;

    protected override bool UsesWorldUpTransform =>
      WorldUp == WorldUpType.ObjectUp || WorldUp == WorldUpType.ObjectRotationUp;

    public sealed override bool AffectsAnyAxis()
    {
      return AffectsRotationX || AffectsRotationY || AffectsRotationZ;
    }
  }
}
