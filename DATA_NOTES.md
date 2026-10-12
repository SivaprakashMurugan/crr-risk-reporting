# DATA NOTES

Source: `data/risk_positions.csv`

## Row and schema checks

- Data rows: **50,000**
- Columns: **12**
- Bad row length records: **0**
- Duplicate `tradeId` values: **0**
- Null/blank values: **0** across all fields

## Distinct values

- `book` (4): `Ags`, `Ferrous`, `Freight`, `Non-Ferrous`
- `commodity` (15): `Aluminium`, `Capesize`, `Coking Coal`, `Copper`, `Corn`, `HRC Steel`, `Iron Ore`, `Nickel`, `Palm Oil`, `Panamax`, `Rebar`, `Soybean`, `Supramax`, `Wheat`, `Zinc`
- `region` (3): `AMER`, `APAC`, `EMEA`
- `instrumentType` (3): `Future`, `Physical`, `Swap`
- `currency` (4): `CNY`, `EUR`, `SGD`, `USD`
- Distinct `asOfDate` values: **30**

## Numeric profile and outlier notes

### `position`

- Min/Max: **-4999.9 / 5000**
- 1st/99th percentile: **-4894.1 / 4897.6**
- IQR outliers: **0**

### `marketPrice`

- Min/Max: **100.31 / 25299.4**
- 1st/99th percentile: **105.64 / 24520.88**
- IQR outliers: **0**

### `pnl`

- Min/Max: **-2303320.64 / 2322375.98**
- 1st/99th percentile: **-1125332.45 / 1115479.1**
- IQR outliers: **16257**
- Note: heavy tails are expected for synthetic P&L distributions; sums should use `decimal` in API/database logic.

### `fxExposure`

- Min/Max: **-374996.27 / 374999.53**
- 1st/99th percentile: **-367398.54 / 367438.92**
- IQR outliers: **0**

## Formatting checks

- `asOfDate` format issues (`yyyy-MM-dd`): **0**
- CSV formatting anomalies detected: **none**

## Implementation implications

- The dataset quality is clean enough for strict validation (unknown columns/operators should return `400`).
- `pnl` uses a wide range; aggregate endpoints should use database-side `SUM` over `numeric/decimal`.
- Position totals should only be displayed when grouped by `commodity` due to mixed units across commodities.
