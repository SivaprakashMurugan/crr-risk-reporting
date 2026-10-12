import { useEffect, useState } from 'react'
import { fetchAggregations } from '../../api/aggregationsApi'
import { useDebounce } from '../../hooks/useDebounce'
import type {
	AggregationResponse,
	FilterModel,
	GroupBy,
	SortModelItem,
} from '../../types/risk'

interface UseAggregationsArgs {
	groupBy: GroupBy
	search: string
	filterModel: FilterModel
	sortModel: SortModelItem[]
}

export function useAggregations({ groupBy, search, filterModel, sortModel }: UseAggregationsArgs) {
	const [data, setData] = useState<AggregationResponse | null>(null)
	const [error, setError] = useState<string | null>(null)
	const [isLoading, setIsLoading] = useState(false)

	const debouncedSearch = useDebounce(search, 300)

	useEffect(() => {
		const controller = new AbortController()
			// eslint-disable-next-line react-hooks/set-state-in-effect
		setIsLoading(true)
		setError(null)

		fetchAggregations(
			{
				groupBy,
				search: debouncedSearch,
				filterModel,
				sortModel,
			},
			controller.signal,
		)
			.then(setData)
			.catch((exception: unknown) => {
				if (controller.signal.aborted || (exception instanceof DOMException && exception.name === 'AbortError')) {
					return
				}

				setError(exception instanceof Error ? exception.message : 'Failed to load summary data')
			})
			.finally(() => {
				if (!controller.signal.aborted) {
					setIsLoading(false)
				}
			})

		return () => controller.abort()
	}, [groupBy, debouncedSearch, filterModel, sortModel])

	return { data, error, isLoading }
}
