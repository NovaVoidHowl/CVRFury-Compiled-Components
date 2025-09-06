using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [AddComponentMenu("")]
  public class VRCLookAtConstraintBase : VRCWorldUpConstraintBase
  {
    public float Roll;
    public bool UseUpTransform;

    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.None;
    protected override VRCConstraintRotationMode RotationMode => VRCConstraintRotationMode.LookAtPosition;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.None;
    protected override bool UsesWorldUpTransform => UseUpTransform;

    public sealed override bool AffectsAnyAxis()
    {
      return true;
    }
  }
}
