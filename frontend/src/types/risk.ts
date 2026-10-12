export type SortDirection = 'asc' | 'desc'

export interface SortModelItem {
  colId: string
  sort: SortDirection
}

export type FilterModel = Record<string, unknown>

export interface RowsRequest {
  startRow: number
  endRow: number
  sortModel: SortModelItem[]
  filterModel: FilterModel
  search?: string
}

export interface RiskPosition {
  tradeId: string
  book: string
  commodity: string
  counterparty: string
  region: string
  instrumentType: string
  position: number
  currency: string
  marketPrice: number
  pnl: number
  fxExposure: number
  asOfDate: string
}

export interface RowsResponse {
  rows: RiskPosition[]
  lastRow: number
}

export type GroupBy = 'book' | 'commodity' | 'region' | 'instrumentType'

export interface AggregationRequest {
  groupBy: GroupBy
  sortModel: SortModelItem[]
  filterModel: FilterModel
  search?: string
}

export interface AggregationGroup {
  groupKey: string
  rowCount: number
  pnlTotal: number
  fxExposureTotal: number
  positionTotal: number | null
  positionNote: string | null
}

export interface AggregationTotals {
  rowCount: number
  pnlTotal: number
  fxExposureTotal: number
}

export interface AggregationResponse {
  groups: AggregationGroup[]
  grandTotals: AggregationTotals
  positionNote: string | null
}
