using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [AddComponentMenu("")]
  public class VRCScaleConstraintBase : VRCConstraintBase
  {
    public Vector3 ScaleAtRest = Vector3.one;
    public Vector3 ScaleOffset = Vector3.one;
    public bool AffectsScaleX = true;
    public bool AffectsScaleY = true;
    public bool AffectsScaleZ = true;

    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.None;
    protected override VRCConstraintRotationMode RotationMode => VRCConstraintRotationMode.None;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.MatchScale;

    public sealed override bool AffectsAnyAxis()
    {
      return AffectsScaleX || AffectsScaleY || AffectsScaleZ;
    }
  }
}
