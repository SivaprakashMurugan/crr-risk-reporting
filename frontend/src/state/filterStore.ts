import type { FilterModel, GroupBy, SortModelItem } from '../types/risk'

export interface GroupChip {
  column: GroupBy
  value: string
}

export interface AppFilterState {
  search: string
  groupBy: GroupBy
  selectedGroup: GroupChip | null
  gridFilterModel: FilterModel
  gridSortModel: SortModelItem[]
}

export function createExternalFilterModel(state: AppFilterState): FilterModel {
  if (!state.selectedGroup) {
    return {}
  }

  return {
    [state.selectedGroup.column]: {
      filterType: 'text',
      type: 'equals',
      filter: state.selectedGroup.value,
    },
  }
}
