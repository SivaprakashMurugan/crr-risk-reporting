import { apiPost } from './client'
import type { AggregationRequest, AggregationResponse } from '../types/risk'

export function fetchAggregations(request: AggregationRequest, signal?: AbortSignal) {
  return apiPost<AggregationResponse>('/api/risk/aggregations', request, signal)
}
