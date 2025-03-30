using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRC.Dynamics
{
  public abstract class ContactBase : MonoBehaviour
  {
    public const float MAX_SIZE = 6;
    public const int MAX_COLLISION_TAGS = 16;
    public static Func<int, int, bool> OnValidatePlayers;
    public static Func<ContactBase, bool> OnInitialize;

    public Transform rootTransform;
    public CollisionScene.Shape shape;
    public bool allowInit;

    public List<string> collisionTags;
    public int playerId;

    public Vector3 position;

    public float height;

    public float radius;

    public ShapeType shapeType;

    public Quaternion rotation;

    public Vector3 axis { get; }

    [Serializable]
    public enum ShapeType
    {
      Sphere = 0,
      Capsule = 1
    }
  }
}
