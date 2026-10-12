import type { AppFilterState } from '../../state/filterStore'

interface FilterChipsProps {
  state: AppFilterState
  onClearGroup: () => void
}

export function FilterChips({ state, onClearGroup }: Readonly<FilterChipsProps>) {
  if (!state.selectedGroup) {
    return null
  }

  return (
    <div className="filter-chips">
      <span className="chip">
        {state.selectedGroup.column}: {state.selectedGroup.value}
        <button type="button" onClick={onClearGroup}>
          ×
        </button>
      </span>
    </div>
  )
}
