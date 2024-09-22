﻿using ReactiveUI;
using System.Collections.ObjectModel;
using Zametek.Common.ProjectPlan;
using Zametek.Contract.ProjectPlan;

namespace Zametek.ViewModel.ProjectPlan
{
    public class ResourceSelectorViewModel
        : ViewModelBase, IResourceSelectorViewModel
    {
        #region Fields

        private readonly object m_Lock;
        private static readonly EqualityComparer<ISelectableResourceViewModel> s_EqualityComparer =
            EqualityComparer<ISelectableResourceViewModel>.Create(
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

        private static readonly Comparer<ISelectableResourceViewModel> s_SortComparer =
            Comparer<ISelectableResourceViewModel>.Create(
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

        #endregion

        #region Ctors

        public ResourceSelectorViewModel()
        {
            m_Lock = new object();
            m_TargetResources = new(s_EqualityComparer);
            m_ReadOnlyTargetResources = new(m_TargetResources);
            m_SelectedTargetResources = new(s_EqualityComparer);
        }

        #endregion

        #region Properties

        private readonly ObservableUniqueCollection<ISelectableResourceViewModel> m_TargetResources;
        private readonly ReadOnlyObservableCollection<ISelectableResourceViewModel> m_ReadOnlyTargetResources;
        public ReadOnlyObservableCollection<ISelectableResourceViewModel> TargetResources => m_ReadOnlyTargetResources;

        // Use ObservableUniqueCollection to prevent selected
        // items appearing twice in the Urse MultiComboBox.
        private readonly ObservableUniqueCollection<ISelectableResourceViewModel> m_SelectedTargetResources;
        public ObservableCollection<ISelectableResourceViewModel> SelectedTargetResources => m_SelectedTargetResources;

        public string TargetResourcesString
        {
            get
            {
                lock (m_Lock)
                {
                    return string.Join(
                        DependenciesStringValidationRule.Separator,
                        TargetResources.Where(x => x.IsSelected).Select(x => x.DisplayName));
                }
            }
        }

        public IList<int> SelectedResourceIds
        {
            get
            {
                lock (m_Lock)
                {
                    return TargetResources
                        .Where(x => x.IsSelected)
                        .Select(x => x.Id)
                        .ToList();
                }
            }
        }

        #endregion

        #region Public Methods

        public string GetAllocatedToResourcesString(HashSet<int> allocatedToResources)
        {
            ArgumentNullException.ThrowIfNull(allocatedToResources);
            lock (m_Lock)
            {
                return string.Join(
                    DependenciesStringValidationRule.Separator,
                    TargetResources.Where(x => allocatedToResources.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Select(x => x.DisplayName));
            }
        }

        public void SetTargetResources(
            IEnumerable<ResourceModel> targetResources,
            HashSet<int> selectedTargetResources)
        {
            ArgumentNullException.ThrowIfNull(targetResources);
            ArgumentNullException.ThrowIfNull(selectedTargetResources);
            lock (m_Lock)
            {
                {
                    // Find target view models that have been removed.
                    List<ISelectableResourceViewModel> removedViewModels = m_TargetResources
                        .ExceptBy(targetResources.Select(x => x.Id), x => x.Id)
                        .ToList();

                    // Delete the removed items from the target and selected collections.
                    foreach (ISelectableResourceViewModel vm in removedViewModels)
                    {
                        m_TargetResources.Remove(vm);
                        m_SelectedTargetResources.Remove(vm);
                        vm.Dispose();
                    }

                    // Find the selected view models that have been removed.
                    List<ISelectableResourceViewModel> removedSelectedViewModels = m_SelectedTargetResources
                        .ExceptBy(selectedTargetResources, x => x.Id)
                        .ToList();

                    // Delete the removed selected items from the selected collections.
                    foreach (ISelectableResourceViewModel vm in removedSelectedViewModels)
                    {
                        vm.IsSelected = false;
                        m_SelectedTargetResources.Remove(vm);
                    }
                }
                {
                    // Find the target models that have been added.
                    List<ResourceModel> addedModels = targetResources
                        .ExceptBy(m_TargetResources.Select(x => x.Id), x => x.Id)
                        .ToList();

                    List<ISelectableResourceViewModel> addedViewModels = [];

                    // Create a collection of new view models.
                    foreach (ResourceModel model in addedModels)
                    {
                        var vm = new SelectableResourceViewModel(
                            model.Id,
                            model.Name,
                            selectedTargetResources.Contains(model.Id),
                            this);

                        m_TargetResources.Add(vm);
                        if (vm.IsSelected)
                        {
                            m_SelectedTargetResources.Add(vm);
                        }
                    }
                }
                {
                    // Update names.
                    Dictionary<int, ResourceModel> targetResourceLookup = targetResources.ToDictionary(x => x.Id);

                    foreach (ISelectableResourceViewModel vm in m_TargetResources)
                    {
                        if (targetResourceLookup.TryGetValue(vm.Id, out ResourceModel? value))
                        {
                            vm.Name = value.Name;
                        }
                    }
                }

                m_TargetResources.Sort(s_SortComparer);
            }
            RaiseTargetResourcesPropertiesChanged();
        }

        public void ClearTargetResources()
        {
            lock (m_Lock)
            {
                foreach (IDisposable targetResource in TargetResources)
                {
                    targetResource.Dispose();
                }
                m_TargetResources.Clear();
            }
        }

        public void ClearSelectedTargetResources()
        {
            lock (m_Lock)
            {
                foreach (IDisposable targetResource in SelectedTargetResources)
                {
                    targetResource.Dispose();
                }
                m_SelectedTargetResources.Clear();
            }
        }

        public void RaiseTargetResourcesPropertiesChanged()
        {
            this.RaisePropertyChanged(nameof(TargetResources));
            this.RaisePropertyChanged(nameof(TargetResourcesString));
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return TargetResourcesString;
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
                ClearTargetResources();
                ClearSelectedTargetResources();
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
}
