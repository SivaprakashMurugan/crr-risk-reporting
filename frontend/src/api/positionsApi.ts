import { apiGet, apiPost } from './client'
import type { RiskPosition, RowsRequest, RowsResponse } from '../types/risk'

export function fetchPositions(request: RowsRequest, signal?: AbortSignal) {
  return apiPost<RowsResponse>('/api/risk/positions', request, signal)
}

export function fetchPositionByTradeId(tradeId: string, signal?: AbortSignal) {
  return apiGet<RiskPosition>(`/api/risk/positions/${encodeURIComponent(tradeId)}`, signal)
}
