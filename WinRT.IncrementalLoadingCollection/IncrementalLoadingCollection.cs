using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace WinRT.IncrementalLoadingCollection
{
    /// <summary>
    /// <para>Represents a strongly typed collection of objects that can be accessed by index.</para>
    /// <para>Supports out of the box:</para>Supports observability and incremental loading.
    /// <para>Observability: all changes inside in the collection are observed by binding clients.</para>
    /// <para>Incremental Loading: supports collection items paging by loading more data on scrolling data items control event trigger.</para>
    /// </summary>
    /// <typeparam name="T">The type of elements in the IncrementalLoadingCollection&lt;T&gt;.</typeparam>
    public class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        /// <summary>
        /// <para>Func delegate gets paged items from data source.</para>
        /// <para>Parameters:</para>
        /// <para>int: Page number.</para>
        ///<para>int: Page size (Number of items per page).</para>
        ///<para>Returns: Task&lt;IEnumerable&lt;T&gt;&gt; (Current page items).</para>
        /// </summary>        
        private readonly Func<int, int, Task<IEnumerable<T>>> itemsLoader;

        /// <summary>
        /// Determines current page index.
        /// </summary>
        private int currentPage { get; set; }

        /// <summary>
        /// Determines page size (Number of items per page).
        /// </summary>
        private readonly int pageSize;

        private bool _loading;
        /// <summary>
        /// XAML-Bindable property exposes loading state.
        /// </summary>
        public bool Loading { get { return _loading; } private set { _loading = value; OnPropertyChanged(new PropertyChangedEventArgs("Loading")); } }
        /// <summary>
        /// Determines if the IncrementalLoadingCollection&lt;T&gt; has more items.
        /// </summary>
        public bool HasMoreItems { get; private set; }

        /// <summary>
        /// Initializes a new instance of Windows.Libraries.Collections.IncrementalLoadingCollection&lt;T&gt; that is empty and with initial capacity.
        /// </summary>
        /// <param name="itemsLoader">
        /// <para>Func delegate gets paged items from data source.</para>
        /// <para>Parameters:</para>
        /// <para>int: Page number.</para>
        ///<para>int: Page size (Number of items per page).</para>
        ///<para>Returns:</para>
        ///<para>Task&lt;IEnumerable&lt;T&gt;&gt; (Current page items).</para>
        /// </param>
        /// <param name="pageSize">
        /// int: Sets page size (Number of items per page).
        /// </param>
        public IncrementalLoadingCollection(Func<int, int, Task<IEnumerable<T>>> itemsLoader, int pageSize)
            : base()
        {
            this.HasMoreItems = true;
            this.itemsLoader = itemsLoader;
            this.pageSize = pageSize;
        }

        /// <summary>
        /// Initializes a new instance of IncrementalLoadingCollection&lt;T&gt; that contains a copty of collection parameter.
        /// </summary>
        /// <param name="collection">The collection whose items will be copied to the IncrementalLoadingCollection&lt;T&gt;.</param>
        /// <param name="itemsLoader">
        /// <para>Func delegate gets paged items from data source.</para>
        /// <para>Parameters:</para>
        /// <para>int: Page number.</para>
        ///<para>int: Page size (Number of items per page).</para>
        ///<para>Returns:</para>
        ///<para>Task&lt;IEnumerable&lt;T&gt;&gt; (Current page items).</para>
        /// </param>
        /// <param name="pageSize">
        /// int: Sets page size (Number of items per page).
        /// </param>
        public IncrementalLoadingCollection(IEnumerable<T> collection, Func<int, int, Task<IEnumerable<T>>> itemsLoader, int pageSize)
            : base(collection)
        {
            this.HasMoreItems = true;
            this.itemsLoader = itemsLoader;
            this.pageSize = pageSize;
        }

        /// <summary>
        /// Loads a new page items on data items control event trigger.
        /// </summary>
        /// <param name="count">The count of the IncrementalLoadingCollection&lt;T&gt;.</param>
        /// <returns>The new collection after adding the new page.</returns>
        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            Loading = true;

            try
            {
                return System.Threading.Tasks.Task.Run<LoadMoreItemsResult>(async () =>
                {
                    var items = await itemsLoader(currentPage, pageSize);
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
                Loading = false;
            }
        }
    }
}
