import { useEffect, useMemo, useRef } from 'react'
import type {
  FilterChangedEvent,
  FilterModel,
  GridApi,
  GridReadyEvent,
  SortChangedEvent,
} from 'ag-grid-community'
import { AgGridReact } from 'ag-grid-react'
import { columnDefs } from './columnDefs'
import { createInfiniteDatasource } from './infiniteDatasource'
import type { SortModelItem } from '../../types/risk'

interface RiskGridProps {
  search: string
  externalFilterModel: Record<string, unknown>
  selectedTradeId: string | null
  onFilterModelChange: (filterModel: FilterModel) => void
  onSortModelChange: (sortModel: SortModelItem[]) => void
  onRowOpen: (tradeId: string) => void
}

export function RiskGrid({
  search,
  externalFilterModel,
  selectedTradeId,
  onFilterModelChange,
  onSortModelChange,
  onRowOpen,
}: Readonly<RiskGridProps>) {
  const gridApiRef = useRef<GridApi | null>(null)

  const datasource = useMemo(
    () => createInfiniteDatasource({ search, externalFilterModel }),
    [search, externalFilterModel],
  )

  useEffect(() => {
    if (gridApiRef.current) {
      gridApiRef.current.setGridOption('datasource', datasource)
      gridApiRef.current.refreshInfiniteCache()
    }
  }, [datasource])

  useEffect(() => {
    if (gridApiRef.current) {
      gridApiRef.current.redrawRows()
    }
  }, [selectedTradeId])

  const onGridReady = (event: GridReadyEvent) => {
    gridApiRef.current = event.api
    event.api.setGridOption('datasource', datasource)
  }

  const onFilterChanged = (event: FilterChangedEvent) => {
    onFilterModelChange(event.api.getFilterModel())
  }

  const onSortChanged = (event: SortChangedEvent) => {
    const sortModel = event.api.getColumnState()
      .filter((state) => state.sort)
      .map((state) => ({
        colId: state.colId,
        sort: state.sort,
      })) as SortModelItem[]

    onSortModelChange(sortModel)
  }

  return (
    <section className="grid-panel">
      <div className="grid-host">
        <AgGridReact
          rowModelType="infinite"
          columnDefs={columnDefs}
          defaultColDef={{
            resizable: true,
            sortable: true,
            flex: 1,
            minWidth: 130,
            filterParams: { buttons: ['reset', 'apply'] },
          }}
          cacheBlockSize={100}
          maxBlocksInCache={12}
          rowBuffer={8}
          animateRows={false}
          suppressAggFuncInHeader
          getRowClass={(params) => (params.data?.tradeId === selectedTradeId ? 'positions-row-selected' : undefined)}
          onGridReady={onGridReady}
          onFilterChanged={onFilterChanged}
          onSortChanged={onSortChanged}
          onRowClicked={(event) => {
            if (event.data?.tradeId) {
              onRowOpen(event.data.tradeId)
            }
          }}
          overlayNoRowsTemplate="No rows found"
        />
      </div>
    </section>
  )
}
