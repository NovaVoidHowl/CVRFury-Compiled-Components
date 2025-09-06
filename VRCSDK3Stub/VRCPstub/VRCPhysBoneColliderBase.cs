using System;
using UnityEngine;

namespace VRC.Dynamics
{
  public abstract class VRCPhysBoneColliderBase : MonoBehaviour
  {
    public Transform rootTransform;
    public CollisionScene.Shape shape;
    public int playerId;
    public bool bonesAsSpheres;
    public Quaternion rotation;
    public bool isGlobalCollider;
    public float height;
    public float radius;
    public bool insideBounds;
    public ShapeType shapeType;
    public Vector3 position;

    public Vector3 axis { get; }

    [Serializable]
    public enum ShapeType
    {
      Sphere = 0,
      Capsule = 1,
      Plane = 2
    }
  }
}
