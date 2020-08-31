using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AutoSplitVideo.Core.DataStructure
{
	public class ObservableQueue<T> : ConcurrentQueue<T>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		public int MaxSize { get; }

		public string Text => string.Join(Environment.NewLine, this);

		public ObservableQueue(int max)
		{
			MaxSize = max;
		}

		public new void Clear()
		{
			base.Clear();
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public new bool TryDequeue(out T item)
		{
			T dequeueItem;
			var dequeueResult = base.TryDequeue(out dequeueItem);
			if (dequeueResult)
			{
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, dequeueItem));
			}
			item = dequeueItem;

			return dequeueResult;
		}

		public new void Enqueue(T item)
		{
			base.Enqueue(item);
			while (Count > MaxSize)
			{
				_ = TryDequeue(out _);
			}
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			RaiseCollectionChanged(e);
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(Text)));
		}

		private void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			RaisePropertyChanged(e);
		}

		private event PropertyChangedEventHandler PropertyChanged;

		private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}

		private void RaisePropertyChanged(PropertyChangedEventArgs e)
		{
			PropertyChanged?.Invoke(this, e);
		}

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add => PropertyChanged += value;
			remove => PropertyChanged -= value;
		}
	}
}
