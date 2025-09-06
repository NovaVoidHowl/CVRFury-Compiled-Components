using System;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [Serializable]
  public struct VRCConstraintSource
  {
    public Transform SourceTransform;

    public float Weight;

    public Vector3 ParentPositionOffset;

    public Vector3 ParentRotationOffset;

    public VRCConstraintSource(Transform transform, float weight)
      : this(transform, weight, Vector3.zero, Vector3.zero) { }

    public VRCConstraintSource(
      Transform transform,
      float weight,
      Vector3 parentPositionOffset,
      Vector3 parentRotationOffset
    )
    {
      SourceTransform = transform;
      Weight = weight;
      ParentPositionOffset = parentPositionOffset;
      ParentRotationOffset = parentRotationOffset;
    }

    public static VRCConstraintSource CreateDefault()
    {
      return new VRCConstraintSource(null, 1f, Vector3.zero, Vector3.zero);
    }
  }

  [Serializable]
  public struct VRCConstraintSourceKeyableList
    : IList<VRCConstraintSource>,
      ICollection<VRCConstraintSource>,
      IEnumerable<VRCConstraintSource>,
      System.Collections.IEnumerable,
      System.Collections.IList,
      System.Collections.ICollection
  {
    private struct KeyableListEnumerator : IEnumerator<VRCConstraintSource>, System.Collections.IEnumerator, IDisposable
    {
      private VRCConstraintSourceKeyableList _keyableList;

      private int _index;

      public VRCConstraintSource Current => _keyableList.Get(_index);

      object System.Collections.IEnumerator.Current => Current;

      public KeyableListEnumerator(ref VRCConstraintSourceKeyableList list)
      {
        _keyableList = list;
        _index = -1;
      }

      public bool MoveNext()
      {
        _index++;
        return _index < _keyableList.Count;
      }

      public void Reset()
      {
        _index = -1;
      }

      public void Dispose() { }
    }

    public const int MaxFlatLength = 16;

    [SerializeField]
    private VRCConstraintSource source0;

    [SerializeField]
    private VRCConstraintSource source1;

    [SerializeField]
    private VRCConstraintSource source2;

    [SerializeField]
    private VRCConstraintSource source3;

    [SerializeField]
    private VRCConstraintSource source4;

    [SerializeField]
    private VRCConstraintSource source5;

    [SerializeField]
    private VRCConstraintSource source6;

    [SerializeField]
    private VRCConstraintSource source7;

    [SerializeField]
    private VRCConstraintSource source8;

    [SerializeField]
    private VRCConstraintSource source9;

    [SerializeField]
    private VRCConstraintSource source10;

    [SerializeField]
    private VRCConstraintSource source11;

    [SerializeField]
    private VRCConstraintSource source12;

    [SerializeField]
    private VRCConstraintSource source13;

    [SerializeField]
    private VRCConstraintSource source14;

    [SerializeField]
    private VRCConstraintSource source15;

    [SerializeField]
    [HideInInspector]
    [NotKeyable]
    private int totalLength;

    [SerializeField]
    [NotKeyable]
    private List<VRCConstraintSource> overflowList;

    private IEnumerator<VRCConstraintSource> _valueEnumerator;

    public int Count => totalLength;

    private List<VRCConstraintSource> OverflowList
    {
      get
      {
        if (overflowList == null)
        {
          overflowList = new List<VRCConstraintSource>();
        }
        return overflowList;
      }
    }

    private IEnumerator<VRCConstraintSource> ValueEnumerator
    {
      get
      {
        if (_valueEnumerator == null)
        {
          _valueEnumerator = new KeyableListEnumerator(ref this);
        }
        else
        {
          _valueEnumerator.Reset();
        }
        return _valueEnumerator;
      }
    }

    public bool IsReadOnly => false;

    public bool IsFixedSize => false;

    bool System.Collections.ICollection.IsSynchronized => true;

    object System.Collections.ICollection.SyncRoot
    {
      get { throw new NotSupportedException("SyncRoot is not supported on this list type."); }
    }

    object System.Collections.IList.this[int index]
    {
      get { return Get(index); }
      set { Set(index, (VRCConstraintSource)value); }
    }

    public VRCConstraintSource this[int index]
    {
      get { return Get(index); }
      set { Set(index, value); }
    }

    public VRCConstraintSourceKeyableList(int initialLength)
    {
      if (initialLength < 0)
      {
        throw new ArgumentOutOfRangeException("initialLength", "The initial length cannot be a value less than zero.");
      }
      int num = Mathf.Max(0, initialLength - 16);
      overflowList = new List<VRCConstraintSource>(num);
      for (int i = 0; i < num; i++)
      {
        overflowList.Add(VRCConstraintSource.CreateDefault());
      }
      totalLength = initialLength;
      source0 = default(VRCConstraintSource);
      source1 = default(VRCConstraintSource);
      source2 = default(VRCConstraintSource);
      source3 = default(VRCConstraintSource);
      source4 = default(VRCConstraintSource);
      source5 = default(VRCConstraintSource);
      source6 = default(VRCConstraintSource);
      source7 = default(VRCConstraintSource);
      source8 = default(VRCConstraintSource);
      source9 = default(VRCConstraintSource);
      source10 = default(VRCConstraintSource);
      source11 = default(VRCConstraintSource);
      source12 = default(VRCConstraintSource);
      source13 = default(VRCConstraintSource);
      source14 = default(VRCConstraintSource);
      source15 = default(VRCConstraintSource);
      _valueEnumerator = null;
    }

    public VRCConstraintSourceKeyableList(IList<VRCConstraintSource> list)
    {
      if (list == null)
      {
        throw new ArgumentNullException("list", "A list must be defined.");
      }
      int count = list.Count;
      int num = Mathf.Max(0, count - 16);
      overflowList = new List<VRCConstraintSource>(num);
      for (int i = 0; i < num; i++)
      {
        overflowList.Add(list[i + 16]);
      }
      totalLength = count;
      source0 = ((count > 0) ? list[0] : default(VRCConstraintSource));
      source1 = ((count > 1) ? list[1] : default(VRCConstraintSource));
      source2 = ((count > 2) ? list[2] : default(VRCConstraintSource));
      source3 = ((count > 3) ? list[3] : default(VRCConstraintSource));
      source4 = ((count > 4) ? list[4] : default(VRCConstraintSource));
      source5 = ((count > 5) ? list[5] : default(VRCConstraintSource));
      source6 = ((count > 6) ? list[6] : default(VRCConstraintSource));
      source7 = ((count > 7) ? list[7] : default(VRCConstraintSource));
      source8 = ((count > 8) ? list[8] : default(VRCConstraintSource));
      source9 = ((count > 9) ? list[9] : default(VRCConstraintSource));
      source10 = ((count > 10) ? list[10] : default(VRCConstraintSource));
      source11 = ((count > 11) ? list[11] : default(VRCConstraintSource));
      source12 = ((count > 12) ? list[12] : default(VRCConstraintSource));
      source13 = ((count > 13) ? list[13] : default(VRCConstraintSource));
      source14 = ((count > 14) ? list[14] : default(VRCConstraintSource));
      source15 = ((count > 15) ? list[15] : default(VRCConstraintSource));
      _valueEnumerator = null;
    }

    private VRCConstraintSource Get(int index)
    {
      return index switch
      {
        0 => source0,
        1 => source1,
        2 => source2,
        3 => source3,
        4 => source4,
        5 => source5,
        6 => source6,
        7 => source7,
        8 => source8,
        9 => source9,
        10 => source10,
        11 => source11,
        12 => source12,
        13 => source13,
        14 => source14,
        15 => source15,
        _ => OverflowList[index - 16],
      };
    }

    private void Set(int index, VRCConstraintSource value)
    {
      switch (index)
      {
        case 0:
          source0 = value;
          return;
        case 1:
          source1 = value;
          return;
        case 2:
          source2 = value;
          return;
        case 3:
          source3 = value;
          return;
        case 4:
          source4 = value;
          return;
        case 5:
          source5 = value;
          return;
        case 6:
          source6 = value;
          return;
        case 7:
          source7 = value;
          return;
        case 8:
          source8 = value;
          return;
        case 9:
          source9 = value;
          return;
        case 10:
          source10 = value;
          return;
        case 11:
          source11 = value;
          return;
        case 12:
          source12 = value;
          return;
        case 13:
          source13 = value;
          return;
        case 14:
          source14 = value;
          return;
        case 15:
          source15 = value;
          return;
      }
      int num = index - 16;
      if (num < OverflowList.Count)
      {
        OverflowList[num] = value;
        return;
      }
      if (num == OverflowList.Count)
      {
        OverflowList.Add(value);
        return;
      }
      throw new ArgumentOutOfRangeException(
        $"Index {index} is out of range of the overflow buffer ({num} vs {OverflowList.Count})"
      );
    }

    public IEnumerator<VRCConstraintSource> GetEnumerator()
    {
      return ValueEnumerator;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    int System.Collections.IList.Add(object item)
    {
      Add((VRCConstraintSource)item);
      return totalLength - 1;
    }

    public void Add(VRCConstraintSource item)
    {
      Set(totalLength, item);
      totalLength++;
    }

    public void Clear()
    {
      totalLength = 0;
      OverflowList.Clear();
    }

    int System.Collections.IList.IndexOf(object item)
    {
      return IndexOf((VRCConstraintSource)item);
    }

    public int IndexOf(VRCConstraintSource item)
    {
      for (int i = 0; i < totalLength; i++)
      {
        if (Get(i).Equals(item))
        {
          return i;
        }
      }
      return -1;
    }

    bool System.Collections.IList.Contains(object item)
    {
      for (int i = 0; i < totalLength; i++)
      {
        if (Get(i).Equals(item))
        {
          return true;
        }
      }
      return false;
    }

    public bool Contains(VRCConstraintSource item)
    {
      for (int i = 0; i < totalLength; i++)
      {
        if (Get(i).Equals(item))
        {
          return true;
        }
      }
      return false;
    }

    void System.Collections.ICollection.CopyTo(Array array, int arrayIndex)
    {
      if (array == null)
      {
        throw new ArgumentNullException("array", "The array cannot be null.");
      }
      if (arrayIndex < 0)
      {
        throw new ArgumentOutOfRangeException("arrayIndex", "The starting array index cannot be negative.");
      }
      if (totalLength > array.Length - arrayIndex + 1)
      {
        throw new ArgumentException("The destination array has fewer elements than the collection.");
      }
      for (int i = 0; i < totalLength; i++)
      {
        array.SetValue(Get(i), i + arrayIndex);
      }
    }

    public void CopyTo(VRCConstraintSource[] array, int arrayIndex)
    {
      if (array == null)
      {
        throw new ArgumentNullException("array", "The array cannot be null.");
      }
      if (arrayIndex < 0)
      {
        throw new ArgumentOutOfRangeException("arrayIndex", "The starting array index cannot be negative.");
      }
      if (totalLength > array.Length - arrayIndex + 1)
      {
        throw new ArgumentException("The destination array has fewer elements than the collection.");
      }
      for (int i = 0; i < totalLength; i++)
      {
        array[i + arrayIndex] = Get(i);
      }
    }

    void System.Collections.IList.Remove(object item)
    {
      Remove((VRCConstraintSource)item);
    }

    public bool Remove(VRCConstraintSource item)
    {
      for (int i = 0; i < totalLength; i++)
      {
        if (Get(i).Equals(item))
        {
          RemoveAt(i);
          return true;
        }
      }
      return false;
    }

    public void RemoveAt(int index)
    {
      if (index < 16)
      {
        while (index < 15 && index < totalLength - 1)
        {
          Set(index, Get(index + 1));
          index++;
        }
        if (totalLength > 16)
        {
          Set(15, OverflowList[0]);
          OverflowList.RemoveAt(0);
        }
      }
      else
      {
        int index2 = index - 16;
        OverflowList.RemoveAt(index2);
      }
      totalLength--;
    }

    void System.Collections.IList.Insert(int index, object value)
    {
      Insert(index, (VRCConstraintSource)value);
    }

    public void Insert(int index, VRCConstraintSource item)
    {
      if (index >= 16)
      {
        int index2 = index - 16;
        OverflowList.Insert(index2, item);
        totalLength++;
        return;
      }
      for (int num = totalLength; num > index; num--)
      {
        Set(num, Get(num - 1));
      }
      Set(index, item);
      totalLength++;
    }

    public void SetLength(int newLength)
    {
      if (newLength < 0)
      {
        throw new ArgumentOutOfRangeException("newLength", "The length must be an integer greater than zero.");
      }
      if (newLength > totalLength)
      {
        for (int i = totalLength; i < Mathf.Min(newLength, 16); i++)
        {
          Set(i, VRCConstraintSource.CreateDefault());
        }
        int num = newLength - 16;
        int count = OverflowList.Count;
        for (int j = 0; j < num - count; j++)
        {
          OverflowList.Add(VRCConstraintSource.CreateDefault());
        }
      }
      else if (newLength < totalLength)
      {
        int num2 = Mathf.Max(0, newLength - 16);
        OverflowList.RemoveRange(num2, OverflowList.Count - num2);
      }
      totalLength = newLength;
    }
  }
}
