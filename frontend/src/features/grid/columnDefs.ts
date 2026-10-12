import type { ColDef } from 'ag-grid-community'
import type { RiskPosition } from '../../types/risk'
import { formatCurrency, formatDate, formatNumber } from './formatters'

const numericCellClass = (params: { value: number }) => {
  if (params.value == null || Number.isNaN(params.value)) {
    return 'cell-number'
  }

  return params.value >= 0 ? 'cell-number positive' : 'cell-number negative'
}

export const columnDefs: ColDef<RiskPosition>[] = [
  { field: 'tradeId', filter: 'agTextColumnFilter', sortable: true },
  { field: 'book', filter: 'agTextColumnFilter', sortable: true },
  { field: 'commodity', filter: 'agTextColumnFilter', sortable: true },
  { field: 'counterparty', filter: 'agTextColumnFilter', sortable: true },
  { field: 'region', filter: 'agTextColumnFilter', sortable: true },
  { field: 'instrumentType', headerName: 'Instrument', filter: 'agTextColumnFilter', sortable: true },
  {
    field: 'position',
    filter: 'agNumberColumnFilter',
    sortable: true,
    valueFormatter: ({ value }) => formatNumber(value),
    cellClass: 'cell-number',
  },
  { field: 'currency', filter: 'agTextColumnFilter', sortable: true },
  {
    field: 'marketPrice',
    filter: 'agNumberColumnFilter',
    sortable: true,
    valueFormatter: ({ value }) => formatNumber(value),
    cellClass: 'cell-number',
  },
  {
    field: 'pnl',
    headerName: 'P&L (USD)',
    filter: 'agNumberColumnFilter',
    sortable: true,
    valueFormatter: ({ value }) => formatCurrency(value),
    cellClass: numericCellClass,
  },
  {
    field: 'fxExposure',
    headerName: 'FX Exposure (USD)',
    filter: 'agNumberColumnFilter',
    sortable: true,
    valueFormatter: ({ value }) => formatCurrency(value),
    cellClass: numericCellClass,
  },
  {
    field: 'asOfDate',
    headerName: 'As Of Date',
    filter: 'agDateColumnFilter',
    sortable: true,
    valueFormatter: ({ value }) => formatDate(value),
  },
]
