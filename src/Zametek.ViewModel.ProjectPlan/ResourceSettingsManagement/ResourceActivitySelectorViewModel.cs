﻿using ReactiveUI;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Zametek.Common.ProjectPlan;
using Zametek.Contract.ProjectPlan;
using System.Linq;

namespace Zametek.ViewModel.ProjectPlan
{
    public class ResourceActivitySelectorViewModel
        : ViewModelBase, IResourceActivitySelectorViewModel
    {
        #region Fields

        private readonly object m_Lock;
        private readonly ICoreViewModel m_CoreViewModel;
        private readonly int m_ResourceId;
        private readonly int m_Time;
        private static readonly EqualityComparer<ISelectableResourceActivityViewModel> s_EqualityComparer =
            EqualityComparer<ISelectableResourceActivityViewModel>.Create(
                    (x, y) =>
                    {
                        if (x is null)
                        {
                            return false;
                        }
                        if (y is null)
                        {
                            return false;
                        }
                        return x.Id == y.Id;
                    },
                    x => x.Id);

        private static readonly Comparer<ISelectableResourceActivityViewModel> s_SortComparer =
            Comparer<ISelectableResourceActivityViewModel>.Create(
                    (x, y) =>
                    {
                        if (x is null)
                        {
                            if (y is null)
                            {
                                return 0;
                            }
                            return -1;
                        }
                        if (y is null)
                        {
                            return 1;
                        }

                        return x.Id.CompareTo(y.Id);
                    });

        private readonly IDisposable? m_ReviseResourceActivityTrackersSub;

        #endregion

        #region Ctors

        public ResourceActivitySelectorViewModel(
            ICoreViewModel coreViewModel,
            ResourceTrackerModel resourceTrackerModel)
        {
            ArgumentNullException.ThrowIfNull(coreViewModel);
            ArgumentNullException.ThrowIfNull(resourceTrackerModel);
            m_Lock = new object();
            m_CoreViewModel = coreViewModel;
            m_TargetResourceActivities = new(s_EqualityComparer);
            m_ReadOnlyTargetResourceActivities = new(m_TargetResourceActivities);
            m_SelectedTargetResourceActivities = new(s_EqualityComparer);

            m_Time = resourceTrackerModel.Time;
            m_ResourceId = resourceTrackerModel.ResourceId;

            // Initial set up.
            ReviseTrackers(resourceTrackerModel.ActivityTrackers);

            m_ReviseResourceActivityTrackersSub = this
                .WhenAnyValue(x => x.m_CoreViewModel.IsReadyToReviseTrackers)
                .ObserveOn(Scheduler.CurrentThread)
                .Subscribe(isReadyToRevise =>
                {
                    if (isReadyToRevise == ReadyToRevise.Yes)
                    {
                        ReviseTrackers();
                    }
                });
        }

        #endregion

        #region Private Members

        private void ReviseTrackers(IList<ResourceActivityTrackerModel> resourceActivityTrackers)
        {
            lock (m_Lock)
            {
                HashSet<int> selectedResourceActivityIds = resourceActivityTrackers.Select(x => x.ActivityId).ToHashSet();

                Dictionary<int, ResourceActivityTrackerModel> resourceActivityTrackerLookup = resourceActivityTrackers.ToDictionary(x => x.ActivityId);

                List<ResourceActivityTrackerModel> newResourceActivityTrackers =
                    m_CoreViewModel.Activities
                    .Select(x => new ResourceActivityTrackerModel
                    {
                        Time = m_Time,
                        ResourceId = m_ResourceId,
                        ActivityId = x.Id,
                        ActivityName = x.Name,
                        PercentageWorked = 0
                    }).ToList();

                foreach (ResourceActivityTrackerModel resourceActivityTracker in newResourceActivityTrackers)
                {
                    resourceActivityTrackerLookup.TryAdd(resourceActivityTracker.ActivityId, resourceActivityTracker);
                }

                SetTargetResourceActivities(
                    resourceActivityTrackerLookup.Values,
                    selectedResourceActivityIds);
            }
        }

        private void ReviseTrackers()
        {
            lock (m_Lock)
            {
                List<ResourceActivityTrackerModel> newResourceActivityTrackers =
                    m_CoreViewModel.Activities
                    .Select(x => new ResourceActivityTrackerModel
                    {
                        Time = m_Time,
                        ResourceId = m_ResourceId,
                        ActivityId = x.Id,
                        ActivityName = x.Name,
                        PercentageWorked = 0
                    }).ToList();

                SetTargetResourceActivities(
                    newResourceActivityTrackers,
                    SelectedResourceActivityIds.ToHashSet());
            }
        }

        #endregion

        #region Properties

        public static IResourceActivitySelectorViewModel Empty { get; } = new EmptyResourceActivitySelectorViewModel();

        private readonly ObservableUniqueCollection<ISelectableResourceActivityViewModel> m_TargetResourceActivities;
        private readonly ReadOnlyObservableCollection<ISelectableResourceActivityViewModel> m_ReadOnlyTargetResourceActivities;
        public ReadOnlyObservableCollection<ISelectableResourceActivityViewModel> TargetResourceActivities => m_ReadOnlyTargetResourceActivities;

        // Use ObservableUniqueCollection to prevent selected
        // items appearing twice in the Urse MultiComboBox.
        private readonly ObservableUniqueCollection<ISelectableResourceActivityViewModel> m_SelectedTargetResourceActivities;
        public ObservableCollection<ISelectableResourceActivityViewModel> SelectedTargetResourceActivities => m_SelectedTargetResourceActivities;

        public string TargetResourceActivitiesString
        {
            get
            {
                lock (m_Lock)
                {
                    return string.Join(
                        DependenciesStringValidationRule.Separator,
                        TargetResourceActivities.Where(x => x.IsSelected).Select(x => x.DisplayName));
                }
            }
        }

        public IList<int> SelectedResourceActivityIds
        {
            get
            {
                lock (m_Lock)
                {
                    return TargetResourceActivities
                        .Where(x => x.IsSelected)
                        .Select(x => x.Id)
                        .ToList();
                }
            }
        }

        #endregion

        #region Public IResourceActivitySelectorViewModel

        //public string GetAllocatedToActivitiesString(HashSet<int> allocatedToActivities)
        //{
        //    ArgumentNullException.ThrowIfNull(allocatedToActivities);
        //    lock (m_Lock)
        //    {
        //        return string.Join(
        //            DependenciesStringValidationRule.Separator,
        //            TargetActivities.Where(x => allocatedToActivities.Contains(x.Id))
        //                .OrderBy(x => x.Id)
        //                .Select(x => x.DisplayName));
        //    }
        //}

        public void SetTargetResourceActivities(
            IEnumerable<ResourceActivityTrackerModel> targetResourceActivities,
            HashSet<int> selectedTargetResourceActivities)
        {
            ArgumentNullException.ThrowIfNull(targetResourceActivities);
            ArgumentNullException.ThrowIfNull(selectedTargetResourceActivities);
            lock (m_Lock)
            {
                {
                    // Find target view models that have been removed.
                    List<ISelectableResourceActivityViewModel> removedViewModels = m_TargetResourceActivities
                        .ExceptBy(targetResourceActivities.Select(x => x.ActivityId), x => x.Id)
                        .ToList();

                    // Delete the removed items from the target and selected collections.
                    foreach (ISelectableResourceActivityViewModel vm in removedViewModels)
                    {
                        m_TargetResourceActivities.Remove(vm);
                        m_SelectedTargetResourceActivities.Remove(vm);
                        vm.Dispose();
                    }

                    // Find the selected view models that have been removed.
                    List<ISelectableResourceActivityViewModel> removedSelectedViewModels = m_SelectedTargetResourceActivities
                        .ExceptBy(selectedTargetResourceActivities, x => x.Id)
                        .ToList();

                    // Delete the removed selected items from the selected collections.
                    foreach (ISelectableResourceActivityViewModel vm in removedSelectedViewModels)
                    {
                        vm.IsSelected = false;
                        m_SelectedTargetResourceActivities.Remove(vm);
                    }
                }
                {
                    // Find the target models that have been added.
                    List<ResourceActivityTrackerModel> addedModels = targetResourceActivities
                        .ExceptBy(m_TargetResourceActivities.Select(x => x.Id), x => x.ActivityId)
                        .ToList();

                    List<ISelectableResourceActivityViewModel> addedViewModels = [];

                    // Create a collection of new view models.
                    foreach (ResourceActivityTrackerModel model in addedModels)
                    {
                        var vm = new SelectableResourceActivityViewModel(
                            model.ActivityId,
                            model.ActivityName,
                            selectedTargetResourceActivities.Contains(model.ActivityId),
                            model.PercentageWorked,
                            this);

                        m_TargetResourceActivities.Add(vm);
                        if (vm.IsSelected)
                        {
                            m_SelectedTargetResourceActivities.Add(vm);
                        }
                    }
                }
                {
                    // Update names.
                    Dictionary<int, ResourceActivityTrackerModel> targetResourceActivityLookup = targetResourceActivities.ToDictionary(x => x.ActivityId);

                    foreach (ISelectableResourceActivityViewModel vm in m_TargetResourceActivities)
                    {
                        if (targetResourceActivityLookup.TryGetValue(vm.Id, out ResourceActivityTrackerModel? value))
                        {
                            vm.Name = value.ActivityName;
                        }
                    }
                }

                m_TargetResourceActivities.Sort(s_SortComparer);
            }
            RaiseTargetResourceActivitiesPropertiesChanged();
        }

        public void ClearTargetResourceActivities()
        {
            lock (m_Lock)
            {
                foreach (ISelectableResourceActivityViewModel targetResourceActivity in TargetResourceActivities)
                {
                    targetResourceActivity.Dispose();
                }
                m_TargetResourceActivities.Clear();
            }
        }

        public void ClearSelectedTargetResourceActivities()
        {
            lock (m_Lock)
            {
                foreach (ISelectableResourceActivityViewModel targetResourceActivity in SelectedTargetResourceActivities)
                {
                    targetResourceActivity.Dispose();
                }
                m_SelectedTargetResourceActivities.Clear();
            }
        }

        public void RaiseTargetResourceActivitiesPropertiesChanged()
        {
            this.RaisePropertyChanged(nameof(TargetResourceActivities));
            this.RaisePropertyChanged(nameof(TargetResourceActivitiesString));
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return TargetResourceActivitiesString;
        }

        #endregion

        #region IDisposable Members

        private bool m_Disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
            {
                return;
            }

            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
                m_ReviseResourceActivityTrackersSub?.Dispose();
                ClearTargetResourceActivities();
                ClearSelectedTargetResourceActivities();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            m_Disposed = true;
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public class EmptyResourceActivitySelectorViewModel
        : IResourceActivitySelectorViewModel
    {
        internal EmptyResourceActivitySelectorViewModel()
        {
            TargetResourceActivities = new ReadOnlyObservableCollection<ISelectableResourceActivityViewModel>([]);
            SelectedTargetResourceActivities = [];
            TargetResourceActivitiesString = string.Empty;
            SelectedResourceActivityIds = [];
        }

        public ReadOnlyObservableCollection<ISelectableResourceActivityViewModel> TargetResourceActivities { get; init; }

        public ObservableCollection<ISelectableResourceActivityViewModel> SelectedTargetResourceActivities { get; init; }

        public string TargetResourceActivitiesString { get; init; }

        public IList<int> SelectedResourceActivityIds { get; init; }

        public void ClearSelectedTargetResourceActivities()
        {
        }

        public void ClearTargetResourceActivities()
        {
        }

        public void Dispose()
        {
        }

        public void RaiseTargetResourceActivitiesPropertiesChanged()
        {
        }

        public void SetTargetResourceActivities(
            IEnumerable<ResourceActivityTrackerModel> targetResourceActivities,
            HashSet<int> selectedTargetResourceActivities)
        {
        }
    }
}
