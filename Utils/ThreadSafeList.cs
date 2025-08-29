namespace PCL.Core.Utils;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 需要 using System.Linq; 来使用 ToArray() 和 ToList()

public class ThreadSafeList<T> : IList<T> {
    private readonly List<T> _list = [];
    private readonly object _syncRoot = new();

    public ThreadSafeList() { }

    public ThreadSafeList(IEnumerable<T> collection) {
        lock (_syncRoot) {
            _list.AddRange(collection);
        }
    }

    public T this[int index] {
        get {
            lock (_syncRoot) {
                if (index < 0 || index >= _list.Count) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return _list[index];
            }
        }
        set {
            lock (_syncRoot) {
                if (index < 0 || index >= _list.Count) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                _list[index] = value;
            }
        }
    }

    public int Count {
        get {
            lock (_syncRoot) {
                return _list.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public void Add(T item) {
        lock (_syncRoot) {
            _list.Add(item);
        }
    }

    public void Clear() {
        lock (_syncRoot) {
            _list.Clear();
        }
    }

    public bool Contains(T item) {
        lock (_syncRoot) {
            return _list.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex) {
        lock (_syncRoot) {
            _list.CopyTo(array, arrayIndex);
        }
    }

    public bool Remove(T item) {
        lock (_syncRoot) {
            return _list.Remove(item);
        }
    }

    public int IndexOf(T item) {
        lock (_syncRoot) {
            return _list.IndexOf(item);
        }
    }

    public void Insert(int index, T item) {
        lock (_syncRoot) {
            _list.Insert(index, item);
        }
    }

    public void RemoveAt(int index) {
        lock (_syncRoot) {
            _list.RemoveAt(index);
        }
    }

    // 修复后的 GetEnumerator 方法
    public IEnumerator<T> GetEnumerator() {
        lock (_syncRoot) {
            // 返回一个副本的枚举器，防止遍历时修改导致的异常
            return _list.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
