import { formatCurrency, formatNumber } from '../grid/formatters'
import { useAggregations } from './useAggregations'
import type { FilterModel, GroupBy, SortModelItem } from '../../types/risk'

interface SummaryPanelProps {
  groupBy: GroupBy
  search: string
  filterModel: FilterModel
  sortModel: SortModelItem[]
  selectedGroupValue: string | null
  onSelectGroup: (groupValue: string) => void
}

export function SummaryPanel({
  groupBy,
  search,
  filterModel,
  sortModel,
  selectedGroupValue,
  onSelectGroup,
}: Readonly<SummaryPanelProps>) {
  const { data, error, isLoading } = useAggregations({
    groupBy,
    search,
    filterModel,
    sortModel,
  })

  return (
    <section className="summary-panel">
      <div className="summary-header">
        <h2>Filtered summary</h2>
        {isLoading && <span>Loading...</span>}
        {error && <span className="error">{error}</span>}
      </div>

      {data && (
        <>
          <table>
            <thead>
              <tr>
                <th>{groupBy}</th>
                <th>Rows</th>
                <th>P&amp;L (USD)</th>
                <th>FX Exposure (USD)</th>
                <th>Position</th>
              </tr>
            </thead>
            <tbody>
              {data.groups.map((group) => {
                const isSelected = selectedGroupValue === group.groupKey
                const rowClassName = isSelected ? 'clickable summary-row-selected' : 'clickable'

                return (
                  <tr
                    key={group.groupKey}
                    className={rowClassName}
                    onClick={() => onSelectGroup(group.groupKey)}
                  >
                    <td>{group.groupKey}</td>
                    <td>{formatNumber(group.rowCount)}</td>
                    <td>{formatCurrency(group.pnlTotal)}</td>
                    <td>{formatCurrency(group.fxExposureTotal)}</td>
                    <td>
                      {group.positionTotal == null ? '—' : formatNumber(group.positionTotal)}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>

          <div className="summary-totals">
            <strong>Grand totals:</strong>
            <span>Rows: {formatNumber(data.grandTotals.rowCount)}</span>
            <span>P&amp;L: {formatCurrency(data.grandTotals.pnlTotal)}</span>
            <span>FX Exposure: {formatCurrency(data.grandTotals.fxExposureTotal)}</span>
          </div>
          <p className="position-note">{data.positionNote ?? 'Position shown as units per commodity only.'}</p>
        </>
      )}
    </section>
  )
}
