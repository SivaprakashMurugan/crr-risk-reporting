import type { IDatasource, IGetRowsParams } from 'ag-grid-community'
import { fetchPositions } from '../../api/positionsApi'
import type { FilterModel } from '../../types/risk'

interface DatasourceOptions {
  search: string
  externalFilterModel: FilterModel
}

export function createInfiniteDatasource({ search, externalFilterModel }: DatasourceOptions): IDatasource {
  let requestVersion = 0
  let controller: AbortController | null = null

  return {
    getRows: async (params: IGetRowsParams) => {
      requestVersion += 1
      const currentVersion = requestVersion

      controller?.abort()
      controller = new AbortController()

      try {
        const response = await fetchPositions(
          {
            startRow: params.startRow,
            endRow: params.endRow,
            sortModel: params.sortModel.map((item) => ({
              colId: item.colId,
              sort: item.sort,
            })),
            filterModel: {
              ...(params.filterModel as FilterModel),
              ...externalFilterModel,
            },
            search,
          },
          controller.signal,
        )

        if (currentVersion !== requestVersion) {
          return
        }

        params.successCallback(response.rows, response.lastRow)
      } catch (exception) {
        if (controller.signal.aborted) {
          return
        }

        params.failCallback()
        console.error('Failed to fetch rows', exception)
      }
    },
  }
}
