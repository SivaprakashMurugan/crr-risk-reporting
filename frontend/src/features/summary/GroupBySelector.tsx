import type { GroupBy } from '../../types/risk'

interface GroupBySelectorProps {
  value: GroupBy
  onChange: (groupBy: GroupBy) => void
}

const options: Array<{ label: string; value: GroupBy }> = [
  { label: 'Book', value: 'book' },
  { label: 'Commodity', value: 'commodity' },
  { label: 'Region', value: 'region' },
  { label: 'Instrument Type', value: 'instrumentType' },
]

export function GroupBySelector({ value, onChange }: Readonly<GroupBySelectorProps>) {
  return (
    <label className="groupby-selector">
      <span>Group summary by</span>
      <select value={value} onChange={(event) => onChange(event.target.value as GroupBy)}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  )
}
