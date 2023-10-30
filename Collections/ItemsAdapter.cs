using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria.ID;

namespace Terraria.Plugins.Common.Collections {
    /// <summary>
    ///   Terraria.Item[] to IList<ItemData> adapter.
    /// </summary>
    public class ItemsAdapter : IList<ItemData>
    {
    private static volatile Dictionary<int,int[]> PreferredSlots;
    protected readonly Item[] itemArray;
    protected readonly int fromIndex;
    protected readonly int toIndex;

    public ItemsAdapter(Item[] itemArray, int fromIndex = 0, int toIndex = 0) {
      if (itemArray == null) throw new ArgumentNullException();
      if (!(itemArray.Length > 0)) throw new ArgumentException();
      if (!(fromIndex <= toIndex)) throw new ArgumentOutOfRangeException();

      this.itemArray = itemArray;
      this.fromIndex = fromIndex;
      this.toIndex = toIndex > 0 ? toIndex : itemArray.Length - 1;
    }

    /// <summary>
    ///   Gets the indexes of the slot preferred for the given item type.
    /// </summary>
    /// <param name="itemType">Type of the item.</param>
    public virtual HashSet<int> PreferredSlotIndexes(int itemType) {
      return new HashSet<int>();
    }

    #region IList<ItemData> Implementation
    public int Count => (this.toIndex - this.fromIndex) + 1;
    public bool IsReadOnly => false;

    public virtual ItemData this[int index] {
      get { return ItemData.FromItem(this.itemArray[this.fromIndex + index]); }
      set {
        Item tItem = value.ToItem();
        this.itemArray[this.fromIndex + index] = tItem;
      }
    }

    public int IndexOf(ItemData item) {
      for (int i = 0; i < this.itemArray.Length; i++) {
        Item tItem = this.itemArray[0];
        if (tItem.netID == (int)item.Type && tItem.stack == item.StackSize && tItem.prefix == (int)item.Prefix)
          return i;
      }

      return -1;
    }

    public void Insert(int index, ItemData item) {
      throw new NotImplementedException();
    }

    public void RemoveAt(int index) {
      throw new NotImplementedException();
    }

    public IEnumerator<ItemData> GetEnumerator() {
      foreach (Item item in this.itemArray)
        yield return ItemData.FromItem(item);
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    public void Add(ItemData item) {
      throw new NotImplementedException();
    }

    public void Clear() {
      throw new NotImplementedException();
    }

    public bool Contains(ItemData item) {
      return this.IndexOf(item) != -1;
    }

    public void CopyTo(ItemData[] array, int arrayIndex) {
      for (int i = arrayIndex; i < Math.Min(array.Length, this.itemArray.Length); i++)
        array[i] = ItemData.FromItem(this.itemArray[i]);
    }

    public bool Remove(ItemData item) {
      throw new NotImplementedException();
    }
    #endregion
  }
}
