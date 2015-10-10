using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace WinRT.Collections
{
    /// <summary>
    /// <para>Represents a strongly typed collection of objects that can be accessed by index.</para>
    /// <para>Supports out of the box:</para>Supports observability and incremental loading.
    /// <para>Observability: all changes inside in the collection are observed by binding clients.</para>
    /// <para>Incremental: supports collection items paging by loading more data on scrolling data items control event trigger.</para>
    /// <para>IsLoading property: exposes the state of ItemsLoader delegate if it is loading or not (created to be bound to ProgressBar or ProgressRing if needed).</para>
    /// </summary>
    /// <typeparam name="T">The type of elements in the WinRT.Collections.IncrementalLoadingCollection&lt;T&gt;.</typeparam>
    public class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading, INotifyPropertyChanged
    {

        /// <summary>
        /// <para>Func delegate gets paged items from data source.</para>
        /// <para>Parameters:</para>
        /// <para>int: Page number.</para>
        ///<para>int: Page size (Number of items per page).</para>
        ///<para>Returns: Task&lt;IEnumerable&lt;T&gt;&gt; (Current page items).</para>
        /// </summary>        
        public Func<int, int, Task<IEnumerable<T>>> ItemsLoader { get; private set; }

        /// <summary>
        /// Determines current page index.
        /// </summary>
        public int CurrentPage { get; private set; }

        /// <summary>
        /// Determines page size (Number of items per page).
        /// </summary>
        public int PageSize { get; private set; }

        private bool isLoading;
        /// <summary>
        /// XAML-Bindable property exposes loading state.
        /// </summary>
        public bool IsLoading { get { return isLoading; } private set { isLoading = value; OnPropertyChanged(new PropertyChangedEventArgs("IsLoading")); } }
        /// <summary>
        /// Determines if the IncrementalLoadingCollection&lt;T&gt; has more items.
        /// </summary>
        public bool HasMoreItems { get; private set; }
        /// <summary>
        /// XAML-Bindable property Gets the items of the WinRT.Collections.IncrementalLoadingCollection&lt;T&gt;.
        /// </summary>
        public IncrementalLoadingCollection<T> PagedItems { get { return this; } }

        /// <summary>
        /// Initializes a new instance of WinRT.Collections.IncrementalLoadingCollection&lt;T&gt; that is empty and with initial capacity.
        /// </summary>
        /// <param name="itemsLoaderAsync">
        /// <para>Func delegate gets paged items from data source.</para>
        /// <para>Parameters:</para>
        /// <para>int: Page number.</para>
        ///<para>int: Page size (Number of items per page).</para>
        ///<para>Returns:</para>
        ///<para>Task&lt;IEnumerable&lt;T&gt;&gt; (Current page items).</para>
        /// </param>
        /// <param name="PageSize">
        /// int: Sets page size (Number of items per page).
        /// </param>
        public IncrementalLoadingCollection(Func<int, int, Task<IEnumerable<T>>> itemsLoader, int pageSize)
            : base()
        {
            this.HasMoreItems = true;
            this.ItemsLoader = itemsLoader;
            this.PageSize = pageSize;
        }

        /// <summary>
        /// Initializes a new instance of WinRT.Collections.IncrementalLoadingCollection&lt;T&gt; that contains a copty of collection parameter.
        /// </summary>
        /// <param name="collection">The collection whose items will be copied to the WinRT.Collections.IncrementalLoadingCollection&lt;T&gt;.</param>
        /// <param name="itemsLoaderAsync">
        /// <para>Func delegate gets paged items from data source.</para>
        /// <para>Parameters:</para>
        /// <para>int: Page number.</para>
        ///<para>int: Page size (Number of items per page).</para>
        ///<para>Returns:</para>
        ///<para>Task&lt;IEnumerable&lt;T&gt;&gt; (Current page items).</para>
        /// </param>
        /// <param name="PageSize">
        /// int: Sets page size (Number of items per page).
        /// </param>
        public IncrementalLoadingCollection(IEnumerable<T> collection, Func<int, int, Task<IEnumerable<T>>> itemsLoader, int pageSize)
            : base(collection)
        {
            this.HasMoreItems = true;
            this.ItemsLoader = itemsLoader;
            this.PageSize = pageSize;
        }

        /// <summary>
        /// Loads a new page items on data items control event trigger.
        /// </summary>
        /// <param name="count">The count of the WinRT.Collections.IncrementalLoadingCollection&lt;T&gt;.</param>
        /// <returns>The new collection after adding the new page.</returns>
        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            IsLoading = true;

            try
            {
                return System.Threading.Tasks.Task.Run<LoadMoreItemsResult>(async () =>
                {
                    var items = await ItemsLoader(CurrentPage++, PageSize);
                    if (items.Count() == 0)
                    {
                        HasMoreItems = false;
                        return new LoadMoreItemsResult() { Count = count };
                    }
                    else
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () =>
                            {
                                foreach (var item in items)
                                {
                                    this.Add(item);
                                }
                            });
                        return new LoadMoreItemsResult() { Count = count + ((uint)Items.Count - 1) };
                    }
                }).AsAsyncOperation<LoadMoreItemsResult>();
            }

            catch
            {
                throw;
            }

            finally
            {
                IsLoading = false;
            }
        }
    }
}
