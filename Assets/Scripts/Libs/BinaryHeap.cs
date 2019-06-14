using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Libs
{
	/// <summary>
	///     A min-type priority queue of Nodes
	/// </summary>
	[NativeContainerSupportsDeallocateOnJobCompletion]
	[NativeContainerSupportsMinMaxWriteRestriction]
	[NativeContainer]
	public unsafe struct BinaryHeap<T, K> : IDisposable
		where T : struct
		where K : IComparable<K>
	{
		private readonly Allocator _mAllocatorLabel;
		[NativeDisableUnsafePtrRestriction] private void* _data;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		private AtomicSafetyHandle _mSafety;
		[NativeSetClassTypeToNullOnSchedule] private DisposeSentinel _mDisposeSentinel;
#endif
		private int _capacity;
		private readonly K[] _priorities;

		/// <summary>
		///     Creates a new, empty priority queue with the specified capacity.
		/// </summary>
		/// <param name="capacity">The maximum number of nodes that will be stored in the queue.</param>
		/// <param name="allocator">Allocator</param>
		public BinaryHeap(int capacity, Allocator allocator)
		{
			_mAllocatorLabel = allocator;
			_capacity = capacity;
			long size = (long) UnsafeUtility.SizeOf<T>() * capacity;
			if (allocator <= Allocator.None)
				throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), "Length must be >= 0");
			if (size > int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(capacity),
					$"Length * sizeof(T) cannot exceed {(object) int.MaxValue} bytes");

			_data = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), allocator);
			_priorities = new K[capacity];
			Count = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Create(out _mSafety, out _mDisposeSentinel, 1, allocator);
#endif
		}

		/// <summary>
		///     Adds an item to the queue.  Is position is determined by its priority relative to the other items in the queue.
		///     aka HeapInsert
		/// </summary>
		/// <param name="item">Item to add</param>
		/// <param name="priority">
		///     Priority value to attach to this item.  Note: this is a min heap, so lower priority values come
		///     out first.
		/// </param>
		public void Add(T item, K priority)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (Count >= _capacity)
				throw new IndexOutOfRangeException("Capacity Reached");
			AtomicSafetyHandle.CheckReadAndThrow(_mSafety);
#endif

			// Add the item to the heap in the end position of the array (i.e. as a leaf of the tree)
			int position = Count++;
			UnsafeUtility.WriteArrayElement(_data, position, item);
			_priorities[position] = priority;
			// Move it upward into position, if necessary
			MoveUp(position);
		}

		/// <summary>
		///     Extracts the item in the queue with the minimal priority value.
		/// </summary>
		/// <returns></returns>
		public T ExtractMin() // Probably THE most important function... Got everything working
		{
			var minNode = this[0];
			Swap(0, Count - 1);
			Count--;
			MoveDown(0);
			return minNode;
		}

		/// <summary>
		///     Moves the node at the specified position upward, it it violates the Heap Property.
		///     This is the while loop from the HeapInsert procedure in the slides.
		/// </summary>
		/// <param name="position"></param>
		private void MoveUp(int position)
		{
			while (position > 0 && _priorities[Parent(position)].CompareTo(_priorities[position]) > 0)
			{
				int originalParentPos = Parent(position);
				Swap(position, originalParentPos);
				position = originalParentPos;
			}
		}

		/// <summary>
		///     Moves the node at the specified position down, if it violates the Heap Property
		///     aka Heapify
		/// </summary>
		/// <param name="position"></param>
		private void MoveDown(int position)
		{
			int lChild = LeftChild(position);
			int rChild = RightChild(position);
			int largest = 0;
			if (lChild < Count && _priorities[lChild].CompareTo(_priorities[position]) < 0)
				largest = lChild;
			else
				largest = position;

			if (rChild < Count && _priorities[rChild].CompareTo(_priorities[largest]) < 0) largest = rChild;

			if (largest != position)
			{
				Swap(position, largest);
				MoveDown(largest);
			}
		}

		/// <summary>
		///     Number of items waiting in queue
		/// </summary>
		public int Count { get; private set; }

		public T this[int index]
		{
			get
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (index < 0 || index >= Count)
					FailOutOfRangeError(index);
				AtomicSafetyHandle.CheckReadAndThrow(_mSafety);
#endif

				return UnsafeUtility.ReadArrayElement<T>(_data, index);
			}
		}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		private void FailOutOfRangeError(int index)
		{
			throw new IndexOutOfRangeException(
				$"Index {(object) index} is out of range of '{(object) _capacity}' Length.");
		}
#endif

		/// <summary>
		///     Swaps the nodes at the respective positions in the heap
		///     Updates the nodes' QueuePosition properties accordingly.
		/// </summary>
		private void Swap(int position1, int position2)
		{
			var pos1 = this[position1];
			var pos2 = this[position2];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckReadAndThrow(_mSafety);
#endif
			UnsafeUtility.WriteArrayElement(_data, position1, pos2);
			UnsafeUtility.WriteArrayElement(_data, position2, pos1);

			K temp2 = _priorities[position1];
			_priorities[position1] = _priorities[position2];
			_priorities[position2] = temp2;
		}

		/// <summary>
		///     Gives the position of a node's parent, the node's position in the queue.
		/// </summary>
		private static int Parent(int position)
		{
			return (position - 1) / 2;
		}

		/// <summary>
		///     Returns the position of a node's left child, given the node's position.
		/// </summary>
		private static int LeftChild(int position)
		{
			return 2 * position + 1;
		}

		/// <summary>
		///     Returns the position of a node's right child, given the node's position.
		/// </summary>
		private static int RightChild(int position)
		{
			return 2 * position + 2;
		}

		/// <summary>
		///     Checks all entries in the heap to see if they satisfy the heap property.
		/// </summary>
		public void TestHeapValidity()
		{
			for (int i = 1; i < Count; i++)
				if (_priorities[Parent(i)].CompareTo(_priorities[i]) > 0)
					throw new Exception("Heap violates the Heap Property at position " + i);
		}

		public void Dispose()
		{
			if (!UnsafeUtility.IsValidAllocator(_mAllocatorLabel))
				throw new InvalidOperationException(
					"The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Dispose(ref _mSafety, ref _mDisposeSentinel);
#endif
			UnsafeUtility.Free(_data, _mAllocatorLabel);
			_data = null;
			Count = 0;
			_capacity = 0;
		}

		public void Clear()
		{
			Count = 0;
		}
	}
}