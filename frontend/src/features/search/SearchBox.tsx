interface SearchBoxProps {
  value: string
  onChange: (value: string) => void
}

export function SearchBox({ value, onChange }: Readonly<SearchBoxProps>) {
  return (
    <label className="search-box">
      <span>Search</span>
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder="counterparty or commodity"
      />
    </label>
  )
}
