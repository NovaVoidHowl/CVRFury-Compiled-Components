using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [AddComponentMenu("")]
  public class VRCParentConstraintBase : VRCConstraintBase
  {
    public Vector3 PositionAtRest = Vector3.zero;
    public bool AffectsPositionX = true;
    public bool AffectsPositionY = true;
    public bool AffectsPositionZ = true;
    public Vector3 RotationAtRest = Vector3.zero;
    public bool AffectsRotationX = true;
    public bool AffectsRotationY = true;
    public bool AffectsRotationZ = true;

    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.ChildPosition;
    protected override VRCConstraintRotationMode RotationMode => VRCConstraintRotationMode.MatchRotation;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.None;

    public sealed override bool AffectsAnyAxis()
    {
      return AffectsPositionX
        || AffectsPositionY
        || AffectsPositionZ
        || AffectsRotationX
        || AffectsRotationY
        || AffectsRotationZ;
    }
  }
}
