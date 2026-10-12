import { useMemo, useState } from 'react'
import { RiskGrid } from './features/grid/RiskGrid'
import { SearchBox } from './features/search/SearchBox'
import { SummaryPanel } from './features/summary/SummaryPanel'
import { GroupBySelector } from './features/summary/GroupBySelector'
import { FilterChips } from './features/drilldown/FilterChips'
import { DetailDrawer } from './features/drilldown/DetailDrawer'
import { type AppFilterState } from './state/filterStore'
import './styles/app.css'

function App() {
  const [state, setState] = useState<AppFilterState>({
    search: '',
    groupBy: 'book',
    selectedGroup: null,
    gridFilterModel: {},
    gridSortModel: [],
  })
  const [selectedTradeId, setSelectedTradeId] = useState<string | null>(null)

  const externalFilterModel = useMemo(
    () => {
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
    },
    [state.selectedGroup],
  )

  const mergedFilterModel = useMemo(
    () => ({ ...state.gridFilterModel, ...externalFilterModel }),
    [state.gridFilterModel, externalFilterModel],
  )

  const appShellClassName = selectedTradeId ? 'app-shell drawer-open' : 'app-shell'

  return (
    <div className={appShellClassName}>
      <header className="header-row">
        <div>
          <h1>Risk Reporting</h1>
        </div>
        <SearchBox
          value={state.search}
          onChange={(value) => {
            setSelectedTradeId(null)
            setState((previous) => ({ ...previous, search: value }))
          }}
        />
      </header>

      <section className="controls-row">
        <GroupBySelector
          value={state.groupBy}
          onChange={(groupBy) => {
            setSelectedTradeId(null)
            setState((previous) => ({
              ...previous,
              groupBy,
              selectedGroup: null,
            }))
          }}
        />
        <FilterChips
          state={state}
          onClearGroup={() => {
            setSelectedTradeId(null)
            setState((previous) => ({
              ...previous,
              selectedGroup: null,
            }))
          }}
        />
      </section>

      <div className="content-main">
        <SummaryPanel
          search={state.search}
          groupBy={state.groupBy}
          filterModel={mergedFilterModel}
          sortModel={state.gridSortModel}
          selectedGroupValue={state.selectedGroup?.value ?? null}
          onSelectGroup={(groupValue) =>
            {
              setSelectedTradeId(null)
              setState((previous) => ({
                ...previous,
                selectedGroup: { column: previous.groupBy, value: groupValue },
              }))
            }
          }
        />

        <section className="grid-section">
          <div className="grid-section-header">
            <h2>Positions</h2>
          </div>
          <RiskGrid
            search={state.search}
            externalFilterModel={externalFilterModel}
            selectedTradeId={selectedTradeId}
            onFilterModelChange={(filterModel) =>
              setState((previous) => ({
                ...previous,
                gridFilterModel: filterModel,
              }))
            }
            onSortModelChange={(sortModel) =>
              setState((previous) => ({
                ...previous,
                gridSortModel: sortModel,
              }))
            }
            onRowOpen={(tradeId) => setSelectedTradeId(tradeId)}
          />
        </section>
      </div>

      <DetailDrawer
        tradeId={selectedTradeId}
        onClose={() => setSelectedTradeId(null)}
      />
    </div>
  )
}

export default App
